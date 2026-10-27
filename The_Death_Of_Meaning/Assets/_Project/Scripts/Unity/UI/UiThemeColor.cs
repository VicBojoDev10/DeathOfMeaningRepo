using TDOM.POCO.ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity.UI
{
    public enum UiThemeColorName
    {
        tinta950,
        tinta900,
        tinta800,
        tinta700,
        acero600,
        acero400,
        niebla300,
        hueso100,
        oro300,
        oro500,
        oro700,
        carmesi300,
        carmesi500,
        carmesi700,
        luna300,
        luna500,
    }

    [ExecuteAlways]
    public class UiThemeColor : MonoBehaviour
    {
        [SerializeField]
        private UiTheme theme;

        [SerializeField]
        private UiThemeColorName colorName;

        [SerializeField]
        private Graphic target;

        private void Awake() => ApplyColor();

        private void OnValidate() => ApplyColor();

        private void ApplyColor()
        {
            if (theme == null || target == null)
                return;

            target.color = GetColor(theme, colorName);
        }

        private static Color GetColor(UiTheme source, UiThemeColorName name)
        {
            switch (name)
            {
                case UiThemeColorName.tinta950:
                    return source.tinta950;
                case UiThemeColorName.tinta900:
                    return source.tinta900;
                case UiThemeColorName.tinta800:
                    return source.tinta800;
                case UiThemeColorName.tinta700:
                    return source.tinta700;
                case UiThemeColorName.acero600:
                    return source.acero600;
                case UiThemeColorName.acero400:
                    return source.acero400;
                case UiThemeColorName.niebla300:
                    return source.niebla300;
                case UiThemeColorName.hueso100:
                    return source.hueso100;
                case UiThemeColorName.oro300:
                    return source.oro300;
                case UiThemeColorName.oro500:
                    return source.oro500;
                case UiThemeColorName.oro700:
                    return source.oro700;
                case UiThemeColorName.carmesi300:
                    return source.carmesi300;
                case UiThemeColorName.carmesi500:
                    return source.carmesi500;
                case UiThemeColorName.carmesi700:
                    return source.carmesi700;
                case UiThemeColorName.luna300:
                    return source.luna300;
                case UiThemeColorName.luna500:
                    return source.luna500;
                default:
                    return Color.white;
            }
        }
    }
}