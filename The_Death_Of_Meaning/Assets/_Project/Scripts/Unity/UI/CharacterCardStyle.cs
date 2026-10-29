using DG.Tweening;
using UnityEngine;

namespace TDOM.Unity
{
    [CreateAssetMenu(menuName = "TDOM/UI/Character Card Style")]
    public sealed class CharacterCardStyle : ScriptableObject
    {
        public Color normalName;
        public Color selectedName;
        public Color blockedArtwork;
        public Color artworkBackgroundColor;
        public Vector3 selectedScale;
        public Vector3 lockedScale;
        public float lockedAngle;
        public Vector2 availablePosition;
        public Vector2 companionPosition;

        [Min(0f)]
        public float transitionDuration;
        public Ease transitionEase;
        public string ownTag;
        public string otherTag;
        public string availableSubtitle;
        public string selectedSubtitle;
        public string lockedSubtitle;
    }
}
