using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class VolumeUI : UIWindow
    {
        [SerializeField] private Slider generalVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Button backButton;
    }
}
