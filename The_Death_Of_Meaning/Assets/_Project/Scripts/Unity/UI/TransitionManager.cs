using System;
using System.Collections;
using DG.Tweening;
using TDOM.Data;
using TDOM.Gameplay.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TDOM.Unity.UI
{
    public class TransitionManager : MonoBehaviour
    {
        private const float ProgresoMaximoAntesDeActivar = 0.9f;

        [SerializeField]
        private TransitionConfig _config;

        [SerializeField]
        private CanvasGroupFader _fade;

        [SerializeField]
        private PantallaCarga _pantallaCarga;

        [SerializeField]
        private ConnectionManager _conexion;

        private LoadingScreenState _estadoCarga;
        private Coroutine _rutinaVentana;
        private Coroutine _rutinaCargaLocal;
        private UIWindow _ventanaOrigenPendiente;
        private UIWindow _ventanaDestinoPendiente;
        private bool _hayCambioPendiente;

        public bool CargandoEscena => _estadoCarga.Visible;

        private void Awake()
        {
            _estadoCarga = new LoadingScreenState(_config.tiempoMinimoPantallaCarga);
            _estadoCarga.Ocultada += AlOcultarPantallaCarga;
            _pantallaCarga.Configurar(_config.velocidadSpinnerGradosPorSegundo);
            _pantallaCarga.OcultarInstantaneo();
            _fade.OcultarInstantaneo();
        }

        private void OnEnable()
        {
            if (_conexion == null)
                return;

            _conexion.OnCargaEscenaIniciada += MostrarPantallaCargaEnRed;
            _conexion.OnCargaEscenaCompletada += _estadoCarga.CompletarCarga;
            _conexion.OnEstadoCambiado += AlCambiarEstadoConexion;
        }

        private void OnDisable()
        {
            if (_conexion == null)
                return;

            _conexion.OnCargaEscenaIniciada -= MostrarPantallaCargaEnRed;
            _conexion.OnCargaEscenaCompletada -= _estadoCarga.CompletarCarga;
            _conexion.OnEstadoCambiado -= AlCambiarEstadoConexion;
        }

        private void OnDestroy()
        {
            _estadoCarga.Ocultada -= AlOcultarPantallaCarga;
        }

        private void Update()
        {
            _estadoCarga.Tick(Time.unscaledDeltaTime);
            _pantallaCarga.FijarProgreso(_estadoCarga.Progreso);
        }

        public YieldInstruction FadeOut(float duracion) =>
            _fade.Mostrar(duracion).WaitForCompletion();

        public YieldInstruction FadeIn(float duracion) =>
            _fade.Ocultar(duracion).WaitForCompletion();

        public void CambiarVentana(UIWindow de, UIWindow a)
        {
            CompletarCambioDeVentana();

            if (de != null)
                de.SetInteractable(false);

            _ventanaOrigenPendiente = de;
            _ventanaDestinoPendiente = a;
            _hayCambioPendiente = true;
            _rutinaVentana = StartCoroutine(RutinaCambiarVentana());
        }

        public void CargarEscena(string nombreEscena, Action alCubrirPantalla = null)
        {
            if (_rutinaCargaLocal != null)
                return;

            _rutinaCargaLocal = StartCoroutine(RutinaCargaLocal(nombreEscena, alCubrirPantalla));
        }

        private IEnumerator RutinaCambiarVentana()
        {
            yield return FadeOut(_config.duracionFadeVentana);
            AplicarCambioDeVentana();
            yield return FadeIn(_config.duracionFadeVentana);
            _rutinaVentana = null;
        }

        private void CompletarCambioDeVentana()
        {
            if (_rutinaVentana != null)
            {
                StopCoroutine(_rutinaVentana);
                _rutinaVentana = null;
            }

            AplicarCambioDeVentana();
        }

        private void AplicarCambioDeVentana()
        {
            if (!_hayCambioPendiente)
                return;

            _hayCambioPendiente = false;

            if (_ventanaOrigenPendiente != null)
                _ventanaOrigenPendiente.Hide();

            if (_ventanaDestinoPendiente != null)
                _ventanaDestinoPendiente.Show();
        }

        private IEnumerator RutinaCargaLocal(string nombreEscena, Action alCubrirPantalla)
        {
            _estadoCarga.Mostrar();
            yield return _pantallaCarga
                .Mostrar(_config.duracionFadePantallaCarga)
                .WaitForCompletion();

            CompletarCambioDeVentana();
            alCubrirPantalla?.Invoke();

            var operacion = SceneManager.LoadSceneAsync(nombreEscena);
            while (operacion != null && !operacion.isDone)
            {
                _estadoCarga.ReportarProgreso(operacion.progress / ProgresoMaximoAntesDeActivar);
                yield return null;
            }

            _estadoCarga.CompletarCarga();
            _rutinaCargaLocal = null;
        }

        private void MostrarPantallaCargaEnRed()
        {
            _estadoCarga.Mostrar();
            _pantallaCarga.MostrarInstantaneo();
        }

        private void AlCambiarEstadoConexion(EstadoSesion estado)
        {
            if (estado == EstadoSesion.Desconectado || estado == EstadoSesion.Error)
                _estadoCarga.CompletarCarga();
        }

        private void AlOcultarPantallaCarga()
        {
            _pantallaCarga.Ocultar(_config.duracionFadePantallaCarga);
        }
    }
}
