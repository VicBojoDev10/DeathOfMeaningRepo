using TDOM.Unity.Player;
using TDOM.Unity.UI;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public enum ResultadoPartida : byte
    {
        EnCurso,
        Victoria,
        Derrota,
    }

    public class PauseManager : NetworkBehaviour
    {
        public static PauseManager Instance { get; private set; }
        public static bool PausadoGlobal { get; private set; }
        public static ResultadoPartida ResultadoGlobal { get; private set; }
        public readonly NetworkVariable<bool> Pausado = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        public readonly NetworkVariable<ResultadoPartida> Resultado = new(
            ResultadoPartida.EnCurso,
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
        private bool _confirmacionDesdePantallaFinalPendiente;
        private string _ventanaFinalPendiente;

        private void Update()
        {
            var clienteLocal = NetworkManager?.LocalClient;
            if (clienteLocal == null || (ResultadoGlobal != ResultadoPartida.EnCurso))
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
            Resultado.OnValueChanged += AlCambiarResultado;
            AlCambiarPausa(false, Pausado.Value);
            AlCambiarResultado(ResultadoPartida.EnCurso, Resultado.Value);
        }

        public override void OnNetworkDespawn()
        {
            Pausado.OnValueChanged -= AlCambiarPausa;
            Resultado.OnValueChanged -= AlCambiarResultado;
            if (Instance == this)
                Instance = null;
            PausadoGlobal = false;
            ResultadoGlobal = ResultadoPartida.EnCurso;
        }

        private void AlCambiarResultado(ResultadoPartida anterior, ResultadoPartida actual)
        {
            ResultadoGlobal = actual;
        }

        public void EstablecerResultadoDesdeServidor(ResultadoPartida resultado)
        {
            if (IsServer)
                Resultado.Value = resultado;
        }

        public void RestablecerResultadoDesdeServidor()
        {
            if (IsServer)
                Resultado.Value = ResultadoPartida.EnCurso;
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

        public void PedirConfirmacionReinicioVictoria() =>
            PedirConfirmacionDesdePantallaFinalRpc(false, "victoriaui");

        public void PedirConfirmacionSalirVictoria() =>
            PedirConfirmacionDesdePantallaFinalRpc(true, "victoriaui");

        public void PedirConfirmacionReinicioDerrota() =>
            PedirConfirmacionDesdePantallaFinalRpc(false, WindowsIds.DerrotaUI);

        public void PedirConfirmacionSalirDerrota() =>
            PedirConfirmacionDesdePantallaFinalRpc(true, WindowsIds.DerrotaUI);

        [Rpc(SendTo.Server)]
        private void PedirConfirmacionDesdePantallaFinalRpc(
            bool salir,
            string ventanaFinal,
            RpcParams rpc = default
        )
        {
            if (
                !IsServer
                || _confirmacionDesdePantallaFinalPendiente
                || (ventanaFinal != "victoriaui" && ventanaFinal != WindowsIds.DerrotaUI)
            )
                return;

            _solicitante = rpc.Receive.SenderClientId;
            _salir = salir;
            _ventanaFinalPendiente = ventanaFinal;
            _confirmacionDesdePantallaFinalPendiente = true;

            bool hayOtroJugador = false;
            foreach (var cliente in NetworkManager.ConnectedClientsIds)
            {
                if (cliente == _solicitante)
                    continue;

                hayOtroJugador = true;
                ConfirmacionClientRpc(salir, RpcTarget.Single(cliente, RpcTargetUse.Temp));
            }

            if (!hayOtroJugador)
            {
                _confirmacionDesdePantallaFinalPendiente = false;
                _ventanaFinalPendiente = null;
                Debug.LogWarning(
                    "No hay otro jugador conectado para confirmar la acción desde la pantalla final."
                );
            }
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
            bool desdePantallaFinal = _confirmacionDesdePantallaFinalPendiente;
            if (!IsServer || (!Pausado.Value && !desdePantallaFinal))
                return;

            ulong quienResponde = rpc.Receive.SenderClientId;
            if (quienResponde == _solicitante)
                return;

            if (aceptar)
            {
                if (desdePantallaFinal)
                {
                    bool salirDesdePantallaFinal = _salir;
                    string ventanaFinal = _ventanaFinalPendiente;
                    _confirmacionDesdePantallaFinalPendiente = false;
                    _ventanaFinalPendiente = null;
                    if (salirDesdePantallaFinal)
                        SalirAlMenu();
                    else
                        ReiniciarConCargaClientRpc(true, ventanaFinal);
                }
                else if (_salir)
                    SalirAlMenu();
                else
                    RecargarNivel();
            }
            else
            {
                if (desdePantallaFinal)
                    _confirmacionDesdePantallaFinalPendiente = false;
                RespuestaClientRpc(
                    _salir,
                    desdePantallaFinal,
                    _ventanaFinalPendiente,
                    RpcTarget.Single(quienResponde, RpcTargetUse.Temp)
                );
                if (desdePantallaFinal)
                    _ventanaFinalPendiente = null;
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void RespuestaClientRpc(
            bool salir,
            bool desdePantallaFinal,
            string ventanaFinal,
            RpcParams rpc = default
        )
        {
            if (desdePantallaFinal)
            {
                var ui = UiManager.Instance;
                ui?.CloseWindow(salir ? WindowsIds.ExitPopUpUI : WindowsIds.RetryPopUpUI);
                (ui?.GetWindow(WindowsIds.PausaUI) as PausaUI)?.SetPopupActivo(false);
                ui?.ActivarInteraccionSolo(ventanaFinal);
                return;
            }

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
            ReiniciarConCargaClientRpc(false, string.Empty);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ReiniciarConCargaClientRpc(bool desdePantallaFinal, string ventanaFinal)
        {
            var transicion = UiManager.Instance?.TransitionManager;
            if (transicion == null)
            {
                if (IsServer)
                {
                    if (!desdePantallaFinal)
                        Pausado.Value = false;

                    GameSessionManager.Instance?.ReiniciarPartidaDesdePausa();
                }

                if (desdePantallaFinal)
                    MostrarGameplayDespuesDePantallaFinal(ventanaFinal);

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

                    if (desdePantallaFinal)
                        MostrarGameplayDespuesDePantallaFinal(ventanaFinal);
                },
                () =>
                {
                    if (IsServer && !desdePantallaFinal)
                        Pausado.Value = false;
                }
            );
        }

        private static void MostrarGameplayDespuesDePantallaFinal(string ventanaFinal)
        {
            var ui = UiManager.Instance;
            ui?.CloseWindow(WindowsIds.RetryPopUpUI);
            (ui?.GetWindow(WindowsIds.PausaUI) as PausaUI)?.SetPopupActivo(false);
            if (!string.IsNullOrEmpty(ventanaFinal))
                ui?.CloseWindow(ventanaFinal);
            ui?.CloseWindow("victoriaui");
            ui?.CloseWindow(WindowsIds.DerrotaUI);
            ui?.ShowWindow(WindowsIds.GamePlayUI);
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
            ui?.CloseWindow("victoriaui");
            ui?.CloseWindow(WindowsIds.DerrotaUI);

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
