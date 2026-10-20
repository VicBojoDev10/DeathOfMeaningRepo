using UnityEngine;

namespace TDOM.Unity.Combat
{
    public class PlayerCombatAnimator : MonoBehaviour
    {
        private static readonly int ComboIndexHash = Animator.StringToHash("ComboIndex");
        private static readonly int ChargedHash = Animator.StringToHash("Charged");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        [SerializeField]
        private Animator _animator;

        public void PlayCombo(int comboIndex, bool charged)
        {
            if (_animator == null)
                return;

            _animator.SetInteger(ComboIndexHash, comboIndex);
            _animator.SetBool(ChargedHash, charged);
            _animator.ResetTrigger(AttackHash);
            _animator.SetTrigger(AttackHash);
        }
    }
}
