using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(menuName = "TDOM/Grapple Profile")]
    public sealed class GrappleProfile : ScriptableObject
    {
        public float Range;
        public float PullSpeed;
        public float Cooldown;
        public float ArrivalDistance;
        public float MaxDuration;
    }
}
