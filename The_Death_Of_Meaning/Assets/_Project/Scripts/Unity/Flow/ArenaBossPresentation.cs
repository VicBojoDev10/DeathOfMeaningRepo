using System;
using System.Collections;
using TDOM.Unity;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using Unity.Cinemachine;

namespace TDOM.Unity.Flow
{
    public sealed class ArenaBossPresentation : NetworkBehaviour
    {
        [SerializeField] private PlayableDirector _timeline;
        [SerializeField, Min(0f)] private float _momentoRevelado = 1.5f;
        [SerializeField, Min(0f)] private float _margenEsperaBlend = 0.5f;
        [SerializeField, Min(0f)] private float _duracionRegresoCamara = 2.25f;
        [SerializeField] private bool _permitirSaltarEnEditor = true;

        private BossController _jefe;
        private bool _revelado;
        private bool _terminada;
        private bool _suscrito;
        private bool _presentacionIniciada;
        private double _inicioRed;

        public override void OnNetworkSpawn()
        {
            IntentarSuscribirEncuentro();
        }

        public override void OnNetworkDespawn()
        {
            if (ArenaEncounterNetwork.Instance != null)
                ArenaEncounterNetwork.Instance.Fase.OnValueChanged -= AlCambiarFase;
            _suscrito = false;
            _presentacionIniciada = false;
            BossController.DejarDeObservarJefe(AlSpawnearJefe);
            if (_jefe != null && IsServer) _jefe.Pausar(false);
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
            if (encuentro == null || !encuentro.IsSpawned) return;
            if (!_suscrito)
            {
                encuentro.Fase.OnValueChanged += AlCambiarFase;
                _suscrito = true;
            }
            if (encuentro.Fase.Value == ArenaEncounterPhase.Presentacion)
                SolicitarInicioSincronizado();
        }

        private void SolicitarInicioSincronizado()
        {
            if (_presentacionIniciada) return;
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
            if (_timeline != null)
            {
                _timeline.time = Math.Max(0d, NetworkManager.ServerTime.Time - horaInicio);
                _timeline.extrapolationMode = DirectorWrapMode.Hold;
                _timeline.Play();
            }
            JugadorLocal()?.SetControlCinematica(true);
            if (IsServer)
            {
                BossController.ObservarJefe(NetworkManager, AlSpawnearJefe);
                _jefe?.Pausar(true);
            }
        }

        private void Update()
        {
            if (!IsSpawned) return;
            IntentarSuscribirEncuentro();
            if (ArenaEncounterNetwork.Instance == null)
                return;
            if (ArenaEncounterNetwork.Instance.Fase.Value == ArenaEncounterPhase.Esperando)
            {
                _presentacionIniciada = false;
                _terminada = false;
                return;
            }
            if (ArenaEncounterNetwork.Instance.Fase.Value != ArenaEncounterPhase.Presentacion || _terminada) return;

            float transcurrido = (float)(NetworkManager.ServerTime.Time - _inicioRed);
            if (!_revelado && transcurrido >= _momentoRevelado)
            {
                _revelado = true;
                if (IsServer) ArenaEncounterNetwork.Instance.NotificarJefeRevelado();
            }

            float duracion = _timeline != null ? (float)_timeline.duration : 4f;
            if (!_revelado && transcurrido >= Mathf.Min(_momentoRevelado, duracion))
            {
                _revelado = true;
                if (IsServer) ArenaEncounterNetwork.Instance.NotificarJefeRevelado();
            }
            // Inicia la mezcla mientras la cámara del Timeline todavía conserva el último plano.
            bool tocaRegreso = transcurrido >= Mathf.Max(0f, duracion - _duracionRegresoCamara);
            if (InputSaltar() || tocaRegreso)
            {
                if (IsServer) FinalizarRpc();
            }
        }

        private bool InputSaltar()
        {
#if UNITY_EDITOR
            return IsServer && _permitirSaltarEnEditor && Keyboard.current != null &&
                Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            return false;
#endif
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void FinalizarRpc()
        {
            if (_terminada) return;
            _terminada = true;
            StartCoroutine(DevolverCamaraYControl());
        }

        private IEnumerator DevolverCamaraYControl()
        {
            var jugador = JugadorLocal();
            jugador?.SetCamaraCedida(false);
            if (_timeline != null) _timeline.Stop();

            // Stop libera la cámara del Timeline; Cinemachine mezcla desde su último estado al rig local.
            yield return null;
            var cerebro = FindObjectOfType<CinemachineBrain>();
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

            if (_timeline != null) _timeline.Stop();
            jugador?.SetControlCinematica(false);
            if (IsServer)
            {
                _jefe?.Pausar(false);
                ArenaEncounterNetwork.Instance?.IniciarCombate();
            }
        }

        private void AlSpawnearJefe(BossController jefe)
        {
            _jefe = jefe;
            if (IsServer && ArenaEncounterNetwork.Instance != null &&
                ArenaEncounterNetwork.Instance.Fase.Value == ArenaEncounterPhase.Presentacion)
                _jefe.Pausar(true);
        }

        private PlayerRoot JugadorLocal()
        {
            if (NetworkManager?.LocalClient == null) return null;
            foreach (var objeto in NetworkManager.LocalClient.OwnedObjects)
                if (objeto.TryGetComponent(out PlayerRoot jugador)) return jugador;
            return null;
        }
    }
}
