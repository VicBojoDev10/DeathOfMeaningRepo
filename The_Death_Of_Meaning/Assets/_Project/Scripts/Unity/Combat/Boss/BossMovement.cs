using TDOM.Contracts;
using TDOM.POCO.ScriptableObjects;
using TDOM.Unity.Combat;
using UnityEngine;

namespace TDOM.Unity
{
    public class BossMovement : MonoBehaviour
    {
        [SerializeField]
        private BossController _jefe;

        [SerializeField]
        private AtaquesArek _ataques;

        [Header("Límite circular de la arena")]
        [SerializeField, Min(0f)]
        private float _margenBorde = 3f;

        [SerializeField, Min(0f)]
        private float _radioArenaTotalManual;

        [SerializeField, Min(16)]
        private int _segmentosGizmo = 64;

        private GameObject _circuloArena;
        private float _proximaBusquedaArena;

        private readonly TDOM.Gameplay.Combat.BossMovement _logica =
            new TDOM.Gameplay.Combat.BossMovement();

        private void Awake()
        {
            if (_jefe == null)
                _jefe = GetComponent<BossController>();
            if (_ataques == null)
                _ataques = GetComponent<AtaquesArek>();
        }

        private void OnEnable()
        {
            if (_jefe != null)
                _jefe.ServerMovementTick += Mover;
        }

        private void OnDisable()
        {
            if (_jefe != null)
                _jefe.ServerMovementTick -= Mover;
        }

        private void Mover(float dt, int fase, bool ataqueEnCurso, BossAttackKind tipoAtaque)
        {
            if (_jefe == null || !_jefe.IsServer || _jefe.Pausado)
                return;

            if (_jefe.HayOjoAbierto)
                return;

            Vector3 posicion = transform.position;
            BossPhaseProfile perfil = _jefe.PerfilFaseActual;
            bool detenersePorAtaque =
                ataqueEnCurso && perfil != null && perfil.DetenerseDuranteAtaque(tipoAtaque);
            if (
                fase > 1
                && perfil != null
                && perfil.VelocidadMovimiento > 0f
                && !detenersePorAtaque
                && _jefe.PuedeMoverse
                && _ataques != null
                && _ataques.TryObjetivoMovimiento(out ulong objetivoId, out bool zendreAnclado)
                && _ataques.TryPosicionJugador(objetivoId, out Vector3 objetivo)
            )
            {
                _logica.Avanzar(
                    fase,
                    zendreAnclado,
                    posicion.x,
                    posicion.z,
                    objetivo.x,
                    objetivo.z,
                    perfil.VelocidadMovimiento,
                    perfil.DistanciaMinimaJugador,
                    dt,
                    out float x,
                    out float z
                );
                posicion = new Vector3(x, posicion.y, z);
            }

            if (TryObtenerLimitesArena(out Vector3 centro, out float radioInterior))
            {
                _logica.LimitarDentroDeCirculo(
                    posicion.x,
                    posicion.z,
                    centro.x,
                    centro.z,
                    radioInterior,
                    out float xLimitada,
                    out float zLimitada
                );
                posicion = new Vector3(xLimitada, posicion.y, zLimitada);
            }

            transform.position = posicion;
        }

        private bool TryObtenerLimitesArena(out Vector3 centro, out float radioInterior)
        {
            centro = default;
            radioInterior = 0f;
            GameObject circulo = ResolverCirculoArena();
            if (circulo == null)
                return false;

            if (!TryObtenerRadioTotal(circulo, out float radioTotal))
                return false;

            centro = ObtenerBoundsCirculo(circulo).center;
            radioInterior = TDOM.Gameplay.Combat.BossMovement.CalcularRadioSeguro(
                radioTotal,
                _margenBorde,
                ObtenerRadioHorizontalBoss()
            );
            return radioInterior > 0f;
        }

        private float ObtenerRadioHorizontalBoss()
        {
            float radio = 0f;
            Vector3 origen = transform.position;
            foreach (Renderer rendererBoss in GetComponentsInChildren<Renderer>(true))
            {
                if (!rendererBoss.enabled || !rendererBoss.gameObject.activeInHierarchy)
                    continue;

                Bounds bounds = rendererBoss.bounds;
                float extensoX = Mathf.Abs(bounds.center.x - origen.x) + bounds.extents.x;
                float extensoZ = Mathf.Abs(bounds.center.z - origen.z) + bounds.extents.z;
                radio = Mathf.Max(radio, Mathf.Sqrt(extensoX * extensoX + extensoZ * extensoZ));
            }

            return radio;
        }

        private bool TryObtenerRadioTotal(GameObject circulo, out float radio)
        {
            radio = _radioArenaTotalManual;
            if (radio > 0f)
                return true;

            Bounds bounds = ObtenerBoundsCirculo(circulo);
            radio = Mathf.Min(bounds.extents.x, bounds.extents.z);
            return radio > 0f;
        }

        private static Bounds ObtenerBoundsCirculo(GameObject circulo)
        {
            var renderer = circulo.GetComponent<Renderer>();
            if (renderer == null)
                renderer = circulo.GetComponentInChildren<Renderer>();
            return renderer != null
                ? renderer.bounds
                : new Bounds(circulo.transform.position, Vector3.zero);
        }

        private GameObject ResolverCirculoArena()
        {
            if (_circuloArena != null)
                return _circuloArena;

            if (Application.isPlaying)
            {
                if (Time.time < _proximaBusquedaArena)
                    return null;
                _proximaBusquedaArena = Time.time + 1f;
            }
            Transform puenteArena = null;
            foreach (Transform candidato in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (candidato.name != "PuenteColiseoArena_V002")
                    continue;
                if (!candidato.gameObject.scene.IsValid())
                    continue;

                puenteArena = candidato;
                break;
            }

            if (puenteArena != null)
            {
                foreach (Transform hijo in puenteArena.GetComponentsInChildren<Transform>(true))
                {
                    if (hijo.name != "Circle")
                        continue;

                    _circuloArena = hijo.gameObject;
                    return _circuloArena;
                }
            }

            return null;
        }

        private void OnDrawGizmosSelected()
        {
            GameObject circulo = ResolverCirculoArena();
            if (circulo == null || !TryObtenerRadioTotal(circulo, out float radioTotal))
                return;

            Bounds bounds = ObtenerBoundsCirculo(circulo);
            Vector3 centro = bounds.center;
            float radioInterior = TDOM.Gameplay.Combat.BossMovement.CalcularRadioSeguro(
                radioTotal,
                _margenBorde,
                ObtenerRadioHorizontalBoss()
            );
            float altura = bounds.max.y + 0.1f;

            DibujarCirculoGizmo(centro, radioTotal, altura, Color.cyan);
            DibujarCirculoGizmo(centro, radioInterior, altura + 0.02f, Color.yellow);
        }

        private void DibujarCirculoGizmo(Vector3 centro, float radio, float altura, Color color)
        {
            if (radio <= 0f)
                return;

            Gizmos.color = color;
            Vector3 anterior = new Vector3(centro.x + radio, altura, centro.z);
            int segmentos = Mathf.Max(16, _segmentosGizmo);
            for (int i = 1; i <= segmentos; i++)
            {
                float angulo = i * Mathf.PI * 2f / segmentos;
                Vector3 actual = new Vector3(
                    centro.x + Mathf.Cos(angulo) * radio,
                    altura,
                    centro.z + Mathf.Sin(angulo) * radio
                );
                Gizmos.DrawLine(anterior, actual);
                anterior = actual;
            }
        }
    }
}
