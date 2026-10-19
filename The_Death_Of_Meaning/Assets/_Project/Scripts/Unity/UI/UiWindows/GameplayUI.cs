using TDOM.Contracts;
using TDOM.Gameplay.Camera;
using TDOM.Unity.Player;
using TDOM.Unity.UI;
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

        [Header("Cooldowns")]
        [SerializeField] private Image dashandembestidaslider;
        [SerializeField] private Image hookandgraplerslider;
        [SerializeField] private Color cooldownDisponible = Color.white;
        [SerializeField] private Color cooldownEnUso = new Color(0.35f, 0.35f, 0.35f, 1f);


        [Header("PlayerHealth")]
        [SerializeField] private Image mainPlayerIcon;
        [SerializeField] private Image secondaryPlayerIcon;
        [SerializeField] private Slider mainPlayerHealthSlider;
        [SerializeField] private Slider secondaryPlayerHealthSlider;

        [SerializeField] private Sprite zendreIcon;
        [SerializeField] private Sprite aylaIcon;


        [Header("Vida del Jefe")]
        [SerializeField]
        private BossHealthBar _barraVidaJefe;

        private PlayerRoot _localPlayer;
        private RectTransform _crosshairRect;
        private Vector3 _escalaBase;
        private bool _hudVisible = true;
        private PlayerEnergyHud _barraEnergiaJugador;
        private PlayerRoot _jugadorConEnergiaInicializado;

        private void Update()
        {
            if (_barraEnergiaJugador == null)
                _barraEnergiaJugador = GetComponentInChildren<PlayerEnergyHud>(true);

            if (_barraEnergiaJugador == null || _localPlayer == null)
                return;

            _barraEnergiaJugador.ActualizarEnergia(_localPlayer.Energia.Value);

            if (_jugadorConEnergiaInicializado == _localPlayer)
                return;

            string etiquetaEnergia = _localPlayer.CharacterId switch
            {
                CharacterIds.Ayla => "Stamina",
                CharacterIds.Zendre => "Maná",
                _ => "Energy",
            };

            _barraEnergiaJugador.Reinitialize(
                _localPlayer,
                etiquetaEnergia,
                _localPlayer.EnergiaMaxima
            );
            _jugadorConEnergiaInicializado = _localPlayer;
        }

        public void SetHudVisible(bool visible)
        {
            _hudVisible = visible;
            if (canvasGroup != null)
                canvasGroup.alpha = visible ? 1f : 0f;

            if (_barraVidaJefe != null)
                _barraVidaJefe.CambiarVisibilidadHud(visible);
        }

        public override void Initialize()
        {
            base.Initialize();

            ConfigurarImagenCooldown(dashandembestidaslider);
            ConfigurarImagenCooldown(hookandgraplerslider);

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

        private void OnDisable()
        {
            if (_localPlayer != null)
                _localPlayer.DashEmbestidaIniciada -= ActualizarCooldowns;
            _localPlayer = null;
            if (crosshairImage != null)
                crosshairImage.gameObject.SetActive(false);
            if (crosshairDefaultImage != null)
                crosshairDefaultImage.gameObject.SetActive(false);
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

            ActualizarCooldowns();

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
                    _localPlayer.DashEmbestidaIniciada += ActualizarCooldowns;
                    ActualizarIconosPersonajes();
                    if (_crosshairRect == null && crosshairImage != null)
                    {
                        _crosshairRect = crosshairImage.rectTransform;
                        _escalaBase = _crosshairRect.localScale;
                    }
                    break;
                }
            }
        }

        private void ActualizarIconosPersonajes()
        {
            if (_localPlayer == null)
                return;

            if (_localPlayer.CharacterId == CharacterIds.Ayla)
            {
                if (mainPlayerIcon != null)
                    mainPlayerIcon.sprite = aylaIcon;
                if (secondaryPlayerIcon != null)
                    secondaryPlayerIcon.sprite = zendreIcon;
            }
            else if (_localPlayer.CharacterId == CharacterIds.Zendre)
            {
                if (mainPlayerIcon != null)
                    mainPlayerIcon.sprite = zendreIcon;
                if (secondaryPlayerIcon != null)
                    secondaryPlayerIcon.sprite = aylaIcon;
            }
        }

        private static void ConfigurarImagenCooldown(Image imagen)
        {
            if (imagen == null)
                return;

            imagen.type = Image.Type.Filled;
            imagen.fillMethod = Image.FillMethod.Radial360;
            imagen.fillOrigin = (int)Image.Origin360.Top;
            imagen.fillClockwise = true;
            imagen.raycastTarget = false;
        }

        private void ActualizarCooldowns()
        {
            if (_localPlayer == null)
                return;

            var cooldowns = _localPlayer.Cooldowns;
            ActualizarIconoCooldown(dashandembestidaslider, cooldowns.Dash);

            float cooldownSecundario = _localPlayer.CharacterId == CharacterIds.Zendre
                ? cooldowns.Ancla
                : cooldowns.Gancho;
            ActualizarIconoCooldown(hookandgraplerslider, cooldownSecundario);
        }

        private void ActualizarIconoCooldown(Image imagen, float normalizado)
        {
            if (imagen == null)
                return;

            normalizado = Mathf.Clamp01(normalizado);
            imagen.fillAmount = normalizado;
            imagen.color = normalizado <= 0.001f ? cooldownDisponible : cooldownEnUso;
        }
    }
}
