using DG.Tweening;
using UnityEngine;

namespace TDOM.Unity.UI
{
    public class CanvasGroupFader : MonoBehaviour
    {
        private const float AlphaTransparente = 0f;
        private const float AlphaOpaco = 1f;

        [SerializeField]
        private CanvasGroup _grupo;

        private Tween _tween;

        public bool Visible => _grupo.alpha > AlphaTransparente;

        public Tween Mostrar(float duracion) => FundirA(AlphaOpaco, duracion);

        public Tween Ocultar(float duracion) => FundirA(AlphaTransparente, duracion);

        public void MostrarInstantaneo() => Fijar(AlphaOpaco);

        public void OcultarInstantaneo() => Fijar(AlphaTransparente);

        private Tween FundirA(float objetivo, float duracion)
        {
            _tween?.Kill();
            BloquearInteraccion(true);
            _tween = DOTween
                .To(() => _grupo.alpha, alpha => _grupo.alpha = alpha, objetivo, duracion)
                .SetUpdate(true)
                .OnComplete(() => BloquearInteraccion(objetivo > AlphaTransparente));
            return _tween;
        }

        private void Fijar(float alpha)
        {
            _tween?.Kill();
            _grupo.alpha = alpha;
            BloquearInteraccion(alpha > AlphaTransparente);
        }

        private void BloquearInteraccion(bool bloquear)
        {
            _grupo.blocksRaycasts = bloquear;
            _grupo.interactable = false;
        }

        protected virtual void OnDestroy()
        {
            _tween?.Kill();
        }
    }
}
