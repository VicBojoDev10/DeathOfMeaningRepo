using TDOM.Gameplay.Combat;
using TDOM.Unity;
using TDOM.Unity.Player;
using TDOM.Unity.Vfx;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public class Proyectil : NetworkBehaviour
    {
        [SerializeField]
        private float _velocidad = 40f;

        [SerializeField]
        private float _vidaMax = 3f;

        [SerializeField]
        private float _radio = 0.15f;

        [SerializeField]
        private VfxBase _vfxImpactoJefe;

        [SerializeField]
        private LayerMask _capas = (1 << 0) | (1 << 6);

        private Vector3 _direccion;
        private float _daño;
        private ulong _atacanteId;
        private bool _atacanteEsAyla;
        private bool _dañaJugador;
        private Transform _plataformaIgnorada;
        private Transform _emisorIgnorado;
        private float _tiempoVida;

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
            Transform emisorIgnorado = null
        )
        {
            _direccion = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
            _daño = daño;
            _atacanteId = atacanteId;
            _atacanteEsAyla = atacanteEsAyla;
            _dañaJugador = dañaJugador;
            _plataformaIgnorada = plataformaIgnorada;
            _emisorIgnorado = emisorIgnorado;
            _tiempoVida = 0f;
        }

        private void Update()
        {
            if (!IsServer)
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
                            jugador.AplicarDanioEnServidor(_daño);
                            Debug.Log($"[Proyectil] Daño de {_daño:F1} aplicado al jugador {jugador.OwnerClientId}.");
                        }
                        DespawnProyectil();
                        return;
                    }

                    transform.position = pos + paso;
                    return;
                }

                if (_dañaJugador)
                {
                    Debug.Log($"[Proyectil enemigo] Impactó con {hit.collider.name}, sin PlayerRoot.");
                    DespawnProyectil();
                    return;
                }

                var hz = hit.collider.GetComponent<HitZone>();
                if (hz == null)
                {
                    hz = hit.collider.GetComponentInParent<HitZone>();
                }

                if (hz != null)
                {
                    hit.collider.GetComponentInParent<AtaquesArek>()?.RegistrarImpacto(_atacanteId);
                    hz.RegistrarImpacto(
                        _daño,
                        _atacanteId,
                        TipoImpactoOjo.Disparo,
                        _atacanteEsAyla
                    );
                    LanzarVfxImpacto(_vfxImpactoJefe, hit);
                }
                else
                {
                    hit.collider.GetComponentInParent<AtaquesArek>()?.RegistrarImpacto(_atacanteId);
                }

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
                    _capas
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
            return raizIgnorada != null && (objetivo == raizIgnorada || objetivo.IsChildOf(raizIgnorada));
        }

        private void LanzarVfxImpacto(VfxBase efecto, RaycastHit hit)
        {
            if (efecto == null)
                return;

            RedDelAtacante()?.Lanzar(efecto, hit.point, Quaternion.LookRotation(hit.normal));
        }

        private VfxRed RedDelAtacante()
        {
            if (!NetworkManager.ConnectedClients.TryGetValue(_atacanteId, out var cliente))
                return null;

            foreach (var objeto in cliente.OwnedObjects)
            {
                var red = objeto.GetComponentInChildren<VfxRed>();
                if (red != null)
                    return red;
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
