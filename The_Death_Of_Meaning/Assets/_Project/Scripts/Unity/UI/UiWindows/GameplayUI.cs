using TDOM.Contracts;
using TDOM.Gameplay.Camera;
using TDOM.Unity.Player;
using TDOM.Unity.UI;
using Unity.Netcode;
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
        [SerializeField]
        private Image dashandembestidaslider;

        [SerializeField]
        private Image hookandgraplerslider;

        [SerializeField]
        private Color cooldownDisponible = Color.white;

        [SerializeField]
        private Color cooldownEnUso = new Color(0.35f, 0.35f, 0.35f, 1f);

        [Header("PlayerHealth")]
        [SerializeField]
        private Image mainPlayerIcon;

        [SerializeField]
        private Image secondaryPlayerIcon;

        [SerializeField]
        private Slider mainPlayerHealthSlider;

        [SerializeField]
        private Slider secondaryPlayerHealthSlider;

        [SerializeField]
        private Sprite zendreIcon;

        [SerializeField]
        private Sprite aylaIcon;

        [Header("Vida del Jefe")]
        [SerializeField]
        private BossHealthBar _barraVidaJefe;

        private PlayerRoot _localPlayer;
        private PlayerRoot _otherPlayer;
        private float _busquedaJugadoresTimer;
        private RectTransform _crosshairRect;
        private Vector3 _escalaBase;
        private bool _hudVisible = true;
        private PlayerEnergyHud _barraEnergiaJugador;
        private PlayerRoot _jugadorConEnergiaInicializado;

        private void Update()
        {
            if (_localPlayer == null || _otherPlayer == null)
            {
                _busquedaJugadoresTimer -= Time.deltaTime;
                if (_busquedaJugadoresTimer <= 0f)
                {
                    _busquedaJugadoresTimer = 0.5f;
                    BuscarJugadores();
                }
            }

            if (_barraEnergiaJugador == null)
                _barraEnergiaJugador = GetComponentInChildren<PlayerEnergyHud>(true);

            if (_barraEnergiaJugador == null || _localPlayer == null)
                return;

            _barraEnergiaJugador.ActualizarEnergia(_localPlayer.Energia.Value);
            _barraEnergiaJugador.MostrarCostoPendiente(
                _localPlayer.CostoEnergiaPendiente,
                _localPlayer.Energia.Value
            );

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
            DesconectarJugadores();
            if (crosshairImage != null)
                crosshairImage.gameObject.SetActive(false);
            if (crosshairDefaultImage != null)
                crosshairDefaultImage.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            DesconectarJugadores();
        }

        private void DesconectarJugadores()
        {
            if (_localPlayer != null)
            {
                _localPlayer.DashEmbestidaIniciada -= ActualizarCooldowns;
                _localPlayer.Vida.OnValueChanged -= OnVidaLocalChanged;
            }

            if (_otherPlayer != null)
            {
                _otherPlayer.Vida.OnValueChanged -= OnVidaSecundarioChanged;
            }

            _localPlayer = null;
            _otherPlayer = null;
            _jugadorConEnergiaInicializado = null;
        }

        private void LateUpdate()
        {
            if (!_hudVisible)
                return;

            if (_localPlayer == null)
            {
                BuscarJugadores();
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

        private void BuscarJugadores()
        {
            var manager = NetworkManager.Singleton;
            if (manager == null || !manager.IsClient || manager.SpawnManager == null)
                return;

            if (_localPlayer == null && manager.LocalClient != null)
            {
                foreach (var objeto in manager.LocalClient.OwnedObjects)
                {
                    if (
                        objeto != null
                        && objeto.IsSpawned
                        && objeto.TryGetComponent(out PlayerRoot player)
                    )
                    {
                        ConectarJugadorLocal(player);
                        break;
                    }
                }
            }

            if (_otherPlayer != null)
                return;

            foreach (var objeto in manager.SpawnManager.SpawnedObjectsList)
            {
                if (
                    objeto != null
                    && objeto.IsSpawned
                    && !objeto.IsOwner
                    && objeto.TryGetComponent(out PlayerRoot player)
                )
                {
                    ConectarJugadorSecundario(player);
                    return;
                }
            }
        }

        private void ConectarJugadorLocal(PlayerRoot player)
        {
            _localPlayer = player;
            _localPlayer.DashEmbestidaIniciada += ActualizarCooldowns;
            _localPlayer.Vida.OnValueChanged += OnVidaLocalChanged;

            if (mainPlayerHealthSlider != null)
            {
                mainPlayerHealthSlider.minValue = 0f;
                mainPlayerHealthSlider.maxValue = _localPlayer.VidaMaxima;
                mainPlayerHealthSlider.value = Mathf.Clamp(
                    _localPlayer.Vida.Value,
                    0f,
                    _localPlayer.VidaMaxima
                );
            }

            ActualizarIconosPersonajes();

            if (_crosshairRect == null && crosshairImage != null)
            {
                _crosshairRect = crosshairImage.rectTransform;
                _escalaBase = _crosshairRect.localScale;
            }
        }

        private void ConectarJugadorSecundario(PlayerRoot player)
        {
            _otherPlayer = player;
            _otherPlayer.Vida.OnValueChanged += OnVidaSecundarioChanged;

            if (secondaryPlayerHealthSlider != null)
            {
                secondaryPlayerHealthSlider.minValue = 0f;
                secondaryPlayerHealthSlider.maxValue = _otherPlayer.VidaMaxima;
                secondaryPlayerHealthSlider.value = Mathf.Clamp(
                    _otherPlayer.Vida.Value,
                    0f,
                    _otherPlayer.VidaMaxima
                );
            }

            ActualizarIconosPersonajes();
        }

        private void OnVidaLocalChanged(float anterior, float actual)
        {
            if (mainPlayerHealthSlider != null && _localPlayer != null)
            {
                mainPlayerHealthSlider.value = Mathf.Clamp(actual, 0f, _localPlayer.VidaMaxima);
            }
        }

        private void OnVidaSecundarioChanged(float anterior, float actual)
        {
            if (secondaryPlayerHealthSlider != null && _otherPlayer != null)
            {
                secondaryPlayerHealthSlider.value = Mathf.Clamp(
                    actual,
                    0f,
                    _otherPlayer.VidaMaxima
                );
            }
        }

        private void ActualizarIconosPersonajes()
        {
            if (_localPlayer != null)
            {
                if (_localPlayer.CharacterId == CharacterIds.Ayla)
                {
                    if (mainPlayerIcon != null)
                        mainPlayerIcon.sprite = aylaIcon;
                    if (secondaryPlayerIcon != null && _otherPlayer == null)
                        secondaryPlayerIcon.sprite = zendreIcon;
                }
                else if (_localPlayer.CharacterId == CharacterIds.Zendre)
                {
                    if (mainPlayerIcon != null)
                        mainPlayerIcon.sprite = zendreIcon;
                    if (secondaryPlayerIcon != null && _otherPlayer == null)
                        secondaryPlayerIcon.sprite = aylaIcon;
                }
            }

            if (_otherPlayer != null && secondaryPlayerIcon != null)
            {
                secondaryPlayerIcon.sprite =
                    _otherPlayer.CharacterId == CharacterIds.Ayla ? aylaIcon : zendreIcon;
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

            float cooldownSecundario =
                _localPlayer.CharacterId == CharacterIds.Zendre
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
