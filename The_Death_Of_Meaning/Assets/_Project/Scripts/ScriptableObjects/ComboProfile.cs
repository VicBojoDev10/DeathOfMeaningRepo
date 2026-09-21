using TDOM.Contracts;
using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "ComboProfile", menuName = "TDOM/ComboProfile")]
    public class ComboProfile : ScriptableObject
    {
        public AttackStep[] Steps;
        public AttackStep Charged;
        public float HoldThreshold,
            MaxChargeTime,
            BufferWindow;
    }
}
