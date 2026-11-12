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
        private const float DuracionPausaTrasGolpe = 1f;
        private const float DistanciaEmpujeJugador = 0.45f;
        private const float DuracionEmpujeJugador = 0.18f;
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
        private MinionAnimationController _animaciones;
        private Collider[] _colliders;
        private Material _material;
        private Color _colorBase;
        private Quaternion _rotacionVisual;
        private Coroutine _aviso;
        private bool _muerto;
        private bool _pausadoGlobal;
        private bool _agenteEstabaDetenidoAntesDePausa;
        private float _pausaTrasGolpeRestante;

        // Solo servidor.
        private MinionBrain _brain;
        private EmpujeMinion _empuje;
        private int _golpeDelCombo;
        private float _siguienteGolpePermitido;
        private readonly HashSet<ulong> _golpeados = new HashSet<ulong>();
        private readonly List<PlayerRoot> _jugadores = new List<PlayerRoot>();
        private readonly List<MinionCandidato> _candidatos = new List<MinionCandidato>();

        private void Awake()
        {
            _agente = GetComponent<NavMeshAgent>();
            _zona = GetComponent<HitZone>();
            _animaciones = GetComponent<MinionAnimationController>();
            _colliders = GetComponentsInChildren<Collider>();
            _empuje = new EmpujeMinion(_agente, GetComponent<Collider>());

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
            if (!IsServer || !IsSpawned || _muerto || _brain == null || _pausadoGlobal)
                return;

            if (_pausaTrasGolpeRestante > 0f)
            {
                _pausaTrasGolpeRestante = Mathf.Max(0f, _pausaTrasGolpeRestante - Time.deltaTime);
                _agente.isStopped = true;
                _animaciones?.ActualizarMovimiento(false);
                if (_pausaTrasGolpeRestante > 0f)
                    return;
            }

            if (_empuje.Tick(Time.deltaTime))
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

            _animaciones?.ActualizarMovimiento(_brain.DebeMoverse && hayObjetivo);

            switch (evento)
            {
                case EventoMinion.Aviso:
                    _golpeDelCombo = _brain.GolpesDados + 1;
                    AvisoRpc();
                    break;
                case EventoMinion.Golpe:
                    // El evento Golpe se emite al terminar el aviso del cerebro.
                    _animaciones?.ReproducirAtaque();
                    if (Time.time >= _siguienteGolpePermitido && Golpear())
                    {
                        _siguienteGolpePermitido = Time.time + 1f;
                        DetenerTrasGolpe();
                    }
                    break;
            }
        }

        public void EstablecerPausa(bool pausado)
        {
            if (_pausadoGlobal == pausado)
                return;

            _pausadoGlobal = pausado;
            if (IsServer && _agente != null && _agente.enabled)
            {
                if (pausado)
                {
                    _agenteEstabaDetenidoAntesDePausa = _agente.isStopped;
                    _agente.isStopped = true;
                }
                else if (!_muerto)
                {
                    _agente.isStopped = _agenteEstabaDetenidoAntesDePausa;
                }
            }

            _animaciones?.EstablecerPausa(pausado);
        }

        public bool EmpujarEnServidor(Vector3 direccion, DashProfile perfil)
        {
            if (
                !IsServer
                || !IsSpawned
                || _muerto
                || _brain == null
                || !_empuje.TryIniciar(direccion, perfil)
            )
                return false;

            _brain.InterrumpirAtaque();
            InterrumpirAvisoRpc();
            return true;
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void InterrumpirAvisoRpc()
        {
            if (_aviso != null)
                StopCoroutine(_aviso);
            _aviso = null;
            PintarCuerpo(_colorBase);
            _visual.localRotation = _rotacionVisual;
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

        // El servidor detecta el golpe y aplica el daño directamente al jugador.
        private bool Golpear()
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
            bool conecto = false;
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
                    $"[Minion {NetworkObjectId}] golpe {_golpeDelCombo} a cliente {jugador.OwnerClientId} ({_perfil.danio:F0} de daño)"
                );
                jugador.AplicarDanioEnServidor(_perfil.danio);
                EmpujarJugador(jugador);
                JugadorGolpeadoRpc(RpcTarget.Single(jugador.OwnerClientId, RpcTargetUse.Temp));
                conecto = true;
            }

            return conecto;
        }

        private void EmpujarJugador(PlayerRoot jugador)
        {
            Vector3 direccion = jugador.transform.position - transform.position;
            direccion.y = 0f;
            if (direccion.sqrMagnitude < 0.0001f)
                direccion = transform.forward;

            jugador.EmpujarDesdeServidor(
                direccion,
                DistanciaEmpujeJugador,
                altura: 0f,
                duracion: DuracionEmpujeJugador
            );
        }

        private void DetenerTrasGolpe()
        {
            _pausaTrasGolpeRestante = DuracionPausaTrasGolpe;
            if (_agente != null && _agente.enabled)
                _agente.isStopped = true;
            _animaciones?.ActualizarMovimiento(false);
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
            _animaciones?.ActualizarMovimiento(false);
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

            float restante = _perfil.duracionAviso;
            while (restante > 0f)
            {
                if (!_pausadoGlobal)
                    restante -= Time.deltaTime;
                yield return null;
            }

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

    /// <summary>Combina las pausas de cinemática y menú para minions y sus proyectiles.</summary>
    public static class PausaMinionsActivos
    {
        private static bool _pausaCinematica;
        private static bool _pausaMenu;
        private static NetworkManager _networkManager;

        public static void EstablecerPausaCinematica(NetworkManager networkManager, bool pausado)
        {
            if (networkManager != null)
                _networkManager = networkManager;
            _pausaCinematica = pausado;
            AplicarPausaCombinada();
        }

        public static void EstablecerPausaMenu(NetworkManager networkManager, bool pausado)
        {
            if (networkManager != null)
                _networkManager = networkManager;
            _pausaMenu = pausado;
            AplicarPausaCombinada();
        }

        private static void AplicarPausaCombinada()
        {
            if (_networkManager?.SpawnManager == null)
                return;

            bool pausado = _pausaCinematica || _pausaMenu;
            foreach (NetworkObject objeto in _networkManager.SpawnManager.SpawnedObjectsList)
            {
                if (objeto == null)
                    continue;

                objeto.GetComponent<MinionController>()?.EstablecerPausa(pausado);
                objeto.GetComponent<SniperMinionController>()?.EstablecerPausa(pausado);
                objeto.GetComponent<Proyectil>()?.EstablecerPausa(pausado);
            }
        }
    }
}
