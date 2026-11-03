using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class RetryPopUpUI : UIWindow
    {
        [SerializeField]
        private Button yesButton;

        [SerializeField]
        private Button cancelButton;

        private void Start()
        {
            base.Initialize();
            yesButton?.onClick.AddListener(() =>
                PauseManager.Instance?.ResponderConfirmacion(true)
            );
            cancelButton?.onClick.AddListener(() =>
                PauseManager.Instance?.ResponderConfirmacion(false)
            );
        }
    }
}
