using UnityEngine;

namespace TDOM.Unity.Combat
{
    public class PlayerCombatAnimator : MonoBehaviour
    {
        private static readonly int ComboIndexHash = Animator.StringToHash("ComboIndex");
        private static readonly int ChargedHash = Animator.StringToHash("Charged");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int LightTwoHash = Animator.StringToHash("1i");
        private static readonly int LightThreeHash = Animator.StringToHash("2IC");

        private static readonly int ShootHash = Animator.StringToHash("Shoot");
        private static readonly int ShootFullPathHash = Animator.StringToHash("Base Layer.Shoot");

        [SerializeField]
        private Animator _animator;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("Punto del clip actual en el que puede entrar el siguiente golpe del combo.")]
        private float _comboTransitionNormalizedTime = 0.65f;

        private bool _hasPendingCombo;
        private int _pendingComboIndex;
        private bool _pendingCharged;

        private void Update()
        {
            if (_animator == null)
                return;

            if (_animator.GetBool(ShootHash))
            {
                AnimatorStateInfo currentState = _animator.GetCurrentAnimatorStateInfo(0);
                if (currentState.shortNameHash == ShootHash && currentState.normalizedTime >= 1f)
                    _animator.SetBool(ShootHash, false);
            }

            if (_hasPendingCombo && !DebeEsperarFinDelGolpe())
                ReproducirPendiente();
        }

        public void PlayCombo(int comboIndex, bool charged)
        {
            if (_animator == null)
                return;

            _pendingComboIndex = comboIndex;
            _pendingCharged = charged;
            _hasPendingCombo = true;

            if (!DebeEsperarFinDelGolpe())
                ReproducirPendiente();
        }

        private bool DebeEsperarFinDelGolpe()
        {
            if (_animator == null || _animator.layerCount == 0)
                return false;

            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            if (
                _animator.IsInTransition(0)
                && EsEstadoAtaque(_animator.GetNextAnimatorStateInfo(0).shortNameHash)
            )
            {
                state = _animator.GetNextAnimatorStateInfo(0);
            }

            return EsEstadoAtaque(state.shortNameHash)
                && state.normalizedTime < _comboTransitionNormalizedTime;
        }

        private static bool EsEstadoAtaque(int stateHash)
        {
            return stateHash == AttackHash
                || stateHash == LightTwoHash
                || stateHash == LightThreeHash
                || stateHash == ChargedHash;
        }

        private void ReproducirPendiente()
        {
            int comboIndex = _pendingComboIndex;
            bool charged = _pendingCharged;
            _hasPendingCombo = false;

            _animator.SetInteger(ComboIndexHash, comboIndex);
            _animator.SetBool(ChargedHash, charged);
            _animator.ResetTrigger(AttackHash);
            _animator.SetTrigger(AttackHash);
        }

        public void PlayShot(int comboIndex, bool charged)
        {
            if (_animator == null)
                return;

            _animator.SetInteger("ComboIndex", comboIndex);
            _animator.SetBool("Charged", charged);
            _animator.SetBool(ShootHash, true);

            var currentState = _animator.GetCurrentAnimatorStateInfo(0);
            bool shootIsActive = currentState.shortNameHash == ShootHash;
            bool shootIsIncoming =
                _animator.IsInTransition(0)
                && _animator.GetNextAnimatorStateInfo(0).shortNameHash == ShootHash;

            if (shootIsActive || shootIsIncoming)
                _animator.Play(ShootFullPathHash, 0, 0f);
        }
    }
}
