using System;
using System.Collections;
using TDOM.Data;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    [RequireComponent(typeof(HitZone))]
    public class SniperMinionController : NetworkBehaviour
    {
        [SerializeField]
        private SniperMinionProfile _perfil;

        [Tooltip("Hijo visual que se encoge cuando el sniper muere.")]
        [SerializeField]
        private Transform _visual;

        [SerializeField]
        private Renderer _renderer;

        [Header("Disparo")]
        [Tooltip("Prefab de proyectil con NetworkObject, NetworkTransform y Proyectil.")]
        [SerializeField]
        private GameObject _prefabBala;

        [Tooltip("Punto de salida de la bala. Si queda vacío, se usa la posición del sniper.")]
        [SerializeField]
        private Transform _puntoDisparo;

        public NetworkVariable<float> Vida = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>Solo servidor: el minion llegó a 0 de vida (antes del Despawn).</summary>
        public event Action<SniperMinionController> Murio;

        public bool EstaMuerto => _muerto;

        private HitZone _zona;
        private Collider[] _colliders;
        private bool _muerto;
        private float _siguienteDisparo;
        private PlayerRoot _objetivo;
        private Transform _plataforma;
        private Vector3 _posicionLocalPlataforma;
        private Quaternion _rotacionLocalPlataforma;

        /// <summary>Configura el anclaje antes de que el servidor sincronice el NetworkObject.</summary>
        public void ConfigurarPlataforma(Transform plataforma)
        {
            _plataforma = plataforma;
            if (_plataforma == null)
                return;

            _posicionLocalPlataforma = _plataforma.InverseTransformPoint(transform.position);
            _rotacionLocalPlataforma = Quaternion.Inverse(_plataforma.rotation) * transform.rotation;
        }

        private void Awake()
        {
            _zona = GetComponent<HitZone>();
            _colliders = GetComponentsInChildren<Collider>();

            if (_renderer == null)
                _renderer = GetComponentInChildren<Renderer>();

            // Se anima el hijo visual, no la raíz: la raíz la mueve NetworkTransform.
            if (_visual == null)
                _visual = _renderer != null ? _renderer.transform : transform;
        }

        public override void OnNetworkSpawn()
        {
            if (_perfil == null)
            {
                Debug.LogError("[SniperMinionController] Falta asignar SniperMinionProfile en el prefab.");
                return;
            }

            if (_prefabBala == null)
                Debug.LogError("[SniperMinionController] Asigna el prefab de bala en el inspector.");

            if (!IsServer)
                return;

            Vida.Value = _perfil.vida;
            _siguienteDisparo = Time.time + 1f / Mathf.Max(1, _perfil.cadenciaDisparo);
            _zona.DanioRegistrado += AlRecibirDanio;
        }

        public override void OnNetworkDespawn()
        {
            if (_zona != null)
                _zona.DanioRegistrado -= AlRecibirDanio;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsServer || !IsSpawned || _muerto || _perfil == null)
                return;

            SeguirPlataforma();

            _objetivo = BuscarObjetivoMasCercano();
            if (_objetivo == null)
                return;

            Vector3 origen = _puntoDisparo != null ? _puntoDisparo.position : transform.position;
            Vector3 direccion = _objetivo.transform.position - origen;
            if (direccion.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(new Vector3(direccion.x, 0f, direccion.z));

            if (Time.time >= _siguienteDisparo)
            {
                Disparar(origen, direccion.normalized);
                _siguienteDisparo = Time.time + 1f / Mathf.Max(1, _perfil.cadenciaDisparo);
            }
        }

        private void SeguirPlataforma()
        {
            if (_plataforma == null)
                return;

            transform.SetPositionAndRotation(
                _plataforma.TransformPoint(_posicionLocalPlataforma),
                _plataforma.rotation * _rotacionLocalPlataforma
            );
        }

        private PlayerRoot BuscarObjetivoMasCercano()
        {
            PlayerRoot objetivo = null;
            float distanciaMasCercana = float.MaxValue;
            foreach (var objeto in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (objeto == null || !objeto.TryGetComponent(out PlayerRoot jugador))
                    continue;
                if (jugador.EstaAbatidoOMuerto)
                    continue;

                float distancia = (jugador.transform.position - transform.position).sqrMagnitude;
                if (distancia >= distanciaMasCercana)
                    continue;

                objetivo = jugador;
                distanciaMasCercana = distancia;
            }

            return objetivo;
        }

        private void Disparar(Vector3 origen, Vector3 direccion)
        {
            if (_prefabBala == null)
                return;

            GameObject bala = Instantiate(_prefabBala, origen, Quaternion.LookRotation(direccion));
            Proyectil proyectil = bala.GetComponent<Proyectil>();
            NetworkObject networkObject = bala.GetComponent<NetworkObject>();
            if (proyectil == null || networkObject == null)
            {
                Debug.LogError("[SniperMinionController] El prefab de bala necesita Proyectil y NetworkObject.");
                Destroy(bala);
                return;
            }

            // Por ahora solo validamos la creación y sincronización visual; el daño se añadirá después.
            proyectil.Inicializar(direccion, 0f, NetworkManager.ServerClientId);
            networkObject.Spawn(true);
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
        private void MuerteRpc()
        {
            _muerto = true;

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

    }
}
