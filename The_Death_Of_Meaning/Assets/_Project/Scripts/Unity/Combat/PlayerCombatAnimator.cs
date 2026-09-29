using UnityEngine;

namespace TDOM.Unity.Combat
{
    public class PlayerCombatAnimator : MonoBehaviour
    {
        [SerializeField]
        private Animator _animator;

        public void PlayCombo(int comboIndex, bool charged)
        {
            if (_animator == null)
                return;

            _animator.SetInteger("ComboIndex", comboIndex);
            _animator.SetBool("Charged", charged);
            _animator.SetTrigger("Attack");
        }
    }
}
