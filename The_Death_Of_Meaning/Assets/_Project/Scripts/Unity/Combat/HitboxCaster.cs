using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public class HitboxCaster : MonoBehaviour
    {
        [SerializeField]
        private Transform _origen;

        [SerializeField]
        private LayerMask _objetivos;

        [SerializeField]
        private NetworkObject _owner;

        [Header("Prisma alrededor de la espada (solo si hay espada, ej. Ayla)")]
        [
            SerializeField,
            Tooltip(
                "Si hay espada en la mano, el golpe tambien detecta dentro de un prisma que la "
                    + "envuelve. Se suma a la esfera de siempre. Sin espada (Zendre) no cambia."
            )
        ]
        private bool _usarPrismaEspada = true;

        [SerializeField, Min(0.1f), Tooltip("Ancho total del prisma (metros), de lado a lado.")]
        private float _anchoPrisma = 2.5f;

        [SerializeField, Min(0.1f), Tooltip("Alto total del prisma (metros).")]
        private float _altoPrisma = 2.5f;

        [
            SerializeField,
            Min(0f),
            Tooltip("Metros extra que el prisma sobresale por la punta y por el mango.")
        ]
        private float _margenLargo = 0.75f;

        [
            SerializeField,
            Min(0f),
            Tooltip(
                "Solo servidor: metros extra de tolerancia al validar un golpe del prisma, "
                    + "contados desde el personaje (el servidor no ve la espada animada)."
            )
        ]
        private float _extraValidacionServidor = 2f;

        private AylaEspada _espada;

        private Transform Origen => _origen != null ? _origen : transform;

        public readonly struct Impacto
        {
            public readonly NetworkObject Objeto;
            public readonly HitZone Zona;

            public Impacto(NetworkObject objeto, HitZone zona)
            {
                Objeto = objeto;
                Zona = zona;
            }
        }

        public Impacto[] DetectarImpactos(float radio, float alcance)
        {
            Transform orig = Origen;
            Vector3 centro = Centro(alcance);

            Debug.DrawRay(orig.position, orig.forward * alcance, Color.red, 0.25f);

            // El mask del prefab de Ayla apunta a la capa HurtBox (6), mientras que
            // las HitZone de los ojos de AREK estan en Default (0). Busca los ojos
            // explicitamente por sus componentes para que el ataque melee tambien
            // los encuentre sin depender de tags o de que compartan capa.
            IEnumerable<Collider> hits = Physics.OverlapSphere(centro, radio, _objetivos);
            IEnumerable<Collider> todos = Physics.OverlapSphere(
                centro,
                radio,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide
            );

            if (TryPrisma(out Vector3 centroPrisma, out Quaternion giro, out Vector3 mitad))
            {
                hits = hits.Concat(Physics.OverlapBox(centroPrisma, mitad, giro, _objetivos));
                todos = todos.Concat(
                    Physics.OverlapBox(
                        centroPrisma,
                        mitad,
                        giro,
                        Physics.AllLayers,
                        QueryTriggerInteraction.Collide
                    )
                );
            }

            var ojos = todos.Where(c =>
                c.GetComponentInParent<OjosArek>() != null
                && c.GetComponentInParent<HitZone>() != null
            );

            return ImpactosValidos(hits.Concat(ojos))
                .GroupBy(i => new { i.Objeto, i.Zona })
                .Select(g => g.First())
                .ToArray();
        }

        private IEnumerable<Impacto> ImpactosValidos(IEnumerable<Collider> colliders) =>
            colliders
                .Select(c => new Impacto(
                    c.GetComponentInParent<NetworkObject>(),
                    c.GetComponentInParent<HitZone>()
                ))
                .Where(i => i.Objeto != null)
                .Where(i => _owner == null || i.Objeto != _owner);

        public Vector3 Centro(float alcance) => Origen.position + Origen.forward * alcance;

        public Vector3 PosicionOrigen => Origen.position;

        /// <summary>
        /// Metros extra de tolerancia que el servidor da al validar golpes del prisma. 0 si el
        /// personaje no tiene espada (o el prisma esta apagado).
        /// </summary>
        public float ExtraValidacionPrisma => HayEspada() ? _extraValidacionServidor : 0f;

        /// <summary>
        /// Prisma que envuelve la espada: del mango a la punta, con el ancho y alto
        /// configurados. False si el personaje no tiene espada o no se pudo leer la hoja.
        /// </summary>
        public bool TryPrisma(out Vector3 centro, out Quaternion rotacion, out Vector3 mitad)
        {
            centro = Vector3.zero;
            rotacion = Quaternion.identity;
            mitad = Vector3.zero;

            if (!HayEspada() || !_espada.Hoja(out Vector3 baseHoja, out Vector3 punta))
                return false;

            Vector3 eje = punta - baseHoja;
            float largo = eje.magnitude;
            if (largo < 0.01f)
                return false;

            Vector3 direccion = eje / largo;
            Vector3 arriba = Mathf.Abs(direccion.y) > 0.95f ? Origen.forward : Vector3.up;
            rotacion = Quaternion.LookRotation(direccion, arriba);
            centro = (baseHoja + punta) * 0.5f;
            mitad = new Vector3(
                _anchoPrisma * 0.5f,
                _altoPrisma * 0.5f,
                largo * 0.5f + _margenLargo
            );
            return true;
        }

        private bool HayEspada()
        {
            if (!_usarPrismaEspada)
                return false;

            if (_espada == null)
                _espada = Origen.GetComponentInChildren<AylaEspada>(true);
            return _espada != null;
        }

        public Impacto[] DetectarDisparo(float radio, float alcanceMaximo)
        {
            Transform orig = Origen;
            Debug.DrawRay(orig.position, orig.forward * alcanceMaximo, Color.yellow, 0.25f);

            var hits = Physics.SphereCastAll(
                orig.position,
                radio,
                orig.forward,
                alcanceMaximo,
                _objetivos
            );

            var masCercanos = hits.OrderBy(h => h.distance).Select(h => h.collider);
            return ImpactosValidos(masCercanos)
                .GroupBy(i => i.Objeto)
                .Select(g => g.First())
                .ToArray();
        }

        private void OnDrawGizmosSelected()
        {
            Transform orig = Origen;
            if (orig == null)
                return;

            var combat = GetComponentInParent<PlayerCombat>();
            if (combat == null && _owner != null)
                combat = _owner.GetComponent<PlayerCombat>();
            if (combat == null)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(
                orig.position + orig.forward * combat.AlcanceHitbox,
                combat.RadioHitbox
            );

            if (TryPrisma(out Vector3 centro, out Quaternion giro, out Vector3 mitad))
            {
                Gizmos.color = Color.cyan;
                Gizmos.matrix = Matrix4x4.TRS(centro, giro, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, mitad * 2f);
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
    }
}
