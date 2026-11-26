using System;
using System.Collections;
using TDOM.Gameplay.Core;
using TDOM.Unity;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using TDOM.Unity.UI;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace TDOM.Unity.Flow
{
    public sealed class ArenaBossPresentation : NetworkBehaviour
    {
        [SerializeField]
        private PlayableDirector _timeline;

        [SerializeField]
        private CinematicaVideo _video;

        [SerializeField]
        private Transform _entradaZendre;

        [SerializeField]
        private Transform _entradaAyla;

        [SerializeField, Min(0f)]
        private float _momentoRevelado = 1.5f;

        [SerializeField, Min(0f)]
        private float _margenEsperaBlend = 0.5f;

        [SerializeField, Min(0f)]
        private float _duracionRegresoCamara = 2.25f;

        [SerializeField]
        private bool _permitirSaltarEnEditor = true;

        private const double DuracionTimelineAusente = 4d;

        private BossController _jefe;
        private bool _revelado;
        private bool _terminada;
        private bool _suscrito;
        private bool _suscritoRevelado;
        private bool _presentacionIniciada;
        private bool _timelineIniciado;
        private double _inicioRed;

        public override void OnNetworkSpawn()
        {
            IntentarSuscribirEncuentro();
        }

        public override void OnNetworkDespawn()
        {
            if (ArenaEncounterNetwork.Instance != null)
            {
                ArenaEncounterNetwork.Instance.Fase.OnValueChanged -= AlCambiarFase;
                ArenaEncounterNetwork.Instance.JefeRevelado -= AlRevelarJefe;
            }
            _suscrito = false;
            _suscritoRevelado = false;
            _presentacionIniciada = false;
            BossController.DejarDeObservarJefe(AlSpawnearJefe);
            if (_jefe != null && IsServer)
                _jefe.Pausar(false);
            GameplayLocal()?.SetHudVisible(true);
        }

        private void AlCambiarFase(ArenaEncounterPhase anterior, ArenaEncounterPhase actual)
        {
            if (actual == ArenaEncounterPhase.Esperando)
            {
                _presentacionIniciada = false;
                _terminada = false;
                return;
            }
            if (actual == ArenaEncounterPhase.Presentacion)
                SolicitarInicioSincronizado();
        }

        private void IntentarSuscribirEncuentro()
        {
            var encuentro = ArenaEncounterNetwork.Instance;
            if (encuentro == null || !encuentro.IsSpawned)
                return;
            if (!_suscrito)
            {
                encuentro.Fase.OnValueChanged += AlCambiarFase;
                _suscrito = true;
            }
            if (!_suscritoRevelado)
            {
                encuentro.JefeRevelado += AlRevelarJefe;
                _suscritoRevelado = true;
            }
            if (encuentro.Fase.Value == ArenaEncounterPhase.Presentacion)
                SolicitarInicioSincronizado();
        }

        private void SolicitarInicioSincronizado()
        {
            if (_presentacionIniciada)
                return;
            _presentacionIniciada = true;
            if (IsServer)
                IniciarRpc(NetworkManager.ServerTime.Time);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void IniciarRpc(double horaInicio)
        {
            _presentacionIniciada = true;
            _inicioRed = horaInicio;
            _revelado = false;
            _terminada = false;
            _timelineIniciado = false;
            var jugador = JugadorLocal();
            jugador?.SetControlCinematica(true);
            jugador?.SetCamaraCedida(true);
            GameplayLocal()?.SetHudVisible(false, false);
            if (_video != null && _video.Duracion > 0d)
            {
                GlobalGameManager.Instance?.DetenerMusicaPartida();
                _video.Reproducir(Math.Max(0d, NetworkManager.ServerTime.Time - horaInicio));
            }
            if (IsServer)
            {
                BossController.ObservarJefe(NetworkManager, AlSpawnearJefe);
                _jefe?.Pausar(true);
            }
        }

        private void Update()
        {
            if (!IsSpawned)
                return;
            IntentarSuscribirEncuentro();
            if (ArenaEncounterNetwork.Instance == null)
                return;
            if (ArenaEncounterNetwork.Instance.Fase.Value == ArenaEncounterPhase.Esperando)
            {
                _presentacionIniciada = false;
                _terminada = false;
                return;
            }
            if (
                ArenaEncounterNetwork.Instance.Fase.Value != ArenaEncounterPhase.Presentacion
                || _terminada
            )
                return;

            double transcurrido = NetworkManager.ServerTime.Time - _inicioRed;
            var linea = CrearLinea();
            var etapa = linea.Etapa(transcurrido);
            if (etapa != EtapaPresentacionArena.Video && !_timelineIniciado)
                IniciarTimeline(linea.TiempoTimeline(transcurrido));
            if (!_revelado && linea.DebeRevelarJefe(transcurrido))
            {
                _revelado = true;
                if (IsServer)
                    ArenaEncounterNetwork.Instance.NotificarJefeRevelado();
            }
            // Inicia la mezcla mientras la cámara del Timeline todavía conserva el último plano.
            if (InputSaltar() || etapa == EtapaPresentacionArena.Regreso)
            {
                if (IsServer)
                    FinalizarRpc();
            }
        }

        private LineaPresentacionArena CrearLinea() =>
            new LineaPresentacionArena(
                _video != null ? _video.Duracion : 0d,
                _timeline != null ? _timeline.duration : DuracionTimelineAusente,
                _momentoRevelado,
                _duracionRegresoCamara
            );

        private void IniciarTimeline(double tiempo)
        {
            _timelineIniciado = true;
            _video?.Desvanecer();
            GameplayLocal()?.SetHudVisible(false, true);
            GlobalGameManager.Instance?.ReproducirMusicaCombateInmediata();
            if (IsServer)
                LlevarJugadoresALaArena();
            if (_timeline == null)
            {
                Debug.LogError(
                    "[ArenaBossPresentation] Falta asignar PlayableDirector del Timeline de entrada."
                );
                return;
            }
            _timeline.time = Math.Max(0d, tiempo);
            _timeline.extrapolationMode = DirectorWrapMode.Hold;
            _timeline.Play();
            Debug.Log("[ArenaBossPresentation] Timeline Entering_Bossfight iniciado.");
        }

        private void LlevarJugadoresALaArena()
        {
            foreach (var cliente in NetworkManager.ConnectedClientsList)
            foreach (var objeto in cliente.OwnedObjects)
            {
                if (!objeto.TryGetComponent(out PlayerRoot jugador))
                    continue;
                var entrada =
                    jugador.CharacterId == CharacterIds.Zendre ? _entradaZendre : _entradaAyla;
                if (entrada != null)
                    jugador.EnviarSpawnInicial(entrada.position, entrada.rotation);
            }
        }

        private bool InputSaltar()
        {
#if UNITY_EDITOR
            return IsServer
                && _permitirSaltarEnEditor
                && Keyboard.current != null
                && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            return false;
#endif
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void FinalizarRpc()
        {
            if (_terminada)
                return;
            _terminada = true;
            if (!_timelineIniciado)
                IniciarTimeline(0d);
            StartCoroutine(DevolverCamaraYControl());
        }

        private IEnumerator DevolverCamaraYControl()
        {
            var jugador = JugadorLocal();
            var camaraJugador = jugador != null ? jugador.CamaraCinematica : null;
            if (camaraJugador != null)
                camaraJugador.Priority.Value = 100;
            if (_timeline != null)
                _timeline.Stop();

            yield return null;
            var cerebro =
                camaraJugador != null
                    ? CinemachineCore.FindPotentialTargetBrain(camaraJugador)
                    : null;
            if (cerebro != null)
            {
                float limite = cerebro.DefaultBlend.Time + _margenEsperaBlend;
                float transcurrido = 0f;
                while (cerebro.IsBlending && transcurrido < limite)
                {
                    transcurrido += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            if (camaraJugador != null)
                camaraJugador.Priority.Value = jugador.PrioridadBaseCamara;
            jugador?.SetControlCinematica(false);
            GameplayLocal()?.SetHudVisible(true);
            if (IsServer)
            {
                _jefe?.Pausar(false);
                ArenaEncounterNetwork.Instance?.IniciarCombate();
            }
        }

        private void AlSpawnearJefe(BossController jefe)
        {
            _jefe = jefe;
            if (
                IsServer
                && ArenaEncounterNetwork.Instance != null
                && ArenaEncounterNetwork.Instance.Fase.Value == ArenaEncounterPhase.Presentacion
            )
                _jefe.Pausar(true);
        }

        private void AlRevelarJefe() => GameplayLocal()?.SetHudVisible(false, true);

        private IGameplayHud GameplayLocal() =>
            UiManager.Instance?.GetGameplayWindow() as IGameplayHud;

        private PlayerRoot JugadorLocal()
        {
            if (NetworkManager?.LocalClient == null)
                return null;
            foreach (var objeto in NetworkManager.LocalClient.OwnedObjects)
                if (objeto.TryGetComponent(out PlayerRoot jugador))
                    return jugador;
            return null;
        }
    }
}
