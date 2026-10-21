using UnityEngine;

namespace TDOM.Unity.Combat
{
    public class PlayerCombatAnimator : MonoBehaviour
    {
        private static readonly int ComboIndexHash = Animator.StringToHash("ComboIndex");
        private static readonly int ChargedHash = Animator.StringToHash("Charged");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int LightOneHash = Animator.StringToHash("Attack");
        private static readonly int LightTwoHash = Animator.StringToHash("1i");
        private static readonly int LightThreeHash = Animator.StringToHash("2IC");
        private static readonly int ChargedStateHash = Animator.StringToHash("Charged");

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
            if (!_hasPendingCombo || _animator == null || DebeEsperarFinDelGolpe())
                return;

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
            return stateHash == LightOneHash
                || stateHash == LightTwoHash
                || stateHash == LightThreeHash
                || stateHash == ChargedStateHash;
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
    }
}
