using TDOM.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity.UI
{
    public enum UiThemeColorName
    {
        Tinta950,
        Tinta900,
        Tinta800,
        Tinta700,
        Acero600,
        Acero400,
        Niebla300,
        Hueso100,
        Oro300,
        Oro500,
        Oro700,
        Carmesi300,
        Carmesi500,
        Carmesi700,
        Luna300,
        Luna500,
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
                case UiThemeColorName.Tinta950:
                    return source.Tinta950;
                case UiThemeColorName.Tinta900:
                    return source.Tinta900;
                case UiThemeColorName.Tinta800:
                    return source.Tinta800;
                case UiThemeColorName.Tinta700:
                    return source.Tinta700;
                case UiThemeColorName.Acero600:
                    return source.Acero600;
                case UiThemeColorName.Acero400:
                    return source.Acero400;
                case UiThemeColorName.Niebla300:
                    return source.Niebla300;
                case UiThemeColorName.Hueso100:
                    return source.Hueso100;
                case UiThemeColorName.Oro300:
                    return source.Oro300;
                case UiThemeColorName.Oro500:
                    return source.Oro500;
                case UiThemeColorName.Oro700:
                    return source.Oro700;
                case UiThemeColorName.Carmesi300:
                    return source.Carmesi300;
                case UiThemeColorName.Carmesi500:
                    return source.Carmesi500;
                case UiThemeColorName.Carmesi700:
                    return source.Carmesi700;
                case UiThemeColorName.Luna300:
                    return source.Luna300;
                case UiThemeColorName.Luna500:
                    return source.Luna500;
                default:
                    return Color.white;
            }
        }
    }
}
