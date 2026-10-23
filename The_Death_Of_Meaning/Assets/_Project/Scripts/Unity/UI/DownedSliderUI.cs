using TDOM.Data;
using TDOM.Gameplay.Core;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.UI
{
    [DisallowMultipleComponent]
    public sealed class DownedSliderUI : NetworkBehaviour
    {
        [SerializeField]
        private PlayerRoot _playerRoot;

        [SerializeField]
        private UnityEngine.UI.Slider _slider;

        [SerializeField]
        private UnityEngine.UI.Image _fillImage;

        [SerializeField]
        private Canvas _canvas;

        [SerializeField]
        private DownedSliderProfile _profile;

        private UnityEngine.Camera _camaraPrincipal;
        private bool _suscrito;

        private void Awake()
        {
            Ocultar();
        }

        public override void OnNetworkSpawn()
        {
            Conectar();
        }

        private void OnEnable()
        {
            if (IsSpawned)
                Conectar();
        }

        private void Conectar()
        {
            if (!IsClient || _suscrito || !isActiveAndEnabled)
                return;

            if (
                _playerRoot == null
                || _slider == null
                || _fillImage == null
                || _canvas == null
                || _profile == null
            )
            {
                Debug.LogError($"[DownedSliderUI] Referencias incompletas en {name}.", this);
                return;
            }

            _playerRoot.EstadoVida.OnValueChanged += OnEstadoVidaChanged;
            _playerRoot.TiempoAbatidoRestante.OnValueChanged += OnProgresoChanged;
            _playerRoot.ProgresoRevivir.OnValueChanged += OnProgresoChanged;
            _suscrito = true;
            ActualizarEstadoVisual();
        }

        public override void OnNetworkDespawn()
        {
            Desconectar();
        }

        private void OnDisable()
        {
            Desconectar();
        }

        public override void OnDestroy()
        {
            Desconectar();
            base.OnDestroy();
        }

        private void Desconectar()
        {
            if (_suscrito && _playerRoot != null)
            {
                _playerRoot.EstadoVida.OnValueChanged -= OnEstadoVidaChanged;
                _playerRoot.TiempoAbatidoRestante.OnValueChanged -= OnProgresoChanged;
                _playerRoot.ProgresoRevivir.OnValueChanged -= OnProgresoChanged;
            }

            _suscrito = false;
            _camaraPrincipal = null;
            Ocultar();
        }

        private void Ocultar()
        {
            if (_canvas != null)
                _canvas.enabled = false;
        }

        private void OnEstadoVidaChanged(PlayerLifeState anterior, PlayerLifeState actual)
        {
            ActualizarEstadoVisual();
        }

        private void OnProgresoChanged(float anterior, float actual)
        {
            ActualizarEstadoVisual();
        }

        private void ActualizarEstadoVisual()
        {
            _canvas.enabled = _playerRoot.EstadoVida.Value == PlayerLifeState.Abatido;
            if (!_canvas.enabled)
                return;

            float progreso = _playerRoot.ProgresoRevivir.Value;
            bool reviviendo = progreso > 0f;
            _slider.SetValueWithoutNotify(
                Mathf.Clamp01(reviviendo ? progreso : _playerRoot.TiempoAbatidoNormalizado)
            );
            _fillImage.color = reviviendo ? _profile.ColorReviviendo : _profile.ColorAbatido;
            ActualizarPosicion();
        }

        private void LateUpdate()
        {
            if (_suscrito && _canvas.enabled)
                ActualizarPosicion();
        }

        private void ActualizarPosicion()
        {
            _canvas.transform.position = _playerRoot.transform.position + _profile.Offset;
            if (_camaraPrincipal == null || !_camaraPrincipal.isActiveAndEnabled)
                _camaraPrincipal = UnityEngine.Camera.main;

            if (_camaraPrincipal != null)
                _canvas.transform.rotation = _camaraPrincipal.transform.rotation;
        }
    }
}
