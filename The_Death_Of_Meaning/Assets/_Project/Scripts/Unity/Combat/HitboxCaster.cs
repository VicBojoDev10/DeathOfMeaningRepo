using System.Collections.Generic;
using System.Linq;
using TDOM.Unity.Player;
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
        private float _anchoPrisma = 4f;

        [SerializeField, Min(0.1f), Tooltip("Alto total del prisma (metros).")]
        private float _altoPrisma = 4f;

        [
            SerializeField,
            Min(0f),
            Tooltip("Metros extra que el prisma sobresale por la punta y por el mango.")
        ]
        private float _margenLargo = 1.5f;

        [
            SerializeField,
            Min(0f),
            Tooltip(
                "Solo servidor: metros extra de tolerancia al validar un golpe del prisma, "
                    + "contados desde el personaje (el servidor no ve la espada animada)."
            )
        ]
        private float _extraValidacionServidor = 3.5f;

        private readonly List<Impacto> _impactos = new();
        private readonly HashSet<(NetworkObject, HitZone)> _zonasDetectadas = new();
        private readonly HashSet<NetworkObject> _objetosDetectados = new();
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

            _impactos.Clear();
            _zonasDetectadas.Clear();
            var hits = Physics.OverlapSphere(centro, radio, _objetivos);
            var todos = Physics.OverlapSphere(
                centro,
                radio,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide
            );
            AgregarImpactos(hits, false);

            Collider[] ojosPrisma = null;
            if (TryPrisma(out Vector3 centroPrisma, out Quaternion giro, out Vector3 mitad))
            {
                AgregarImpactos(Physics.OverlapBox(centroPrisma, mitad, giro, _objetivos), false);
                ojosPrisma = Physics.OverlapBox(
                    centroPrisma,
                    mitad,
                    giro,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Collide
                );
            }

            AgregarImpactos(todos, true);
            if (ojosPrisma != null)
                AgregarImpactos(ojosPrisma, true);

            var resultado = _impactos.ToArray();
            _impactos.Clear();
            _zonasDetectadas.Clear();
            return resultado;
        }

        private void AgregarImpactos(Collider[] colliders, bool soloOjos)
        {
            foreach (var collider in colliders)
            {
                if (soloOjos && collider.GetComponentInParent<OjosArek>() == null)
                    continue;
                if (!TryImpacto(collider, out Impacto impacto))
                    continue;
                if (soloOjos && impacto.Zona == null)
                    continue;
                if (_zonasDetectadas.Add((impacto.Objeto, impacto.Zona)))
                    _impactos.Add(impacto);
            }
        }

        private bool TryImpacto(Collider collider, out Impacto impacto)
        {
            impacto = default;
            var objeto = collider.GetComponentInParent<NetworkObject>();
            if (objeto == null || (_owner != null && objeto == _owner))
                return false;
            if (objeto.GetComponentInParent<PlayerRoot>() != null)
                return false;

            impacto = new Impacto(objeto, collider.GetComponentInParent<HitZone>());
            return true;
        }

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

            _impactos.Clear();
            _objetosDetectados.Clear();
            foreach (var hit in hits.OrderBy(h => h.distance))
            {
                if (
                    TryImpacto(hit.collider, out Impacto impacto)
                    && _objetosDetectados.Add(impacto.Objeto)
                )
                    _impactos.Add(impacto);
            }

            var resultado = _impactos.ToArray();
            _impactos.Clear();
            _objetosDetectados.Clear();
            return resultado;
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
