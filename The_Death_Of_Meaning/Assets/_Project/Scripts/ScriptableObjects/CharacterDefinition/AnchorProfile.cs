using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(menuName = "TDOM/Anchor Profile", fileName = "AnchorProfile")]
    public sealed class AnchorProfile : ScriptableObject
    {
        public float Range;
        public float TravelSpeed;
        public float StuckDuration;
        public float Cooldown;
    }
}
