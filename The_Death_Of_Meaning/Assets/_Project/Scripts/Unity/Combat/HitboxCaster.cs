using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public class HitboxCaster : MonoBehaviour
    {
        [SerializeField] private Transform _origen;
        [SerializeField] private LayerMask _objetivos;
        [SerializeField] private NetworkObject _owner;
        public NetworkObject[] Detectar(float radio, float alcance)
        {
            {
                Vector3 centro = _origen.position + _origen.forward * alcance;

                Debug.DrawRay(_origen.position, _origen.forward * alcance, Color.red, 0.15f);

                var hits = Physics.OverlapSphere(centro, radio, _objetivos);

                return hits
                    .Select(h => h.GetComponentInParent<NetworkObject>())
                    .Where(n => n != null)
                    .Where(n => _owner == null || n != _owner)
                    .Distinct()
                    .ToArray();
            }
        }
        private void OnDrawGizmosSelected()
        {
            if (_origen == null)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_origen.position + _origen.forward * 1.0f, 1.5f);
        }

    }
}
