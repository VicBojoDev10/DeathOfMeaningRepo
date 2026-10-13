using UnityEngine;

namespace TDOM.Unity
{
    public class UIManagerMenu : MonoBehaviour
    {
        [Tooltip("MainMenuUI que ya está en la escena (opcional; si está vacío se busca)")]
        [SerializeField]
        private MainMenuUI _mainMenu;

        [Tooltip("Se instancia solo si la escena no tiene ningún MainMenuUI")]
        [SerializeField]
        private MainMenuUI _mainMenuPrefab;

        public MainMenuUI MainMenu => _mainMenu;

        private void Awake()
        {
            EnsureMainMenu();
        }

        private void EnsureMainMenu()
        {
            if (_mainMenu == null)
                _mainMenu = FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);

            if (_mainMenu == null && _mainMenuPrefab != null)
                _mainMenu = Instantiate(_mainMenuPrefab, transform);

            if (_mainMenu == null)
                Debug.LogError("UIManagerMenu: no hay MainMenuUI en la escena ni prefab asignado.");
        }
    }
}
