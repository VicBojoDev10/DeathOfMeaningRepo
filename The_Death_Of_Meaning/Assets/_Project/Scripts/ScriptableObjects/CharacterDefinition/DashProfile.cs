using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(menuName = "TDOM/Dash Profile")]
    public sealed class DashProfile : ScriptableObject
    {
        public float Distance,
            Duration,
            Cooldown,
            MaxTurnRate;
        public AnimationCurve Easing = AnimationCurve.Linear(0, 0, 1, 1);
    }
}
