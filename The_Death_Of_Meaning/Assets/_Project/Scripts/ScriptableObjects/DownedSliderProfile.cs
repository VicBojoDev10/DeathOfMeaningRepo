using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(menuName = "TDOM/UI/Downed Slider Profile")]
    public sealed class DownedSliderProfile : ScriptableObject
    {
        public Vector3 Offset = new Vector3(0f, 2.2f, 0f);
        public Color ColorAbatido = new Color(1f, 0.35f, 0.1f, 1f);
        public Color ColorReviviendo = new Color(0.2f, 0.95f, 0.5f, 1f);
    }
}
