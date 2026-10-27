using System;
using UnityEngine;

namespace TDOM.POCO.ScriptableObjects
{
    [CreateAssetMenu(fileName = "UiTheme", menuName = "TDOM/UI/Theme")]
    public class UiTheme : ScriptableObject
    {
        [Header("Tinta")]
        public Color tinta950;
        public Color tinta900;
        public Color tinta800;
        public Color tinta700;

        [Header("Acero")]
        public Color acero600;
        public Color acero400;

        [Header("Niebla / Hueso")]
        public Color niebla300;
        public Color hueso100;

        [Header("Oro")]
        public Color oro300;
        public Color oro500;
        public Color oro700;

        [Header("Carmesí")]
        public Color carmesi300;
        public Color carmesi500;
        public Color carmesi700;

        [Header("Luna")]
        public Color luna300;
        public Color luna500;
    }
}
