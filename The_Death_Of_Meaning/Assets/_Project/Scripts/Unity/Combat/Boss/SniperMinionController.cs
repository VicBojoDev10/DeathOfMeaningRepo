using System;
using System.Collections;
using TDOM.Data;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

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
        public SniperMinionProfile Perfil => _perfil;
        public Transform Plataforma => _plataforma;

        private HitZone _zona;
        private MinionAnimationController _animaciones;
        private Collider[] _colliders;
        private bool _muerto;
        private float _siguienteDisparo;
        private PlayerRoot _objetivo;
        private Transform _plataforma;
        private Collider[] _collidersPlataforma;
        private float _siguienteMovimiento;
        private Vector3 _anclaLocalPlataforma;
        private float _alturaSobreSuperficie;
        private Vector2 _desplazamientoActual;
        private Vector2 _desplazamientoDestino;

        /// <summary>Fija el punto de anclaje y la superficie física de la plataforma antes del spawn de red.</summary>
        public void ConfigurarPlataforma(
            Transform plataforma,
            Collider[] collidersPlataforma,
            Vector3 puntoSuperficie,
            float alturaSobreSuperficie
        )
        {
            _plataforma = plataforma;
            _collidersPlataforma = collidersPlataforma;
            _anclaLocalPlataforma = plataforma.InverseTransformPoint(puntoSuperficie);
            _alturaSobreSuperficie = alturaSobreSuperficie;
        }

        private void Awake()
        {
            _zona = GetComponent<HitZone>();
            _animaciones = GetComponent<MinionAnimationController>();
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
                Debug.LogError(
                    "[SniperMinionController] Falta asignar SniperMinionProfile en el prefab."
                );
                return;
            }

            if (_prefabBala == null)
                Debug.LogError(
                    "[SniperMinionController] Asigna el prefab de bala en el inspector."
                );

            if (!IsServer)
                return;

            if (
                _plataforma == null
                || _collidersPlataforma == null
                || _collidersPlataforma.Length == 0
            )
            {
                Debug.LogError("[SniperMinionController] Falta la plataforma o sus colliders.");
                return;
            }

            Vida.Value = _perfil.vida;
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

            ActualizarDestinoMovimiento();
            AcompanharMovimientoPlataforma();
            _animaciones?.ActualizarMovimiento(
                (_desplazamientoActual - _desplazamientoDestino).sqrMagnitude > 0.0001f
            );

            _objetivo = BuscarObjetivoMasCercano();
            if (_objetivo == null)
                return;

            Vector3 origen = _puntoDisparo != null ? _puntoDisparo.position : transform.position;
            Vector3 direccion = _objetivo.transform.position - origen;
            if (direccion.sqrMagnitude > 0.0001f)
            {
                Vector3 arriba = _plataforma != null ? _plataforma.up : Vector3.up;
                direccion = Vector3.ProjectOnPlane(direccion, arriba);
                if (direccion.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(direccion, arriba);
            }

            if (Time.time >= _siguienteDisparo)
            {
                Disparar(origen, direccion.normalized);
                _siguienteDisparo = Time.time + 1f / Mathf.Max(1, _perfil.cadenciaDisparo);
            }
        }

        private void AcompanharMovimientoPlataforma()
        {
            // La posición se calcula siempre desde el ancla local: el minion acompaña
            // traslación y rotación de la plataforma incluso cuando espera su siguiente paso.
            Vector3 ancla = _plataforma.TransformPoint(_anclaLocalPlataforma);
            Vector3 candidato =
                ancla
                + _plataforma.right * _desplazamientoActual.x
                + _plataforma.up * _desplazamientoActual.y;
            if (!TryProyectarEnPlataforma(candidato, out Vector3 superficie))
                return;

            Vector3 posicionObjetivo = superficie + _plataforma.forward * _alturaSobreSuperficie;
            transform.position = posicionObjetivo;
        }

        private void ActualizarDestinoMovimiento()
        {
            if (_plataforma == null)
                return;

            if (Time.time >= _siguienteMovimiento)
            {
                _siguienteMovimiento = Time.time + Mathf.Max(0.2f, _perfil.intervaloMovimiento);
                float radio = Mathf.Clamp(_perfil.radioMovimiento, 0f, 0.25f);
                for (int intento = 0; intento < 12; intento++)
                {
                    Vector2 destino = Random.insideUnitCircle * radio;
                    Vector3 ancla = _plataforma.TransformPoint(_anclaLocalPlataforma);
                    Vector3 candidato =
                        ancla + _plataforma.right * destino.x + _plataforma.up * destino.y;
                    if (!TryProyectarEnPlataforma(candidato, out _))
                        continue;
                    _desplazamientoDestino = destino;
                    break;
                }
            }

            float velocidadPaseo = Mathf.Clamp(_perfil.velocidad, 0.05f, 0.3f);
            _desplazamientoActual = Vector2.MoveTowards(
                _desplazamientoActual,
                _desplazamientoDestino,
                velocidadPaseo * Time.deltaTime
            );
        }

        private bool TryProyectarEnPlataforma(Vector3 candidato, out Vector3 puntoSuperficie)
        {
            puntoSuperficie = default;
            if (_plataforma == null || _collidersPlataforma == null)
                return false;

            // El modelo exportado tiene la cara superior en su eje local +Z.
            Vector3 normal = _plataforma.forward;
            float distanciaMasCercana = float.MaxValue;
            bool encontro = false;
            foreach (Collider collider in _collidersPlataforma)
            {
                if (collider == null || !collider.enabled || collider.isTrigger)
                    continue;

                float distanciaRayo = Mathf.Max(5f, collider.bounds.extents.magnitude * 2f + 2f);
                Vector3 arriba = candidato + normal * distanciaRayo;
                Vector3 abajo = candidato - normal * distanciaRayo;
                if (
                    collider.Raycast(
                        new Ray(arriba, -normal),
                        out RaycastHit hitArriba,
                        distanciaRayo * 2f
                    )
                )
                    ConsiderarHit(
                        hitArriba,
                        candidato,
                        ref distanciaMasCercana,
                        ref puntoSuperficie,
                        ref encontro
                    );
                if (
                    collider.Raycast(
                        new Ray(abajo, normal),
                        out RaycastHit hitAbajo,
                        distanciaRayo * 2f
                    )
                )
                    ConsiderarHit(
                        hitAbajo,
                        candidato,
                        ref distanciaMasCercana,
                        ref puntoSuperficie,
                        ref encontro
                    );
            }

            return encontro;
        }

        private static void ConsiderarHit(
            RaycastHit hit,
            Vector3 candidato,
            ref float distanciaMasCercana,
            ref Vector3 puntoSuperficie,
            ref bool encontro
        )
        {
            float distancia = (hit.point - candidato).sqrMagnitude;
            if (distancia >= distanciaMasCercana)
                return;
            distanciaMasCercana = distancia;
            puntoSuperficie = hit.point;
            encontro = true;
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
                Debug.LogError(
                    "[SniperMinionController] El prefab de bala necesita Proyectil y NetworkObject."
                );
                Destroy(bala);
                return;
            }

            // El daño y el impacto se resuelven en servidor dentro de Proyectil.
            proyectil.Inicializar(
                direccion,
                _perfil.danio,
                NetworkManager.ServerClientId,
                atacanteEsAyla: false,
                dañaJugador: true,
                plataformaIgnorada: _plataforma,
                emisorIgnorado: transform
            );
            _animaciones?.ReproducirAtaque();
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
            _animaciones?.ActualizarMovimiento(false);
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
