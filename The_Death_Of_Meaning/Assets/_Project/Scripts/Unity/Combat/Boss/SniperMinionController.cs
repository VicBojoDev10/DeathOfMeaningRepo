using System;
using System.Collections;
using TDOM.Data;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


namespace TDOM.Unity
{
    [RequireComponent(typeof(NavMeshAgent), typeof(HitZone))]
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
        public SniperMinionProfile Perfil => _perfil;

        private NavMeshAgent _agente;
        private HitZone _zona;
        private Collider[] _colliders;
        private bool _muerto;
        private float _siguienteDisparo;
        private PlayerRoot _objetivo;
        private Transform _plataforma;
        private float _siguienteMovimiento;
        private Vector3 _anclaLocalPlataforma;
        private Vector3 _ultimaPosicionDestino;
        private Vector3 _ultimaPosicionPlataforma;
        private Quaternion _ultimaRotacionPlataforma;
        private bool _tieneDestino;

        /// <summary>Configura la navegación de plataforma antes de sincronizar el NetworkObject.</summary>
        public void ConfigurarPlataforma(Transform plataforma, NavMeshSurface superficie)
        {
            _plataforma = plataforma;
            _superficiePlataforma = superficie;
        }

        private NavMeshSurface _superficiePlataforma;

        private void Awake()
        {
            _zona = GetComponent<HitZone>();
            _agente = GetComponent<NavMeshAgent>();
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
            {
                if (_agente != null)
                    _agente.enabled = false;
                return;
            }

            if (_agente == null || _superficiePlataforma == null || _superficiePlataforma.navMeshData == null)
            {
                Debug.LogError("[SniperMinionController] Falta el NavMeshAgent o el NavMesh de su plataforma.");
                return;
            }

            Vida.Value = _perfil.vida;
            _agente.speed = _perfil.velocidad;
            _agente.stoppingDistance = 0.25f;
            if (!_agente.Warp(transform.position))
                Debug.LogWarning($"[SniperMinionController] No se pudo colocar {name} sobre el NavMesh de su plataforma.");
            _anclaLocalPlataforma = _plataforma != null ? _plataforma.InverseTransformPoint(transform.position) : transform.position;
            if (_plataforma != null)
            {
                _ultimaPosicionPlataforma = _plataforma.position;
                _ultimaRotacionPlataforma = _plataforma.rotation;
            }
            _siguienteMovimiento = Time.time + Random.Range(0.25f, 1f);
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

            AcompanharMovimientoPlataforma();
            ActualizarDestinoMovimiento();

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

        private void AcompanharMovimientoPlataforma()
        {
            if (_plataforma == null || _agente == null || !_agente.enabled)
                return;

            Vector3 posicionLocalAnterior = Quaternion.Inverse(_ultimaRotacionPlataforma)
                * (transform.position - _ultimaPosicionPlataforma);
            Vector3 posicionActualizada = _plataforma.position + _plataforma.rotation * posicionLocalAnterior;
            Vector3 deltaPlataforma = posicionActualizada - transform.position;
            if (deltaPlataforma.sqrMagnitude > 0.0001f)
                _agente.Warp(posicionActualizada);

            _ultimaPosicionPlataforma = _plataforma.position;
            _ultimaRotacionPlataforma = _plataforma.rotation;
        }

        private void ActualizarDestinoMovimiento()
        {
            if (_plataforma == null || _agente == null || !_agente.enabled || !_agente.isOnNavMesh || Time.time < _siguienteMovimiento)
                return;

            _siguienteMovimiento = Time.time + Mathf.Max(0.2f, _perfil.intervaloMovimiento);
            PlayerRoot jugador = BuscarObjetivoMasCercano();
            if (jugador == null)
                return;

            Vector3 origenAncla = _plataforma.TransformPoint(_anclaLocalPlataforma);
            Vector3 haciaJugador = jugador.transform.position - origenAncla;
            haciaJugador.y = 0f;
            if (haciaJugador.sqrMagnitude < 0.01f)
                return;

            float radioMovimiento = Mathf.Max(0.5f, _perfil.radioMovimiento);
            Vector3 sesgoJugador = haciaJugador.normalized * Mathf.Min(0.3f, radioMovimiento * 0.3f);
            NavMeshQueryFilter filtro = new NavMeshQueryFilter
            {
                agentTypeID = _agente.agentTypeID,
                areaMask = _agente.areaMask,
            };
            NavMeshHit hit = default;
            bool encontroDestino = false;
            for (int intento = 0; intento < 12; intento++)
            {
                Vector2 paseo = Random.insideUnitCircle * (radioMovimiento * 0.55f);
                Vector3 deseado = origenAncla + sesgoJugador + new Vector3(paseo.x, 0f, paseo.y);
                if (!NavMesh.SamplePosition(deseado, out hit, 0.75f, filtro))
                    continue;
                if (!EsPuntoSobrePlataforma(hit.position))
                    continue;
                if ((hit.position - origenAncla).sqrMagnitude > radioMovimiento * radioMovimiento)
                    continue;
                if (!NavMesh.FindClosestEdge(hit.position, out NavMeshHit borde, filtro) || borde.distance < 0.12f)
                    continue;

                encontroDestino = true;
                break;
            }

            if (!encontroDestino)
                return;
            if (_tieneDestino && (hit.position - _ultimaPosicionDestino).sqrMagnitude < 0.2f)
                return;

            NavMeshPath ruta = new NavMeshPath();
            if (!_agente.CalculatePath(hit.position, ruta) || ruta.status != NavMeshPathStatus.PathComplete)
                return;

            _ultimaPosicionDestino = hit.position;
            _tieneDestino = true;
            _agente.SetDestination(hit.position);
        }

        private bool EsPuntoSobrePlataforma(Vector3 punto)
        {
            if (_plataforma == null)
                return false;

            Collider[] colliders = _plataforma.GetComponentsInChildren<Collider>();
            foreach (RaycastHit hit in Physics.RaycastAll(
                punto + Vector3.up * 1.25f,
                Vector3.down,
                3f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            ))
            {
                if (hit.collider.transform.IsChildOf(_plataforma) || hit.collider.transform == _plataforma)
                {
                    for (int i = 0; i < colliders.Length; i++)
                    {
                        if (colliders[i] == hit.collider && (hit.point - punto).sqrMagnitude <= 1f)
                            return true;
                    }
                }
            }

            return false;
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
