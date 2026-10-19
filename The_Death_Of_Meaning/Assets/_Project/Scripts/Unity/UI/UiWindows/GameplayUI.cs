using TDOM.Contracts;
using TDOM.Gameplay.Camera;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class GameplayUI : UIWindow
    {
        [Header("Referencias Crosshair")]
        [Tooltip("Mira de habilidad / apuntado (con raycast de rango, escala y cambio de color)")]
        [SerializeField]
        private Image crosshairImage;

        [Tooltip("Mira básica por defecto (visible en modo libre / cadera)")]
        [SerializeField]
        private Image crosshairDefaultImage;

        [Header("Colores")]
        [SerializeField]
        private Color colorFueraDeRango = Color.white;

        [SerializeField]
        private Color colorEnRango = new Color(0.2f, 1f, 0.2f, 1f);

        [Header("Multiplicador de Escala en Rango")]
        [SerializeField]
        private float multiplicadorEscalaEnRango = 1.15f;

        [Header("Vida del Jefe")]
        [SerializeField]
        private Slider _sliderVidaJefe;

        private BossController _jefe;
        private PlayerRoot _localPlayer;
        private RectTransform _crosshairRect;
        private Vector3 _escalaBase;
        private bool _hudVisible = true;

        public void SetHudVisible(bool visible)
        {
            _hudVisible = visible;
            if (canvasGroup != null)
                canvasGroup.alpha = visible ? 1f : 0f;
        }

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

            MostrarSliderVidaJefe(_jefe != null);
        }

        private void OnEnable()
        {
            BossController.Spawned += AlSpawnearJefe;
        }

        private void OnDisable()
        {
            BossController.Spawned -= AlSpawnearJefe;
            DesuscribirJefe();

            _localPlayer = null;
            if (crosshairImage != null)
                crosshairImage.gameObject.SetActive(false);
            if (crosshairDefaultImage != null)
                crosshairDefaultImage.gameObject.SetActive(false);
        }

        private void AlSpawnearJefe(BossController jefe)
        {
            DesuscribirJefe();
            _jefe = jefe;
            _jefe.Vida.OnValueChanged += AlCambiarVidaJefe;
            MostrarSliderVidaJefe(true);
            ActualizarVidaJefe(_jefe.Vida.Value);
        }

        private void AlCambiarVidaJefe(float anterior, float actual) => ActualizarVidaJefe(actual);

        private void ActualizarVidaJefe(float vida)
        {
            if (_sliderVidaJefe != null)
                _sliderVidaJefe.value = vida;
        }

        private void DesuscribirJefe()
        {
            if (_jefe != null)
                _jefe.Vida.OnValueChanged -= AlCambiarVidaJefe;

            _jefe = null;
            MostrarSliderVidaJefe(false);
        }

        private void MostrarSliderVidaJefe(bool visible)
        {
            if (_sliderVidaJefe != null)
                _sliderVidaJefe.gameObject.SetActive(visible);
        }

        private void LateUpdate()
        {
            if (!_hudVisible)
                return;

            // Si no tenemos referencia al jugador local, lo buscamos en la escena
            if (_localPlayer == null)
            {
                BuscarJugadorLocal();
                if (_localPlayer == null)
                    return;
            }

            if (_localPlayer.CameraTransform == null)
                return;

            var input =
                _localPlayer.InputReader != null ? _localPlayer.InputReader.Read() : default;

            bool esZendre = _localPlayer.CharacterId == CharacterIds.Zendre;
            bool soloAlApuntar = esZendre;
            float alcance = esZendre ? _localPlayer.AnchorRange : _localPlayer.GrappleRange;

            // Control de mira por defecto (ej. el punto de cadera de Zendre cuando no está apuntando)
            if (crosshairDefaultImage != null)
            {
                bool mostrarMiraDefault = soloAlApuntar && !input.AimHeld;
                if (crosshairDefaultImage.gameObject.activeSelf != mostrarMiraDefault)
                    crosshairDefaultImage.gameObject.SetActive(mostrarMiraDefault);
            }

            // Control de la mira de habilidad / apuntado
            bool mostrarMiraApuntado = !soloAlApuntar || input.AimHeld;

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

            Vector3 origen = _localPlayer.transform.position;
            var apuntado = _localPlayer.ResolverApuntado(origen, out Collider impactado);
            bool valido =
                apuntado.HasHit
                && impactado.GetComponentInParent<PlayerRoot>() == null
                && AimResolver.IsWithinRange(origen, apuntado.TargetPoint, alcance);

            // Aplicar colores y escala
            crosshairImage.color = valido ? colorEnRango : colorFueraDeRango;

            if (_crosshairRect != null)
            {
                float factor = valido ? multiplicadorEscalaEnRango : 1f;
                _crosshairRect.localScale = _escalaBase * factor;
            }
        }

        private void BuscarJugadorLocal()
        {
            var players = FindObjectsByType<PlayerRoot>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p != null && p.IsOwner)
                {
                    _localPlayer = p;
                    if (_crosshairRect == null && crosshairImage != null)
                    {
                        _crosshairRect = crosshairImage.rectTransform;
                        _escalaBase = _crosshairRect.localScale;
                    }
                    break;
                }
            }
        }
    }
}
