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

            var hits = Physics.OverlapSphere(centro, radio, _objetivos);

            return ImpactosValidos(hits)
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
        }
    }
}
