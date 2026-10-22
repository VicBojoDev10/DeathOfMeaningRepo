using UnityEngine;

namespace TDOM.Unity.Combat
{
    public class PlayerCombatAnimator : MonoBehaviour
    {
        private static readonly int ShootHash = Animator.StringToHash("Shoot");
        private static readonly int ShootStateHash = Animator.StringToHash("Shoot");
        private static readonly int ShootFullPathHash = Animator.StringToHash("Base Layer.Shoot");

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

        public void PlayShot(int comboIndex, bool charged)
        {
            if (_animator == null)
                return;

            _animator.SetInteger("ComboIndex", comboIndex);
            _animator.SetBool("Charged", charged);
            _animator.SetBool(ShootHash, true);

            var currentState = _animator.GetCurrentAnimatorStateInfo(0);
            bool shootIsActive = currentState.shortNameHash == ShootStateHash;
            bool shootIsIncoming =
                _animator.IsInTransition(0)
                && _animator.GetNextAnimatorStateInfo(0).shortNameHash == ShootStateHash;

            if (shootIsActive || shootIsIncoming)
                _animator.Play(ShootFullPathHash, 0, 0f);
        }

        private void Update()
        {
            if (_animator == null || !_animator.GetBool(ShootHash))
                return;

            var currentState = _animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.shortNameHash == ShootStateHash && currentState.normalizedTime >= 1f)
                _animator.SetBool(ShootHash, false);
        }
    }
}
