using TDOM.Unity.UI;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public class PauseManager : NetworkBehaviour
    {
        public static PauseManager Instance { get; private set; }
        public static bool PausadoGlobal { get; private set; }
        public readonly NetworkVariable<bool> Pausado = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        public readonly NetworkVariable<ulong> QuienPauso = new(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        private ulong _solicitante;
        private bool _salir;

        private void Update()
        {
            var clienteLocal = NetworkManager?.LocalClient;
            if (clienteLocal == null)
                return;

            foreach (var objeto in clienteLocal.OwnedObjects)
            {
                if (
                    objeto == null
                    || !objeto.TryGetComponent(out PlayerRoot jugador)
                    || !jugador.SpawnInicialAplicado
                    || jugador.InputReader == null
                    || !jugador.InputReader.PausaPresionada
                )
                    continue;

                bool gameplayAbierto =
                    UiManager.Instance != null
                    && UiManager.Instance.GetWindow(WindowsIds.GamePlayUI)?.IsOpen == true;
                if (PausadoGlobal || gameplayAbierto)
                    SolicitarPausa(!PausadoGlobal);

                return;
            }
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            Pausado.OnValueChanged += AlCambiarPausa;
            AlCambiarPausa(false, Pausado.Value);
        }

        public override void OnNetworkDespawn()
        {
            Pausado.OnValueChanged -= AlCambiarPausa;
            if (Instance == this)
                Instance = null;
            PausadoGlobal = false;
        }

        private void AlCambiarPausa(bool anterior, bool actual)
        {
            PausadoGlobal = actual;
            var ui = UiManager.Instance;
            if (ui == null)
                return;
            if (actual)
            {
                ui.CloseWindow(WindowsIds.RetryPopUpUI);
                ui.CloseWindow(WindowsIds.ExitPopUpUI);
                ui.ShowWindow(WindowsIds.PausaUI);
                ui.ActivarInteraccionSolo(WindowsIds.PausaUI);
                var ventana = ui.GetWindow(WindowsIds.PausaUI) as PausaUI;
                ventana?.ActualizarTitulo(QuienPauso.Value);
            }
            else
            {
                ui.CloseWindow(WindowsIds.PausaUI);
                ui.CloseWindow(WindowsIds.RetryPopUpUI);
                ui.CloseWindow(WindowsIds.ExitPopUpUI);
                ui.ActivarInteraccionSolo(WindowsIds.GamePlayUI);
            }
        }

        public void SolicitarPausa(bool pausa) => SolicitarPausaRpc(pausa);

        [Rpc(SendTo.Server)]
        public void SolicitarPausaRpc(bool pausa, RpcParams rpc = default)
        {
            if (!IsServer)
                return;
            QuienPauso.Value = pausa ? rpc.Receive.SenderClientId : ulong.MaxValue;
            Pausado.Value = pausa;
        }

        public void PedirConfirmacion(bool salir) => PedirConfirmacionRpc(salir);

        [Rpc(SendTo.Server)]
        private void PedirConfirmacionRpc(bool salir, RpcParams rpc = default)
        {
            if (!IsServer || !Pausado.Value)
                return;
            _solicitante = rpc.Receive.SenderClientId;
            _salir = salir;
            foreach (var cliente in NetworkManager.ConnectedClientsIds)
                if (cliente != _solicitante)
                    ConfirmacionClientRpc(salir, RpcTarget.Single(cliente, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void ConfirmacionClientRpc(bool salir, RpcParams rpc = default)
        {
            MostrarPopupLocal(salir);
        }

        public void ResponderConfirmacion(bool aceptar) => ResponderConfirmacionRpc(aceptar);

        [Rpc(SendTo.Server)]
        private void ResponderConfirmacionRpc(bool aceptar, RpcParams rpc = default)
        {
            if (!IsServer || !Pausado.Value)
                return;
            if (aceptar)
            {
                if (_salir)
                    SalirAlMenu();
                else
                    RecargarNivel();
            }
            else
                RespuestaClientRpc(
                    _salir,
                    RpcTarget.Single(rpc.Receive.SenderClientId, RpcTargetUse.Temp)
                );
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void RespuestaClientRpc(bool salir, RpcParams rpc = default)
        {
            CerrarPopupLocal(salir);
        }

        private static void MostrarPopupLocal(bool salir)
        {
            var ui = UiManager.Instance;
            if (ui == null)
                return;

            (ui.GetWindow(WindowsIds.PausaUI) as PausaUI)?.SetPopupActivo(true);
            ui.ActivarInteraccionSolo(salir ? WindowsIds.ExitPopUpUI : WindowsIds.RetryPopUpUI);
            ui.ShowWindow(salir ? WindowsIds.ExitPopUpUI : WindowsIds.RetryPopUpUI);
        }

        private static void CerrarPopupLocal(bool salir)
        {
            var ui = UiManager.Instance;
            if (ui == null)
                return;

            ui.CloseWindow(salir ? WindowsIds.ExitPopUpUI : WindowsIds.RetryPopUpUI);
            ui.ActivarInteraccionSolo(WindowsIds.PausaUI);
            (ui.GetWindow(WindowsIds.PausaUI) as PausaUI)?.SetPopupActivo(false);
        }

        private void RecargarNivel()
        {
            ReiniciarConCargaClientRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ReiniciarConCargaClientRpc()
        {
            var transicion = UiManager.Instance?.TransitionManager;
            if (transicion == null)
            {
                if (IsServer)
                {
                    Pausado.Value = false;
                    GameSessionManager.Instance?.ReiniciarPartidaDesdePausa();
                }
                return;
            }

            transicion.EjecutarAccionConCarga(
                () =>
                {
                    if (IsServer)
                    {
                        if (GameSessionManager.Instance != null)
                            GameSessionManager.Instance.ReiniciarPartidaDesdePausa();
                        else
                            Debug.LogError(
                                "No se encontró GameSessionManager para reiniciar la partida."
                            );
                    }
                },
                () =>
                {
                    if (IsServer)
                        Pausado.Value = false;
                }
            );
        }

        private void SalirAlMenu()
        {
            GameSessionManager.Instance?.PrepararSalidaAlMenu();
            SalirAlMenuClientRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SalirAlMenuClientRpc()
        {
            var ui = UiManager.Instance;
            ui?.CloseWindow(WindowsIds.PausaUI);
            ui?.CloseWindow(WindowsIds.RetryPopUpUI);
            ui?.CloseWindow(WindowsIds.ExitPopUpUI);
            ui?.CloseWindow(WindowsIds.GamePlayUI);

            var red = NetworkManager;
            if (red != null && red.IsListening)
                red.Shutdown();

            if (ui != null)
            {
                ui.LoadScene(
                    "MainMenu",
                    onSceneLoaded: () =>
                    {
                        ui.CloseWindow(WindowsIds.ConnectionMenuUI);
                        ui.CloseWindow(WindowsIds.ChSelectionUI);
                        ui.ShowWindow(WindowsIds.MainMenuUI);
                        (ui.GetWindow(WindowsIds.MainMenuUI) as MainMenuUI)?.FocusFirstElement();
                    }
                );
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
        }
    }
}
