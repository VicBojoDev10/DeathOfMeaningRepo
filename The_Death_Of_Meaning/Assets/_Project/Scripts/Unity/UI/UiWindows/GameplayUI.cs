using TDOM.Unity.Input;
using TDOM.Unity.Player;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class GameplayUI : UIWindow
    {
        [Header("Referencias Crosshair")]
        [Tooltip("Mira de habilidad / apuntado (con raycast de rango, escala y cambio de color)")]
        [SerializeField] private Image crosshairImage;

        [Tooltip("Mira básica por defecto (visible en modo libre / cadera)")]
        [SerializeField] private Image crosshairDefaultImage;

        [Header("Colores")]
        [SerializeField] private Color colorFueraDeRango = Color.white;
        [SerializeField] private Color colorEnRango = new Color(0.2f, 1f, 0.2f, 1f);

        [Header("Multiplicador de Escala en Rango")]
        [SerializeField] private float multiplicadorEscalaEnRango = 1.15f;

        private Transform _camara;
        private float _alcance;
        private bool _soloAlApuntar;
        private PlayerInputReader _inputReader;
        private RectTransform _crosshairRect;
        private Vector3 _escalaBase;
        private bool _estaConfigurado;

        public override void Initialize()
        {
            base.Initialize();

            if (crosshairImage != null)
            {
                _crosshairRect = crosshairImage.rectTransform;
                _escalaBase = _crosshairRect.localScale;
                if (_escalaBase == Vector3.zero)
                    _escalaBase = new Vector3(0.35f, 0.35f, 1f);

                crosshairImage.gameObject.SetActive(false);
            }

            if (crosshairDefaultImage != null)
            {
                crosshairDefaultImage.gameObject.SetActive(false);
            }
        }

        public void ConfigurarMira(Transform camara, float alcance, bool soloAlApuntar, PlayerInputReader inputReader)
        {
            _camara = camara;
            _alcance = alcance;
            _soloAlApuntar = soloAlApuntar;
            _inputReader = inputReader;
            _estaConfigurado = true;

            if (_crosshairRect == null && crosshairImage != null)
            {
                _crosshairRect = crosshairImage.rectTransform;
                _escalaBase = _crosshairRect.localScale;
            }
        }

        public void DesactivarMira()
        {
            _estaConfigurado = false;
            _camara = null;
            _inputReader = null;

            if (crosshairImage != null)
                crosshairImage.gameObject.SetActive(false);

            if (crosshairDefaultImage != null)
                crosshairDefaultImage.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!_estaConfigurado || _camara == null)
                return;

            var input = _inputReader != null ? _inputReader.Read() : default;

            // Zendre: visible solo sosteniendo L2 (AimHeld). Ayla: siempre visible.
            bool mostrarMiraApuntado = !_soloAlApuntar || input.AimHeld;

            // Control de mira por defecto (ej. el punto de cadera de Zendre cuando no está apuntando)
            if (crosshairDefaultImage != null)
            {
                bool mostrarMiraDefault = _soloAlApuntar && !input.AimHeld;
                if (crosshairDefaultImage.gameObject.activeSelf != mostrarMiraDefault)
                    crosshairDefaultImage.gameObject.SetActive(mostrarMiraDefault);
            }

            if (crosshairImage == null)
                return;

            if (!mostrarMiraApuntado)
            {
                if (crosshairImage.gameObject.activeSelf)
                    crosshairImage.gameObject.SetActive(false);
                return;
            }

            if (!crosshairImage.gameObject.activeSelf)
                crosshairImage.gameObject.SetActive(true);

            // Raycast desde el centro de la cámara
            Ray ray = new Ray(_camara.position, _camara.forward);
            bool valido = false;

            if (Physics.Raycast(ray, out RaycastHit hit, _alcance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                // Ignorar colisiones con el propio o con otro jugador
                if (hit.collider.GetComponentInParent<PlayerRoot>() == null)
                {
                    valido = true;
                }
            }

            // Aplicar colores y escala
            crosshairImage.color = valido ? colorEnRango : colorFueraDeRango;

            if (_crosshairRect != null)
            {
                float factor = valido ? multiplicadorEscalaEnRango : 1f;
                _crosshairRect.localScale = _escalaBase * factor;
            }
        }
    }
}
