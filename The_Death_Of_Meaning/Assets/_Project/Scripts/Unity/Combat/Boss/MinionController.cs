using System;
using System.Collections;
using System.Collections.Generic;
using TDOM.Data;
using TDOM.Gameplay.Combat;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace TDOM.Unity
{
    /// <summary>
    /// Minion de AREK (TW-125), en greybox. La IA (MinionBrain) y el NavMeshAgent corren solo en
    /// el servidor; NetworkTransform replica la posición. Recibe daño por su HitZone, igual que
    /// los ojos del jefe. El aviso y la muerte se mandan por RPC para que ambos jugadores los vean.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(HitZone))]
    public class MinionController : NetworkBehaviour
    {
        private const float DuracionFlashGolpe = 0.25f;
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");

        [SerializeField]
        private MinionProfile _perfil;

        [Tooltip("Hijo con la malla del cubo; se inclina en el aviso y se encoge al morir.")]
        [SerializeField]
        private Transform _visual;

        [SerializeField]
        private Renderer _renderer;

        public NetworkVariable<float> Vida = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>Solo servidor: el minion llegó a 0 de vida (antes del Despawn).</summary>
        public event Action<MinionController> Murio;

        public bool EstaMuerto => _muerto;

        private NavMeshAgent _agente;
        private HitZone _zona;
        private Collider[] _colliders;
        private Material _material;
        private Color _colorBase;
        private Quaternion _rotacionVisual;
        private Coroutine _aviso;
        private bool _muerto;

        // Solo servidor.
        private MinionBrain _brain;
        private int _golpeDelCombo;
        private readonly HashSet<ulong> _golpeados = new HashSet<ulong>();
        private readonly List<PlayerRoot> _jugadores = new List<PlayerRoot>();
        private readonly List<MinionCandidato> _candidatos = new List<MinionCandidato>();

        private void Awake()
        {
            _agente = GetComponent<NavMeshAgent>();
            _zona = GetComponent<HitZone>();
            _colliders = GetComponentsInChildren<Collider>();

            if (_renderer == null)
                _renderer = GetComponentInChildren<Renderer>();

            // Se anima el hijo visual, no la raíz: la raíz la mueve NetworkTransform.
            if (_visual == null)
                _visual = _renderer != null ? _renderer.transform : transform;
            _rotacionVisual = _visual.localRotation;

            if (_renderer != null)
            {
                _material = _renderer.material;
                _colorBase = _material.HasProperty(IdColorBase)
                    ? _material.GetColor(IdColorBase)
                    : _material.color;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (_perfil == null)
            {
                Debug.LogError("[MinionController] Falta asignar el MinionProfile en el prefab.");
                return;
            }

            if (!IsServer)
            {
                // En clientes la posición la manda NetworkTransform; el agente no debe moverlo.
                _agente.enabled = false;
                return;
            }

            Vida.Value = _perfil.vida;
            _agente.speed = _perfil.velocidad;
            _agente.stoppingDistance = _perfil.rangoAtaque * 0.8f;
            _brain = new MinionBrain(
                _perfil.rangoAtaque,
                _perfil.duracionAviso,
                _perfil.pausaEntreGolpes,
                _perfil.duracionRecuperar,
                _perfil.golpesPorCombo
            );
            _zona.DanioRegistrado += AlRecibirDanio;
        }

        public override void OnNetworkDespawn()
        {
            if (_zona != null)
                _zona.DanioRegistrado -= AlRecibirDanio;
        }

        public override void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsServer || !IsSpawned || _muerto || _brain == null)
                return;

            bool hayObjetivo = TryObjetivoMasCercano(out PlayerRoot objetivo, out float distancia);
            var evento = _brain.Tick(Time.deltaTime, hayObjetivo, distancia);

            if (_brain.DebeMoverse && hayObjetivo)
            {
                _agente.isStopped = false;
                _agente.SetDestination(objetivo.transform.position);
            }
            else
            {
                _agente.isStopped = true;
                if (hayObjetivo && _brain.Estado != EstadoMinion.Recuperar)
                    MirarHacia(objetivo.transform.position);
            }

            switch (evento)
            {
                case EventoMinion.Aviso:
                    _golpeDelCombo = _brain.GolpesDados + 1;
                    AvisoRpc();
                    break;
                case EventoMinion.Golpe:
                    Golpear();
                    break;
            }
        }

        // Distancia en XZ al jugador vivo más cercano; los abatidos o muertos no son objetivo.
        private bool TryObjetivoMasCercano(out PlayerRoot objetivo, out float distancia)
        {
            objetivo = null;
            distancia = 0f;

            _jugadores.Clear();
            _candidatos.Clear();
            foreach (var objeto in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (objeto == null || !objeto.TryGetComponent(out PlayerRoot jugador))
                    continue;

                Vector3 hacia = jugador.transform.position - transform.position;
                hacia.y = 0f;
                bool vivo = !jugador.EstaAbatidoOMuerto;
                _jugadores.Add(jugador);
                _candidatos.Add(new MinionCandidato(jugador.OwnerClientId, hacia.magnitude, vivo));
            }

            if (!MinionBrain.TryElegirObjetivoVivo(_candidatos, out ulong id))
                return false;

            for (int i = 0; i < _jugadores.Count; i++)
            {
                if (_jugadores[i].OwnerClientId != id)
                    continue;

                objetivo = _jugadores[i];
                distancia = _candidatos[i].Distancia;
                return true;
            }

            return false;
        }

        private void MirarHacia(Vector3 posicion)
        {
            Vector3 hacia = posicion - transform.position;
            hacia.y = 0f;
            if (hacia.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(hacia.normalized, Vector3.up);
        }

        // Igual que los ataques del jefe: detecta y reporta, no resta vida (no hay sistema de vida).
        private void Golpear()
        {
            Vector3 mitad = _perfil.tamanioGolpe * 0.5f;
            Vector3 centro = transform.position + transform.forward * mitad.z;

            Collider[] hits = Physics.OverlapBox(
                centro,
                mitad,
                transform.rotation,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide
            );

            _golpeados.Clear();
            foreach (var hit in hits)
            {
                var jugador = hit.GetComponentInParent<PlayerRoot>();
                if (
                    jugador == null
                    || jugador.EstaAbatidoOMuerto
                    || !_golpeados.Add(jugador.OwnerClientId)
                )
                    continue;

                Debug.Log(
                    $"[Minion {NetworkObjectId}] golpe {_golpeDelCombo} a cliente {jugador.OwnerClientId} ({_perfil.danio:F0} de daño, sin restar vida)"
                );
                JugadorGolpeadoRpc(RpcTarget.Single(jugador.OwnerClientId, RpcTargetUse.Temp));
            }
        }

        private void AlRecibirDanio(float cantidad)
        {
            if (_muerto)
                return;

            Vida.Value = Mathf.Max(0f, Vida.Value - cantidad);
            Debug.Log($"[Minion {NetworkObjectId}] recibió {cantidad:F1}; vida {Vida.Value:F1}");

            if (Vida.Value <= 0f)
                Morir();
        }

        private void Morir()
        {
            _muerto = true;
            _agente.isStopped = true;
            MuerteRpc();
            Murio?.Invoke(this);
            StartCoroutine(DespawnTrasMuerte());
        }

        private IEnumerator DespawnTrasMuerte()
        {
            yield return new WaitForSeconds(_perfil.duracionMuerte);

            if (IsSpawned)
                NetworkObject.Despawn(true);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AvisoRpc()
        {
            if (_aviso != null)
                StopCoroutine(_aviso);
            _aviso = StartCoroutine(Avisar());
        }

        // El cubo se inclina y se pinta durante el aviso; al terminar vuelve a su pose.
        private IEnumerator Avisar()
        {
            PintarCuerpo(_perfil.colorAviso);
            _visual.localRotation =
                _rotacionVisual * Quaternion.Euler(_perfil.inclinacionAviso, 0f, 0f);

            yield return new WaitForSeconds(_perfil.duracionAviso);

            PintarCuerpo(_colorBase);
            _visual.localRotation = _rotacionVisual;
            _aviso = null;
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void JugadorGolpeadoRpc(RpcParams rpcParams = default)
        {
            AtaqueArekBase.NotificarGolpeAlJugadorLocal();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void MuerteRpc()
        {
            _muerto = true;
            if (_aviso != null)
                StopCoroutine(_aviso);

            foreach (var col in _colliders)
                col.enabled = false;

            StartCoroutine(Encoger());
        }

        private IEnumerator Encoger()
        {
            Vector3 escalaInicial = _visual.localScale;
            float duracion = Mathf.Max(0.01f, _perfil.duracionMuerte);

            for (float t = 0f; t < duracion; t += Time.deltaTime)
            {
                _visual.localScale = Vector3.Lerp(escalaInicial, Vector3.zero, t / duracion);
                yield return null;
            }

            _visual.localScale = Vector3.zero;
        }

        private void PintarCuerpo(Color color)
        {
            if (_material == null)
                return;

            if (_material.HasProperty(IdColorBase))
                _material.SetColor(IdColorBase, color);
            else
                _material.color = color;
        }

        private void OnDrawGizmosSelected()
        {
            if (_perfil == null)
                return;

            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.TRS(
                transform.position + transform.forward * (_perfil.tamanioGolpe.z * 0.5f),
                transform.rotation,
                Vector3.one
            );
            Gizmos.DrawWireCube(Vector3.zero, _perfil.tamanioGolpe);
        }
    }
}
