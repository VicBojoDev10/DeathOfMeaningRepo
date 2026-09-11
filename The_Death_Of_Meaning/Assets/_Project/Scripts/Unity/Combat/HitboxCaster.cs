using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public class HitboxCaster : MonoBehaviour
    {
        [SerializeField] private Transform _origen;
        [SerializeField] private LayerMask _objetivos;
        public NetworkObject[] Detectar(float radio, float alcance)
        {
            var hits = Physics.OverlapSphere(_origen.position + _origen.forward * alcance, radio, _objetivos);
            return hits.Select(h => h.GetComponentInParent<NetworkObject>()).Where(n => n != null).Distinct().ToArray();
        }

    }
}
