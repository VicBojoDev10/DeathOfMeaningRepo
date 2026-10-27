using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "UiTheme", menuName = "TDOM/UI/Theme")]
    public class UiTheme : ScriptableObject
    {
        [Header("Tinta")]
        public Color Tinta950;
        public Color Tinta900;
        public Color Tinta800;
        public Color Tinta700;

        [Header("Acero")]
        public Color Acero600;
        public Color Acero400;

        [Header("Niebla / Hueso")]
        public Color Niebla300;
        public Color Hueso100;

        [Header("Oro")]
        public Color Oro300;
        public Color Oro500;
        public Color Oro700;

        [Header("Carmesí")]
        public Color Carmesi300;
        public Color Carmesi500;
        public Color Carmesi700;

        [Header("Luna")]
        public Color Luna300;
        public Color Luna500;
    }
}
