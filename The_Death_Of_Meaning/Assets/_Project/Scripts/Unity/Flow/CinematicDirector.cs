using System;
using System.Collections;
using System.Collections.Generic;
using TDOM.Gameplay.Core;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace TDOM.Unity.Flow
{
    public class CinematicDirector : NetworkBehaviour
    {
        [Serializable]
        private struct EntradaCatalogo
        {
            public string Id;
            public PlayableDirector Director;
            public CinemachineCamera CamaraDeSalida;
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

        [Header("Salida con blend")]
        [SerializeField]
        private int _prioridadCamaraDeSalida = 100;

        [SerializeField, Min(0f)]
        private float _margenEsperaBlend = 0.5f;

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

        private readonly Dictionary<string, EntradaCatalogo> _entradas = new();
        private GameFlowStateMachine _flujo;
        private PlayableDirector _directorActual;
        private ICinematicExit _salidaActual;
        private BossController _jefe;
        private Coroutine _secuencia;
        private bool _reproduciendo;
        private string _cinematicaPendiente;

        public EstadoFlujo Estado => _flujo.Estado;
        public bool EnGameplay => _flujo.Estado == EstadoFlujo.Gameplay;

        private void Awake()
        {
            foreach (var entrada in _catalogo)
                _entradas[entrada.Id] = entrada;

            RepararBindingsTimelines();
            _flujo = new GameFlowStateMachine(_duracionFade * 2f);
            _flujo.Cambio += AlCambiarEstado;
            FijarFade(AlphaTransparente);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;

            BossController.ObservarJefe(NetworkManager, AlSpawnearJefe);
        }

        public override void OnNetworkDespawn()
        {
            BossController.DejarDeObservarJefe(AlSpawnearJefe);
            DesuscribirJefe();
        }

        private void Update()
        {
            _flujo.Tick(Time.deltaTime);

            if (IsServer && !string.IsNullOrEmpty(_cinematicaPendiente) && EnGameplay)
            {
                string idPendiente = _cinematicaPendiente;
                _cinematicaPendiente = null;
                Debug.Log(
                    $"[CinematicDirector] Reproduciendo cinemática pendiente '{idPendiente}'."
                );
                Reproducir(idPendiente);
            }

            if (IsServer && _reproduciendo && SecuenciaTerminada())
            {
                _reproduciendo = false;
                TerminarRpc();
            }
        }

        public void Reproducir(string id)
        {
            if (!IsServer)
                return;

            if (!EnGameplay)
            {
                Debug.LogWarning(
                    $"[CinematicDirector] No se puede iniciar '{id}' en estado {_flujo.Estado}."
                );
                return;
            }

            if (!_entradas.ContainsKey(id))
            {
                Debug.LogWarning($"[CinematicDirector] No hay cinemática con id '{id}'");
                return;
            }

            ReproducirRpc(id);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ReproducirRpc(string id)
        {
            if (!_entradas.TryGetValue(id, out var entrada))
            {
                Debug.LogWarning(
                    $"[CinematicDirector] No existe la entrada '{id}' en este cliente."
                );
                return;
            }

            _salidaActual?.Cancelar();
            _directorActual = entrada.Director;
            _directorActual.Stop();
            _directorActual.time = 0d;
            _salidaActual = CrearSalida(entrada.CamaraDeSalida);
            Debug.Log(
                $"[CinematicDirector] Iniciando '{id}' con director '{_directorActual.name}'."
            );
            _flujo.IniciarCinematicaSincronizada(id);
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
            _salidaActual.Preparar();
            _directorActual.extrapolationMode = DirectorWrapMode.Hold;
            _directorActual.time = 0d;
            _directorActual.Evaluate();
            _directorActual.Play();
            _reproduciendo = true;
            Debug.Log(
                $"[CinematicDirector] Play '{_directorActual.name}': "
                    + $"state={_directorActual.state}, tiempo={_directorActual.time:F2}/"
                    + $"{_directorActual.duration:F2}, grafo={_directorActual.playableGraph.IsValid()}."
            );

            yield return FundirA(AlphaTransparente);
        }

        private IEnumerator RutinaSalida()
        {
            _flujo.RetenerTransicion();
            try
            {
                if (_flujo.IdActual == "fase_2" && JugadorLocal()?.CamaraCinematica != null)
                    yield return RutinaSalidaACamaraJugador();
                else if (_salidaActual != null)
                    yield return _salidaActual.Salir();
            }
            finally
            {
                _salidaActual = null;
                FijarFade(AlphaTransparente);
                _flujo.LiberarTransicion();
            }
        }

        private IEnumerator RutinaSalidaACamaraJugador()
        {
            var jugador = JugadorLocal();
            var camaraJugador = jugador != null ? jugador.CamaraCinematica : null;
            if (jugador != null)
                jugador.SetCamaraCedida(false);
            if (camaraJugador != null)
                camaraJugador.Priority.Value = _prioridadCamaraDeSalida;

            if (_directorActual != null)
                _directorActual.Stop();
            _directorActual = null;
            _salidaActual?.Cancelar();
            _salidaActual = null;

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

            if (camaraJugador != null && jugador != null)
                camaraJugador.Priority.Value = jugador.PrioridadBaseCamara;
        }

        private void RepararBindingsTimelines()
        {
            var camaraPrincipal = UnityEngine.Camera.main;
            var cerebro =
                camaraPrincipal != null ? camaraPrincipal.GetComponent<CinemachineBrain>() : null;
            foreach (var entrada in _catalogo)
            {
                if (
                    entrada.Director == null
                    || entrada.Director.playableAsset is not TimelineAsset timeline
                )
                    continue;

                foreach (var output in timeline.outputs)
                {
                    if (output.sourceObject is CinemachineTrack pistaCamara)
                    {
                        if (cerebro != null)
                            entrada.Director.SetGenericBinding(pistaCamara, cerebro);
                        else
                            Debug.LogWarning(
                                $"[CinematicDirector] No se encontró CinemachineBrain en Main Camera "
                                    + $"para '{entrada.Id}'."
                            );

                        RepararShots(entrada, pistaCamara);
                    }
                    else if (output.sourceObject is AnimationTrack pistaAnimacion)
                    {
                        var animator = AnimatorDeTimeline(entrada.Id);
                        if (animator != null)
                            entrada.Director.SetGenericBinding(pistaAnimacion, animator);
                        else
                            Debug.LogWarning(
                                $"[CinematicDirector] No se encontró el Animator para la pista "
                                    + $"'{pistaAnimacion.name}' de '{entrada.Id}'."
                            );
                    }
                }

                entrada.Director.RebuildGraph();
            }
        }

        private static void RepararShots(EntradaCatalogo entrada, CinemachineTrack pista)
        {
            int indice = 0;
            foreach (var clip in pista.GetClips())
            {
                if (clip.asset is not CinemachineShot shot)
                    continue;

                string nombreCamara = NombreCamaraTimeline(entrada.Id, indice++);
                var objetoCamara = GameObject.Find(nombreCamara);
                var camara =
                    objetoCamara != null ? objetoCamara.GetComponent<CinemachineCamera>() : null;
                if (camara != null)
                    entrada.Director.SetReferenceValue(shot.VirtualCamera.exposedName, camara);
                else
                    Debug.LogWarning(
                        $"[CinematicDirector] No se encontró la cámara '{nombreCamara}' "
                            + $"para el shot de '{entrada.Id}'."
                    );
            }
        }

        private static string NombreCamaraTimeline(string id, int indice) =>
            id switch
            {
                "prueba" => indice == 0 ? "CamCinematicaPrueba" : "CamCinematicaJugadores",
                "fase_2" => indice == 0 ? "CamFaseCerca" : "CamFaseLejos",
                _ => string.Empty,
            };

        private static Animator AnimatorDeTimeline(string id) =>
            id switch
            {
                "prueba" => GameObject.Find("PivoteOrbita")?.GetComponent<Animator>(),
                "fase_2" => GameObject.Find("CamFaseCerca")?.GetComponent<Animator>(),
                _ => null,
            };

        private ICinematicExit CrearSalida(CinemachineCamera camaraDeSalida) =>
            camaraDeSalida != null
                ? new BlendCinematicExit(
                    camaraDeSalida,
                    _prioridadCamaraDeSalida,
                    _margenEsperaBlend,
                    DetenerDirector
                )
                : new FadeCinematicExit(FundirA, DetenerDirector);

        private void DetenerDirector()
        {
            if (_directorActual != null)
                _directorActual.Stop();
            _directorActual = null;
            JugadorLocal()?.SetCamaraCedida(false);
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
                _jefe.FaseIniciadaEnServidor += AlCambiarFase;
        }

        private void DesuscribirJefe()
        {
            if (_jefe != null)
                _jefe.FaseIniciadaEnServidor -= AlCambiarFase;
            _jefe = null;
        }

        private void AlCambiarFase(int anterior, int actual)
        {
            string id = PrefijoCinematicaDeFase + actual;
            Debug.Log(
                $"[CinematicDirector] Boss cambió de fase {anterior} -> {actual}; "
                    + $"servidor={IsServer}, estado={_flujo.Estado}, cinemática='{id}'."
            );
            if (!_entradas.ContainsKey(id))
                return;
            if (!IsServer)
                return;

            if (!EnGameplay)
            {
                _cinematicaPendiente = id;
                return;
            }
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
            area.y += AltoBotonDebug + MargenDebug;
            if (GUI.Button(area, "Probar fase 2"))
                Reproducir("fase_2");
        }
    }
}
