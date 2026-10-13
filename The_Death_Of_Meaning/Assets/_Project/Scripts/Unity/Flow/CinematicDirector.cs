using System;
using System.Collections;
using System.Collections.Generic;
using TDOM.Gameplay.Core;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;

namespace TDOM.Unity.Flow
{
    public class CinematicDirector : NetworkBehaviour
    {
        [Serializable]
        private struct EntradaCatalogo
        {
            public string Id;
            public PlayableDirector Director;
        }

        private const string PrefijoCinematicaDeFase = "fase_";
        private const float AlphaTransparente = 0f;
        private const float AlphaNegro = 1f;
        private const float AnchoPanelDebug = 200f;
        private const float AltoBotonDebug = 30f;
        private const float MargenDebug = 10f;

        [Header("Catálogo")]
        [SerializeField]
        private EntradaCatalogo[] _catalogo;

        [Header("Fade")]
        [SerializeField]
        private CanvasGroup _fade;

        [SerializeField]
        private float _duracionFade = 0.4f;

        [Header("HUD")]
        [SerializeField]
        private GameplayUI _gameplayUI;

        [Header("Jefe")]
        [SerializeField]
        private bool _reproducirEnCambioDeFase;

        [Header("Debug")]
        [SerializeField]
        private bool _mostrarDebugUI = true;

        [SerializeField]
        private string _idPrueba = "prueba";

        private readonly Dictionary<string, PlayableDirector> _directores = new();
        private GameFlowStateMachine _flujo;
        private PlayableDirector _directorActual;
        private BossController _jefe;
        private Coroutine _secuencia;
        private bool _reproduciendo;

        public EstadoFlujo Estado => _flujo.Estado;
        public bool EnGameplay => _flujo.Estado == EstadoFlujo.Gameplay;

        private void Awake()
        {
            foreach (var entrada in _catalogo)
                _directores[entrada.Id] = entrada.Director;

            _flujo = new GameFlowStateMachine(_duracionFade * 2f);
            _flujo.Cambio += AlCambiarEstado;
            FijarFade(AlphaTransparente);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;

            BossController.Spawned += AlSpawnearJefe;
        }

        public override void OnNetworkDespawn()
        {
            BossController.Spawned -= AlSpawnearJefe;
            DesuscribirJefe();
        }

        private void Update()
        {
            _flujo.Tick(Time.deltaTime);

            if (IsServer && _reproduciendo && SecuenciaTerminada())
            {
                _reproduciendo = false;
                TerminarRpc();
            }
        }

        public void Reproducir(string id)
        {
            if (!IsServer || !EnGameplay)
                return;

            if (!_directores.ContainsKey(id))
            {
                Debug.LogWarning($"[CinematicDirector] No hay cinemática con id '{id}'");
                return;
            }

            ReproducirRpc(id);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ReproducirRpc(string id)
        {
            if (!_directores.TryGetValue(id, out var director))
                return;

            _directorActual = director;
            _flujo.IniciarCinematica(id);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void TerminarRpc()
        {
            _reproduciendo = false;
            _flujo.TerminarCinematica();
        }

        private void AlCambiarEstado(EstadoFlujo estado)
        {
            if (IsServer && _jefe != null)
                _jefe.Pausar(estado != EstadoFlujo.Gameplay);

            switch (estado)
            {
                case EstadoFlujo.Cinematica:
                    AplicarControlLocal(false);
                    IniciarSecuencia(RutinaEntrada());
                    break;
                case EstadoFlujo.Transicion:
                    IniciarSecuencia(RutinaSalida());
                    break;
                case EstadoFlujo.Gameplay:
                    AplicarControlLocal(true);
                    break;
            }
        }

        private IEnumerator RutinaEntrada()
        {
            yield return FundirA(AlphaNegro);

            JugadorLocal()?.SetCamaraCedida(true);
            _directorActual.extrapolationMode = DirectorWrapMode.Hold;
            _directorActual.Play();
            _reproduciendo = true;

            yield return FundirA(AlphaTransparente);
        }

        private IEnumerator RutinaSalida()
        {
            yield return FundirA(AlphaNegro);

            if (_directorActual != null)
                _directorActual.Stop();
            _directorActual = null;
            JugadorLocal()?.SetCamaraCedida(false);

            yield return FundirA(AlphaTransparente);
        }

        private IEnumerator FundirA(float objetivo)
        {
            if (_fade == null)
                yield break;

            float inicio = _fade.alpha;
            float transcurrido = 0f;
            while (transcurrido < _duracionFade)
            {
                transcurrido += Time.deltaTime;
                _fade.alpha = Mathf.Lerp(inicio, objetivo, transcurrido / _duracionFade);
                yield return null;
            }
            _fade.alpha = objetivo;
        }

        private void IniciarSecuencia(IEnumerator rutina)
        {
            if (_secuencia != null)
                StopCoroutine(_secuencia);

            _secuencia = StartCoroutine(rutina);
        }

        private void FijarFade(float alpha)
        {
            if (_fade == null)
                return;

            _fade.alpha = alpha;
            _fade.blocksRaycasts = false;
            _fade.interactable = false;
        }

        private bool SecuenciaTerminada() =>
            _directorActual != null && _directorActual.time >= _directorActual.duration;

        private void AplicarControlLocal(bool activo)
        {
            JugadorLocal()?.SetControlCinematica(!activo);

            if (_gameplayUI != null)
                _gameplayUI.SetHudVisible(activo);
        }

        private PlayerRoot JugadorLocal()
        {
            var cliente = NetworkManager.Singleton?.LocalClient;
            if (cliente == null)
                return null;

            foreach (var objeto in cliente.OwnedObjects)
            {
                var jugador = objeto.GetComponent<PlayerRoot>();
                if (jugador != null)
                    return jugador;
            }
            return null;
        }

        private void AlSpawnearJefe(BossController jefe)
        {
            DesuscribirJefe();
            _jefe = jefe;
            _jefe.Pausar(!EnGameplay);

            if (_reproducirEnCambioDeFase)
                _jefe.OnFaseChanged += AlCambiarFase;
        }

        private void DesuscribirJefe()
        {
            if (_jefe != null)
                _jefe.OnFaseChanged -= AlCambiarFase;
            _jefe = null;
        }

        private void AlCambiarFase(int anterior, int actual)
        {
            string id = PrefijoCinematicaDeFase + actual;
            if (_directores.ContainsKey(id))
                Reproducir(id);
        }

        private void OnGUI()
        {
            if (!_mostrarDebugUI || !IsServer || !IsSpawned)
                return;

            var area = new Rect(
                Screen.width - AnchoPanelDebug - MargenDebug,
                MargenDebug,
                AnchoPanelDebug,
                AltoBotonDebug
            );

            if (GUI.Button(area, "Probar cinemática"))
                Reproducir(_idPrueba);
        }
    }
}
