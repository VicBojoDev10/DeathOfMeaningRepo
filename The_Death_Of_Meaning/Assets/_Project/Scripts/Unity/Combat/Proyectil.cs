using TDOM.Data;
using TDOM.Gameplay.Combat;
using TDOM.Unity;
using TDOM.Unity.Audio;
using TDOM.Unity.Player;
using TDOM.Unity.Vfx;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public class Proyectil : NetworkBehaviour
    {
        private const float DistanciaEmpujeMinion = 0.45f;
        private const float DuracionEmpujeMinion = 0.18f;

        [SerializeField]
        private float _velocidad = 40f;

        [SerializeField]
        private float _vidaMax = 3f;

        [SerializeField]
        private float _radio = 0.15f;

        [SerializeField]
        private VfxBase _vfxImpactoJefe;

        [SerializeField]
        private SfxBanco _sfxImpactoAmbiente;

        [SerializeField]
        private SfxBanco _sfxImpactoJefe;

        [SerializeField]
        private LayerMask _capas = (1 << 0) | (1 << 6);

        private Vector3 _direccion;
        private float _daño;
        private ulong _atacanteId;
        private bool _atacanteEsAyla;
        private bool _dañaJugador;
        private Transform _plataformaIgnorada;
        private Transform _emisorIgnorado;
        private bool _detenerDurantePausaGlobal;
        private bool _pausadoGlobal;
        private float _tiempoVida;
        private bool _avanceLocalEnCliente;

        public float Velocidad => _velocidad;
        public float VidaMax => _vidaMax;
        public float Radio => _radio;
        public LayerMask Capas => _capas;
        public float Daño => _daño;
        public Vector3 Direccion => _direccion;

        public void Inicializar(
            Vector3 dir,
            float daño,
            ulong atacanteId,
            bool atacanteEsAyla = false,
            bool dañaJugador = false,
            Transform plataformaIgnorada = null,
            Transform emisorIgnorado = null,
            bool detenerDurantePausaGlobal = false
        )
        {
            _direccion = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
            _daño = daño;
            _atacanteId = atacanteId;
            _atacanteEsAyla = atacanteEsAyla;
            _dañaJugador = dañaJugador;
            _plataformaIgnorada = plataformaIgnorada;
            _emisorIgnorado = emisorIgnorado;
            _detenerDurantePausaGlobal = detenerDurantePausaGlobal;
            _tiempoVida = 0f;
        }

        public void EstablecerPausa(bool pausado)
        {
            if (_detenerDurantePausaGlobal)
                _pausadoGlobal = pausado;
        }

        private void Awake()
        {
            _avanceLocalEnCliente = GetComponent<NetworkTransform>() == null;
        }

        private void Update()
        {
            if (_avanceLocalEnCliente && IsSpawned && !IsServer)
            {
                transform.position += transform.forward * (_velocidad * Time.deltaTime);
                return;
            }

            if (!IsServer || (_detenerDurantePausaGlobal && _pausadoGlobal))
                return;

            float dt = Time.deltaTime;
            _tiempoVida += dt;

            if (_tiempoVida >= _vidaMax)
            {
                DespawnProyectil();
                return;
            }

            Vector3 pos = transform.position;
            Vector3 paso = _direccion * (_velocidad * dt);
            float distancia = paso.magnitude;
            RaycastHit hit = default;
            bool encontroImpacto = distancia > 0f && TryEncontrarImpacto(pos, distancia, out hit);

            if (encontroImpacto)
            {
                PlayerRoot jugador = hit.collider.GetComponentInParent<PlayerRoot>();
                if (jugador != null)
                {
                    if (_dañaJugador)
                    {
                        if (!jugador.EstaAbatidoOMuerto)
                        {
                            // Usamos la dirección real de vuelo: los tiradores de plataformas
                            // pueden estar elevados o desplazarse con una plataforma giratoria.
                            bool bloqueadoPorBarrera = jugador.MitigarGolpeMinion(
                                _daño,
                                pos - _direccion
                            );
                            if (!bloqueadoPorBarrera)
                                jugador.AplicarDanioEnServidor(_daño);
                            if (_detenerDurantePausaGlobal && !bloqueadoPorBarrera)
                                jugador.EmpujarDesdeServidor(
                                    _direccion,
                                    DistanciaEmpujeMinion,
                                    altura: 0f,
                                    duracion: DuracionEmpujeMinion
                                );
                            if (!bloqueadoPorBarrera)
                                Debug.Log(
                                    $"[Proyectil] Daño de {_daño:F1} aplicado al jugador {jugador.OwnerClientId}."
                                );
                        }
                        DespawnProyectil();
                        return;
                    }

                    transform.position = pos + paso;
                    return;
                }

                if (_dañaJugador)
                {
                    Debug.Log(
                        $"[Proyectil enemigo] Impactó con {hit.collider.name}, sin PlayerRoot."
                    );
                    DespawnProyectil();
                    return;
                }

                var hz = hit.collider.GetComponent<HitZone>();
                if (hz == null)
                {
                    hz = hit.collider.GetComponentInParent<HitZone>();
                }

                DianaDisparoZendre diana = hit.collider.GetComponentInParent<DianaDisparoZendre>();
                if (!_atacanteEsAyla && diana != null)
                {
                    diana.RecibirDisparoZendre(_daño);
                    Debug.Log(
                        $"[Proyectil] DianaZendre recibió {_daño:F1} de daño del disparo de Zendre."
                    );
                    DespawnProyectil();
                    return;
                }

                AtaquesArek jefe = hit.collider.GetComponentInParent<AtaquesArek>();
                jefe?.RegistrarImpacto(_atacanteId);
                if (hz != null)
                {
                    hz.RegistrarImpacto(
                        _daño,
                        _atacanteId,
                        TipoImpactoOjo.Disparo,
                        _atacanteEsAyla
                    );
                    LanzarVfxImpacto(_vfxImpactoJefe, hit);
                }

                ReproducirSfxImpacto(
                    jefe != null ? _sfxImpactoJefe : _sfxImpactoAmbiente,
                    hit.point
                );

                Debug.Log(
                    $"[Proyectil] Impacto con {hit.collider.name} en capa {LayerMask.LayerToName(hit.collider.gameObject.layer)} (HitZone: {hz != null}, Daño: {_daño})"
                );
                DespawnProyectil();
                return;
            }

            transform.position = pos + paso;
        }

        private bool TryEncontrarImpacto(Vector3 posicion, float distancia, out RaycastHit impacto)
        {
            impacto = default;
            if (!_dañaJugador)
            {
                return Physics.SphereCast(
                    posicion,
                    _radio,
                    _direccion,
                    out impacto,
                    distancia,
                    _capas,
                    QueryTriggerInteraction.Ignore
                );
            }

            RaycastHit[] impactos = Physics.SphereCastAll(
                posicion,
                _radio,
                _direccion,
                distancia,
                ~(1 << 2),
                QueryTriggerInteraction.Ignore
            );
            float distanciaMasCercana = float.MaxValue;
            bool encontro = false;
            foreach (RaycastHit hit in impactos)
            {
                if (hit.collider == null || EsImpactoIgnorado(hit.collider.transform))
                    continue;
                if (hit.distance >= distanciaMasCercana)
                    continue;

                impacto = hit;
                distanciaMasCercana = hit.distance;
                encontro = true;
            }

            return encontro;
        }

        private bool EsImpactoIgnorado(Transform objetivo)
        {
            return EsParteDe(objetivo, _plataformaIgnorada) || EsParteDe(objetivo, _emisorIgnorado);
        }

        private static bool EsParteDe(Transform objetivo, Transform raizIgnorada)
        {
            return raizIgnorada != null
                && (objetivo == raizIgnorada || objetivo.IsChildOf(raizIgnorada));
        }

        private void LanzarVfxImpacto(VfxBase efecto, RaycastHit hit)
        {
            if (efecto == null)
                return;

            ComponenteDelAtacante<VfxRed>()
                ?.Lanzar(efecto, hit.point, Quaternion.LookRotation(hit.normal));
        }

        private void ReproducirSfxImpacto(SfxBanco banco, Vector3 punto)
        {
            if (banco == null)
                return;

            ComponenteDelAtacante<SfxRed>()?.Reproducir(banco, punto);
        }

        private T ComponenteDelAtacante<T>()
            where T : Component
        {
            if (!NetworkManager.ConnectedClients.TryGetValue(_atacanteId, out var cliente))
                return null;

            foreach (var objeto in cliente.OwnedObjects)
            {
                var componente = objeto.GetComponentInChildren<T>();
                if (componente != null)
                    return componente;
            }

            return null;
        }

        private void DespawnProyectil()
        {
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
