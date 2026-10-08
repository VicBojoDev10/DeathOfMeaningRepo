using TDOM.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class MainMenuUI : UIWindow
    {
        [SerializeField]
        private Canvas _splashCanvas;
        [SerializeField]
        private Canvas _MainMenuCanvas;
        [SerializeField]
        private Canvas _optionsCanvas;

        [SerializeField]
        private Button _continueButton;

        [SerializeField]
        private Button _playButton;

        [SerializeField]
        private Button _optionsButton;

        [SerializeField]
        private Button _exitButton;

        private void Awake()
        {
            
        }
        public override void Initialize()
        {
            base.Initialize();
        }

    }
}
