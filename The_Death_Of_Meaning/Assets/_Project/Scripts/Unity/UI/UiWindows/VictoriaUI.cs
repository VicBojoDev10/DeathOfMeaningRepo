using TDOM.Unity.UI;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using TDOM.Unity.Player;

namespace TDOM.Unity
{
    public class VictoriaUI : UIWindow
    {
        

        [SerializeField]
        private Button jugarDenuevoButton;

        [SerializeField]
        private Button salirMenuButton;

        private void Awake()
        {
            if (firstSelectedObject == null && jugarDenuevoButton != null)
            {
                firstSelectedObject = jugarDenuevoButton.gameObject;
            }
        }

        private void OnEnable()
        {
            if (jugarDenuevoButton != null)
                jugarDenuevoButton.onClick.AddListener(OnJugarDeNuevoClicked);


           // if (salirMenuButton != null)
             //   salirMenuButton.onClick.AddListener(OnSalirMenuClicked);
        }

        private void OnDisable()
        {

            if (jugarDenuevoButton != null)
                jugarDenuevoButton.onClick.RemoveListener(OnJugarDeNuevoClicked);


            //if (salirMenuButton != null)
              //  salirMenuButton.onClick.RemoveListener(OnSalirMenuClicked);
        }





        private void OnJugarDeNuevoClicked()
        {
            var gameFlow = GameFlowNetwork.Instance;

            if (gameFlow == null || !gameFlow.IsSpawned)
            {
                Debug.LogError(
                    "No hay un GameFlowNetwork activo para solicitar el reinicio."
                );
                return;
            }

            Debug.Log("[VictoriaUI] Solicitando reinicio al servidor.");
            gameFlow.SolicitarReinicioDePartida();
        }
    }
    
}
