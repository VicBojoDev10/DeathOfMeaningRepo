using System.Collections;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public class CombatDebugFeedback : MonoBehaviour
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Color _idleColor = Color.white;
        [SerializeField] private Color _activeColor = Color.red;

        private Coroutine _routine;

        private void Awake()
        {
            SetColor(_idleColor);
        }

        public void FlashActive(float duration)
        {
            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(FlashRoutine(duration));
        }

        private IEnumerator FlashRoutine(float duration)
        {
            SetColor(_activeColor);
            yield return new WaitForSeconds(duration);
            SetColor(_idleColor);
            _routine = null;
        }

        private void SetColor(Color color)
        {
            if (_renderer != null)
                _renderer.material.color = color;
        }
    }
}
