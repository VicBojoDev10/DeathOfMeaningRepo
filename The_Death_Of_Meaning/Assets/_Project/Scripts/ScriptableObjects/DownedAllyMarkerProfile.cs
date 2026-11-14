using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(
        fileName = "DownedAllyMarkerProfile",
        menuName = "TDOM/UI/Downed Ally Marker Profile"
    )]
    public sealed class DownedAllyMarkerProfile : ScriptableObject
    {
        [Header("Holder")]
        public Vector2 HolderSize = new Vector2(280f, 72f);

        [Header("Screen layout")]
        [Min(0f)]
        public float VisibleScreenMargin = 12f;

        [Min(0f)]
        public float OffscreenEdgeInset = 48f;
    }
}
