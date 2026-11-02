using UnityEngine;

namespace TDOM.Unity.Level
{
    public sealed class LedgeClimbAnchor : MonoBehaviour
    {
        [SerializeField]
        private Transform _topPoint;

        public Transform TopPoint => _topPoint;
    }
}
