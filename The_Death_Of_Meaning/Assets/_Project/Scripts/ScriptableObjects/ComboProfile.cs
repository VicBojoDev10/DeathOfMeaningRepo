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

        [Tooltip("Segundos tras completar el combo en los que se ignora todo input de ataque.")]
        public float CooldownFinCombo = 0.3f;
    }
}
