using System.Collections;
using UnityEngine;

namespace TDOM.Unity
{
    public class MinionAnimationController : MonoBehaviour
    {
        private static readonly int IdMove = Animator.StringToHash("Move");
        private static readonly int IdAttack = Animator.StringToHash("Attack");
        private static readonly int IdEstadoAtaque = Animator.StringToHash("Attack");
        private const float TiempoMaximoEntradaAtaque = 0.5f;

        private Animator _animator;
        private Coroutine _solicitudAtaque;
        private float _velocidadAnimatorOriginal = 1f;
        private bool _pausadoGlobal;

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>(true);
            if (_animator == null)
            {
                Debug.LogError(
                    "[MinionAnimationController] No se encontró un Animator en el prefab.",
                    this
                );
                enabled = false;
                return;
            }

            _velocidadAnimatorOriginal = _animator.speed;

            bool tieneMove = false;
            bool tieneAttack = false;
            foreach (AnimatorControllerParameter parametro in _animator.parameters)
            {
                if (parametro.nameHash == IdMove)
                    tieneMove = parametro.type == AnimatorControllerParameterType.Bool;
                else if (parametro.nameHash == IdAttack)
                    tieneAttack = parametro.type == AnimatorControllerParameterType.Bool;
            }

            if (!tieneMove || !tieneAttack)
            {
                Debug.LogError(
                    "[MinionAnimationController] El Animator requiere los parámetros Bool Move y Attack.",
                    this
                );
                enabled = false;
            }
        }

        public void ActualizarMovimiento(bool moviendose)
        {
            if (_animator != null && enabled)
                _animator.SetBool(IdMove, moviendose);
        }

        public void EstablecerPausa(bool pausado)
        {
            _pausadoGlobal = pausado;
            if (_animator == null)
                return;

            if (pausado)
            {
                _animator.SetBool(IdMove, false);
                _animator.speed = 0f;
            }
            else
            {
                _animator.speed = _velocidadAnimatorOriginal;
            }
        }

        /// <summary>
        /// Activa Attack hasta que el Animator haya empezado la transición al estado homónimo.
        /// Así el Bool no se pierde antes de que el Animator evalúe la transición.
        /// </summary>
        public void ReproducirAtaque()
        {
            if (_animator == null || !enabled || _solicitudAtaque != null)
                return;

            _animator.SetBool(IdAttack, true);
            _solicitudAtaque = StartCoroutine(EsperarEntradaAtaque());
        }

        private IEnumerator EsperarEntradaAtaque()
        {
            float tiempo = 0f;
            while (tiempo < TiempoMaximoEntradaAtaque)
            {
                // Deja que el Animator procese el parámetro al menos una vez.
                yield return null;
                if (_pausadoGlobal)
                    continue;

                tiempo += Time.deltaTime;

                AnimatorStateInfo actual = _animator.GetCurrentAnimatorStateInfo(0);
                bool entroAlAtaque = actual.shortNameHash == IdEstadoAtaque;
                if (_animator.IsInTransition(0))
                {
                    AnimatorStateInfo siguiente = _animator.GetNextAnimatorStateInfo(0);
                    entroAlAtaque |= siguiente.shortNameHash == IdEstadoAtaque;
                }

                if (entroAlAtaque)
                    break;
            }

            _animator.SetBool(IdAttack, false);
            _solicitudAtaque = null;
        }
    }
}
