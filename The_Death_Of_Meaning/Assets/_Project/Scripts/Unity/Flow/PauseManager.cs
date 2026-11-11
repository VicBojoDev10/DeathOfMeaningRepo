using System.Collections.Generic;
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
        private const float TiempoLimiteConfirmacionFinal = 30f;
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
        private readonly HashSet<ulong> _clientesQuePuedenResponder = new();
        private ulong _secuenciaConfirmacion;
        private ulong _idConfirmacionPendiente;
        private ulong _idConfirmacionLocal;
        private float _venceConfirmacionFinalEn;

        private void Update()
        {
            if (
                IsServer
                && _confirmacionDesdePantallaFinalPendiente
                && Time.unscaledTime >= _venceConfirmacionFinalEn
            )
                CancelarConfirmacionFinal("se agotó el tiempo de espera", notificarClientes: true);

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
            if (IsServer && NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback += AlDesconectarCliente;
            AlCambiarPausa(false, Pausado.Value);
            AlCambiarResultado(ResultadoPartida.EnCurso, Resultado.Value);
        }

        public override void OnNetworkDespawn()
        {
            Pausado.OnValueChanged -= AlCambiarPausa;
            Resultado.OnValueChanged -= AlCambiarResultado;
            if (IsServer && NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= AlDesconectarCliente;
            _clientesQuePuedenResponder.Clear();
            _confirmacionDesdePantallaFinalPendiente = false;
            _idConfirmacionPendiente = 0;
            _idConfirmacionLocal = 0;
            _ventanaFinalPendiente = null;
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
            if (!IsServer || (ventanaFinal != "victoriaui" && ventanaFinal != WindowsIds.DerrotaUI))
                return;

            if (_confirmacionDesdePantallaFinalPendiente)
            {
                Debug.LogWarning(
                    $"[PauseManager] Solicitud final ignorada: ya hay una confirmación pendiente "
                        + $"(id={_idConfirmacionPendiente}, solicitante={_solicitante})."
                );
                return;
            }

            _solicitante = rpc.Receive.SenderClientId;
            _salir = salir;
            _ventanaFinalPendiente = ventanaFinal;
            _confirmacionDesdePantallaFinalPendiente = true;
            _idConfirmacionPendiente = ++_secuenciaConfirmacion;
            _venceConfirmacionFinalEn = Time.unscaledTime + TiempoLimiteConfirmacionFinal;
            _clientesQuePuedenResponder.Clear();

            foreach (var cliente in NetworkManager.ConnectedClientsIds)
            {
                if (cliente == _solicitante)
                    continue;

                _clientesQuePuedenResponder.Add(cliente);
                ConfirmacionClientRpc(
                    salir,
                    _idConfirmacionPendiente,
                    RpcTarget.Single(cliente, RpcTargetUse.Temp)
                );
            }

            if (_clientesQuePuedenResponder.Count == 0)
            {
                CancelarConfirmacionFinal(
                    "no hay otro jugador conectado",
                    notificarClientes: false
                );
                Debug.LogWarning(
                    "No hay otro jugador conectado para confirmar la acción desde la pantalla final."
                );
                return;
            }

            Debug.Log(
                $"[PauseManager] Confirmación final solicitada: id={_idConfirmacionPendiente}, "
                    + $"solicitante={_solicitante}, acción={(salir ? "salir" : "reiniciar")}, "
                    + $"respuestas={_clientesQuePuedenResponder.Count}."
            );
        }

        public void PedirConfirmacion(bool salir) => PedirConfirmacionRpc(salir);

        [Rpc(SendTo.Server)]
        private void PedirConfirmacionRpc(bool salir, RpcParams rpc = default)
        {
            if (!IsServer || !Pausado.Value)
                return;
            _solicitante = rpc.Receive.SenderClientId;
            _salir = salir;
            _idConfirmacionPendiente = ++_secuenciaConfirmacion;
            _clientesQuePuedenResponder.Clear();
            foreach (var cliente in NetworkManager.ConnectedClientsIds)
                if (cliente != _solicitante)
                {
                    _clientesQuePuedenResponder.Add(cliente);
                    ConfirmacionClientRpc(
                        salir,
                        _idConfirmacionPendiente,
                        RpcTarget.Single(cliente, RpcTargetUse.Temp)
                    );
                }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void ConfirmacionClientRpc(
            bool salir,
            ulong idConfirmacion,
            RpcParams rpc = default
        )
        {
            _idConfirmacionLocal = idConfirmacion;
            MostrarPopupLocal(salir);
        }

        public void ResponderConfirmacion(bool aceptar)
        {
            if (_idConfirmacionLocal == 0)
            {
                Debug.LogWarning(
                    "[PauseManager] Respuesta ignorada: no hay confirmación local activa."
                );
                return;
            }

            ResponderConfirmacionRpc(aceptar, _idConfirmacionLocal);
        }

        [Rpc(SendTo.Server)]
        private void ResponderConfirmacionRpc(
            bool aceptar,
            ulong idConfirmacion,
            RpcParams rpc = default
        )
        {
            bool desdePantallaFinal = _confirmacionDesdePantallaFinalPendiente;
            if (!IsServer || (!Pausado.Value && !desdePantallaFinal))
                return;

            ulong quienResponde = rpc.Receive.SenderClientId;
            if (
                idConfirmacion == 0
                || idConfirmacion != _idConfirmacionPendiente
                || quienResponde == _solicitante
                || !_clientesQuePuedenResponder.Contains(quienResponde)
            )
            {
                Debug.LogWarning(
                    $"[PauseManager] Respuesta ignorada: id={idConfirmacion}, "
                        + $"cliente={quienResponde}, idActivo={_idConfirmacionPendiente}, "
                        + $"pendienteFinal={_confirmacionDesdePantallaFinalPendiente}."
                );
                return;
            }

            Debug.Log(
                $"[PauseManager] Respuesta recibida: id={idConfirmacion}, cliente={quienResponde}, "
                    + $"aceptar={aceptar}, pantallaFinal={desdePantallaFinal}."
            );

            if (aceptar)
            {
                if (desdePantallaFinal)
                {
                    bool salirDesdePantallaFinal = _salir;
                    string ventanaFinal = _ventanaFinalPendiente;
                    LimpiarConfirmacionPendiente();
                    if (salirDesdePantallaFinal)
                        SalirAlMenu();
                    else
                        ReiniciarConCargaClientRpc(true, ventanaFinal);
                }
                else if (_salir)
                {
                    LimpiarConfirmacionPendiente();
                    SalirAlMenu();
                }
                else
                {
                    LimpiarConfirmacionPendiente();
                    RecargarNivel();
                }
            }
            else
            {
                string ventanaFinal = _ventanaFinalPendiente;
                RespuestaClientRpc(
                    _salir,
                    desdePantallaFinal,
                    ventanaFinal,
                    idConfirmacion,
                    RpcTarget.Single(quienResponde, RpcTargetUse.Temp)
                );
                LimpiarConfirmacionPendiente();
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void RespuestaClientRpc(
            bool salir,
            bool desdePantallaFinal,
            string ventanaFinal,
            ulong idConfirmacion,
            RpcParams rpc = default
        )
        {
            if (idConfirmacion != _idConfirmacionLocal)
                return;

            _idConfirmacionLocal = 0;
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

        private void AlDesconectarCliente(ulong cliente)
        {
            if (!_clientesQuePuedenResponder.Contains(cliente) && cliente != _solicitante)
                return;

            if (_confirmacionDesdePantallaFinalPendiente)
            {
                _clientesQuePuedenResponder.Remove(cliente);
                if (cliente == _solicitante || _clientesQuePuedenResponder.Count == 0)
                    CancelarConfirmacionFinal(
                        $"se desconectó el cliente {cliente}",
                        notificarClientes: true
                    );
            }
            else if (_idConfirmacionPendiente != 0)
            {
                LimpiarConfirmacionPendiente();
            }
        }

        private void CancelarConfirmacionFinal(string motivo, bool notificarClientes)
        {
            ulong id = _idConfirmacionPendiente;
            bool salir = _salir;
            string ventana = _ventanaFinalPendiente;
            if (notificarClientes)
            {
                foreach (var cliente in _clientesQuePuedenResponder)
                    CerrarPopupConfirmacionClientRpc(
                        id,
                        salir,
                        ventana,
                        RpcTarget.Single(cliente, RpcTargetUse.Temp)
                    );
            }

            Debug.LogWarning($"[PauseManager] Confirmación final cancelada (id={id}): {motivo}.");
            LimpiarConfirmacionPendiente();
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void CerrarPopupConfirmacionClientRpc(
            ulong idConfirmacion,
            bool salir,
            string ventanaFinal,
            RpcParams rpc = default
        )
        {
            if (idConfirmacion != _idConfirmacionLocal)
                return;

            _idConfirmacionLocal = 0;
            var ui = UiManager.Instance;
            ui?.CloseWindow(salir ? WindowsIds.ExitPopUpUI : WindowsIds.RetryPopUpUI);
            (ui?.GetWindow(WindowsIds.PausaUI) as PausaUI)?.SetPopupActivo(false);
            if (!string.IsNullOrEmpty(ventanaFinal))
                ui?.ActivarInteraccionSolo(ventanaFinal);
        }

        private void LimpiarConfirmacionPendiente()
        {
            _confirmacionDesdePantallaFinalPendiente = false;
            _ventanaFinalPendiente = null;
            _clientesQuePuedenResponder.Clear();
            _idConfirmacionPendiente = 0;
            _venceConfirmacionFinalEn = 0f;
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

                GlobalGameManager.Instance?.ReproducirMusicaTutorial();

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

                    GlobalGameManager.Instance?.ReproducirMusicaTutorial();
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
