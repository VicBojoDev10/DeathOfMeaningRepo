using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public sealed class CharacterCardUI
        : MonoBehaviour,
            IPointerEnterHandler,
            IPointerExitHandler,
            ISelectHandler,
            IDeselectHandler
    {
        public Image frame;
        public Outline focusOutline;
        public Graphic artwork;
        public Graphic artworkBackground;
        public Image lockIcon;
        public GameObject tag;
        public TMP_Text tagText;
        public TMP_Text subtitle;
        public TMP_Text characterName;
        public Sprite freeFrame;
        public Sprite hoverFrame;
        public Material grayscaleMaterial;
        public event System.Action<CharacterCardUI> FocusRequested;
        private bool _previewed;
        private bool _hasPreview;
        private bool _hasSelection;
        private bool _mine;
        private bool _blocked;
        private bool _hovered;
        private bool _focused;
        private Sequence _layoutTween;
        private bool _layoutInitialized;
        private Vector2 _targetPosition;
        private Vector3 _targetScale;
        private float _targetAngle;
        public Sprite selectedFrame;
        public Sprite lockedFrame;
        public CharacterCardStyle style;
        public bool isLeftCard;

        public void Presentar(bool mine, bool blocked, bool hasSelection, int otherPlayerNumber)
        {
            _hasSelection = hasSelection;
            _mine = mine;
            _blocked = blocked;
            frame.overrideSprite = null;
            frame.sprite =
                mine ? selectedFrame
                : blocked ? lockedFrame
                : freeFrame;
            artwork.color = blocked ? style.blockedArtwork : Color.white;
            if (artworkBackground != null)
                artworkBackground.color =
                    style.artworkBackgroundColor * (blocked ? style.blockedArtwork : Color.white);
            artwork.material = blocked ? grayscaleMaterial : null;
            lockIcon.gameObject.SetActive(blocked);
            tag.SetActive(mine || blocked);
            tag.GetComponent<Image>().material = blocked ? grayscaleMaterial : null;
            tagText.text = mine ? style.ownTag : string.Format(style.otherTag, otherPlayerNumber);
            tagText.color = mine ? style.selectedName : style.normalName;
            characterName.color = mine ? style.selectedName : style.normalName;
            subtitle.text =
                mine ? style.selectedSubtitle
                : blocked ? string.Format(style.lockedSubtitle, otherPlayerNumber)
                : style.availableSubtitle;
            ActualizarDisposicionActual();
            ActualizarMarco();
        }

        public void Previsualizar(bool focused)
        {
            _hasPreview = true;
            _previewed = focused;
            ActualizarDisposicionActual();
        }

        private void ActualizarDisposicionActual()
        {
            bool centered = _hasPreview ? _previewed : _mine;
            bool hasFocus = _hasPreview || _hasSelection;
            var scale =
                centered ? style.selectedScale
                : hasFocus ? style.lockedScale
                : Vector3.one;
            var angle = !centered && hasFocus ? style.lockedAngle : 0f;
            var position = hasFocus ? style.companionPosition : style.availablePosition;
            if (isLeftCard)
                position.x = -position.x;
            ActualizarDisposicion(centered ? Vector2.zero : position, scale, angle);
        }

        private void ActualizarDisposicion(Vector2 position, Vector3 scale, float angle)
        {
            if (
                _layoutInitialized
                && _targetPosition == position
                && _targetScale == scale
                && _targetAngle == angle
            )
                return;
            _layoutTween?.Kill();
            _layoutTween = null;
            _targetPosition = position;
            _targetScale = scale;
            _targetAngle = angle;
            var rect = (RectTransform)transform;
            var rotation = new Vector3(0f, 0f, angle);
            bool animate =
                _layoutInitialized
                && Application.isPlaying
                && isActiveAndEnabled
                && style.transitionDuration > 0f;
            _layoutInitialized = true;
            if (!animate)
            {
                rect.anchoredPosition = position;
                transform.localScale = scale;
                transform.localRotation = Quaternion.Euler(rotation);
                return;
            }
            _layoutTween = DOTween
                .Sequence()
                .Join(
                    DOTween
                        .To(
                            () => rect.anchoredPosition,
                            value => rect.anchoredPosition = value,
                            position,
                            style.transitionDuration
                        )
                        .SetEase(style.transitionEase)
                )
                .Join(
                    transform.DOScale(scale, style.transitionDuration).SetEase(style.transitionEase)
                )
                .Join(
                    transform
                        .DOLocalRotate(rotation, style.transitionDuration)
                        .SetEase(style.transitionEase)
                )
                .SetUpdate(true)
                .SetTarget(this)
                .OnKill(() => _layoutTween = null);
        }

        public void OnPointerEnter(PointerEventData _)
        {
            _hovered = true;
            FocusRequested?.Invoke(this);
            ActualizarMarco();
        }

        public void OnPointerExit(PointerEventData _)
        {
            _hovered = false;
            ActualizarMarco();
        }

        public void OnSelect(BaseEventData _)
        {
            _focused = true;
            FocusRequested?.Invoke(this);
            ActualizarMarco();
        }

        public void OnDeselect(BaseEventData _)
        {
            _focused = false;
            ActualizarMarco();
        }

        private void OnDisable()
        {
            _layoutTween?.Kill(true);
            _layoutTween = null;
            _layoutInitialized = false;
            _hasPreview = false;
            _previewed = false;
            _hovered = false;
            _focused = false;
        }

        private void ActualizarMarco()
        {
            if (focusOutline != null)
                focusOutline.enabled = !_blocked && (_mine || _hovered || _focused);
            frame.sprite =
                _mine ? selectedFrame
                : _blocked ? lockedFrame
                : _hovered || _focused ? hoverFrame
                : freeFrame;
        }
    }
}
