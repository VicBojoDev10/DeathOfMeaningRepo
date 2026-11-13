using TDOM.Contracts;
using TDOM.Gameplay.Camera;
using TDOM.Gameplay.Core;
using TDOM.Unity.Player;
using TDOM.Unity.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        private Color cooldownEnUso = new Color(0.8f, 0.8f, 0.8f, 1f);

        [SerializeField]
        private Sprite zendreIcon;

        [SerializeField]
        private Sprite aylaIcon;

        [SerializeField]
        [Tooltip("Color del fondo detras del icono mientras se llena el cooldown.")]
        private Color cooldownFondo = new Color(0f, 0f, 0f, 0.3f);

        [SerializeField]
        private Image buttonPlayerIcon;

        [Header("PlayerHealth")]
        [SerializeField]
        private Slider mainPlayerHealthSlider;

        [SerializeField]
        private Slider secondaryPlayerHealthSlider;

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
        private RectTransform _marcadorAbatido;
        private TMP_Text _textoMarcadorAbatido;
        private TMP_Text _flechaMarcadorAbatido;
        private Color _colorBarraCompanero = Color.white;
        private DownedPlayerOutline _outlineCompanero;

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
            SetHudVisible(visible, visible);
        }

        public void SetHudVisible(bool visible, bool barraJefeVisible)
        {
            _hudVisible = visible;
            if (canvasGroup != null)
                canvasGroup.alpha = visible ? 1f : 0f;

            if (_barraVidaJefe != null)
                _barraVidaJefe.CambiarVisibilidadHud(barraJefeVisible);
        }

        public override void Show()
        {
            SetHudVisible(true);
            base.Show();
        }

        public override void Initialize()
        {
            base.Initialize();

            ConfigurarImagenCooldown(dashandembestidaslider);
            ConfigurarImagenCooldown(hookandgraplerslider);
            CrearFondoCooldown(dashandembestidaslider);
            CrearFondoCooldown(hookandgraplerslider);

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
                _otherPlayer.EstadoVida.OnValueChanged -= OnEstadoSecundarioChanged;
                _otherPlayer.TiempoAbatidoRestante.OnValueChanged -= OnEstadoAbatidoSecundarioChanged;
                _otherPlayer.ProgresoRevivir.OnValueChanged -= OnEstadoAbatidoSecundarioChanged;
            }

            _localPlayer = null;
            if (_outlineCompanero != null)
                _outlineCompanero.SetVisible(false);
            _outlineCompanero = null;
            _otherPlayer = null;
            _jugadorConEnergiaInicializado = null;
            if (_marcadorAbatido != null)
                _marcadorAbatido.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            ActualizarMarcadorAbatido();
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

            if (crosshairDefaultImage != null)
            {
                bool mostrarMiraDefault = soloAlApuntar && !input.AimHeld;
                if (crosshairDefaultImage.gameObject.activeSelf != mostrarMiraDefault)
                    crosshairDefaultImage.gameObject.SetActive(mostrarMiraDefault);
            }
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
            _otherPlayer.EstadoVida.OnValueChanged += OnEstadoSecundarioChanged;
            _otherPlayer.TiempoAbatidoRestante.OnValueChanged += OnEstadoAbatidoSecundarioChanged;
            _otherPlayer.ProgresoRevivir.OnValueChanged += OnEstadoAbatidoSecundarioChanged;

            if (secondaryPlayerHealthSlider != null)
            {
                var fill = secondaryPlayerHealthSlider.fillRect != null
                    ? secondaryPlayerHealthSlider.fillRect.GetComponent<Image>() : null;
                if (fill != null)
                    _colorBarraCompanero = fill.color;
                secondaryPlayerHealthSlider.minValue = 0f;
                secondaryPlayerHealthSlider.maxValue = _otherPlayer.VidaMaxima;
                secondaryPlayerHealthSlider.value = Mathf.Clamp(
                    _otherPlayer.Vida.Value,
                    0f,
                    _otherPlayer.VidaMaxima
                );
            }

            ActualizarIconosPersonajes();
            ActualizarEstadoCompanero();
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
            if (secondaryPlayerHealthSlider != null && _otherPlayer != null
                && _otherPlayer.EstadoVida.Value == PlayerLifeState.Vivo)
            {
                secondaryPlayerHealthSlider.value = Mathf.Clamp(
                    actual,
                    0f,
                    _otherPlayer.VidaMaxima
                );
            }
        }

        private void OnEstadoSecundarioChanged(PlayerLifeState anterior, PlayerLifeState actual) =>
            ActualizarEstadoCompanero();

        private void OnEstadoAbatidoSecundarioChanged(float anterior, float actual) =>
            ActualizarEstadoCompanero();

        private void ActualizarEstadoCompanero()
        {
            if (_otherPlayer == null)
                return;
            bool abatido = _otherPlayer.EstadoVida.Value == PlayerLifeState.Abatido;
            if (_outlineCompanero == null)
                _outlineCompanero = _otherPlayer.GetComponent<DownedPlayerOutline>()
                    ?? _otherPlayer.gameObject.AddComponent<DownedPlayerOutline>();
            _outlineCompanero.SetVisible(abatido);
            if (secondaryPlayerHealthSlider == null)
                return;
            var fill = secondaryPlayerHealthSlider.fillRect != null
                ? secondaryPlayerHealthSlider.fillRect.GetComponent<Image>() : null;
            secondaryPlayerHealthSlider.maxValue = abatido ? 1f : _otherPlayer.VidaMaxima;
            secondaryPlayerHealthSlider.value = abatido
                ? (_otherPlayer.ProgresoRevivir.Value > 0f
                    ? Mathf.Clamp01(_otherPlayer.ProgresoRevivir.Value)
                    : _otherPlayer.TiempoAbatidoNormalizado)
                : Mathf.Clamp(_otherPlayer.Vida.Value, 0f, _otherPlayer.VidaMaxima);
            if (fill != null)
                fill.color = abatido ? new Color(1f, 0.48f, 0.12f, 1f) : _colorBarraCompanero;
            if (abatido)
                CrearMarcadorAbatido();
            else if (_marcadorAbatido != null)
                _marcadorAbatido.gameObject.SetActive(false);
        }

        private void CrearMarcadorAbatido()
        {
            if (_marcadorAbatido != null)
                return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null)
                return;
            var root = new GameObject("DownedAllyMarker", typeof(RectTransform));
            _marcadorAbatido = root.GetComponent<RectTransform>();
            _marcadorAbatido.SetParent(canvas.transform, false);
            _marcadorAbatido.anchorMin = _marcadorAbatido.anchorMax = new Vector2(0.5f, 0.5f);
            _marcadorAbatido.sizeDelta = new Vector2(180f, 54f);
            var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var arrowRect = arrow.GetComponent<RectTransform>();
            arrowRect.SetParent(_marcadorAbatido, false);
            arrowRect.sizeDelta = new Vector2(36f, 36f);
            _flechaMarcadorAbatido = arrow.GetComponent<TMP_Text>();
            _flechaMarcadorAbatido.text = "▲";
            _flechaMarcadorAbatido.alignment = TextAlignmentOptions.Center;
            _flechaMarcadorAbatido.fontSize = 34f;
            _flechaMarcadorAbatido.color = new Color(1f, 0.48f, 0.12f, 1f);
            _flechaMarcadorAbatido.raycastTarget = false;
            _textoMarcadorAbatido = new GameObject("Status", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            _textoMarcadorAbatido.transform.SetParent(_marcadorAbatido, false);
            _textoMarcadorAbatido.rectTransform.anchoredPosition = new Vector2(0f, -29f);
            _textoMarcadorAbatido.rectTransform.sizeDelta = new Vector2(180f, 28f);
            _textoMarcadorAbatido.alignment = TextAlignmentOptions.Center;
            _textoMarcadorAbatido.fontSize = 20f;
            _textoMarcadorAbatido.color = Color.white;
            _textoMarcadorAbatido.raycastTarget = false;
            _textoMarcadorAbatido.text = "▼";
        }

        private void ActualizarMarcadorAbatido()
        {
            if (_localPlayer == null || _otherPlayer == null || _otherPlayer.EstadoVida.Value != PlayerLifeState.Abatido)
            {
                if (_marcadorAbatido != null) _marcadorAbatido.gameObject.SetActive(false);
                return;
            }
            CrearMarcadorAbatido();
            var cam = UnityEngine.Camera.main;
            if (_marcadorAbatido == null || cam == null)
                return;
            _marcadorAbatido.gameObject.SetActive(true);
            Vector3 world = _otherPlayer.transform.position + Vector3.up * 1.8f;
            Vector3 screen = cam.WorldToScreenPoint(world);
            Vector2 direction = new Vector2(screen.x - Screen.width * .5f, screen.y - Screen.height * .5f);
            bool visible = screen.z > 0f && screen.x >= 12f && screen.x <= Screen.width - 12f
                && screen.y >= 12f && screen.y <= Screen.height - 12f;
            if (!visible)
            {
                if (screen.z < 0f) direction = -direction;
                direction = direction.sqrMagnitude > .001f ? direction.normalized : Vector2.up;
                float edgeX = Screen.width * .5f - 48f;
                float edgeY = Screen.height * .5f - 48f;
                float scale = Mathf.Min(edgeX / Mathf.Max(Mathf.Abs(direction.x), .001f), edgeY / Mathf.Max(Mathf.Abs(direction.y), .001f));
                screen = new Vector3(Screen.width * .5f + direction.x * scale, Screen.height * .5f + direction.y * scale, 0f);
            }
            else
                direction = Vector2.up;
            _marcadorAbatido.position = screen;
            _flechaMarcadorAbatido.rectTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            float remaining = _otherPlayer.TiempoAbatidoRestante.Value;
            _textoMarcadorAbatido.text = visible
                ? $"{remaining:0}s  •  {Vector3.Distance(_localPlayer.transform.position, _otherPlayer.transform.position):0}m"
                : $"ALIADO ABATIDO  •  {remaining:0}s  •  {Vector3.Distance(_localPlayer.transform.position, _otherPlayer.transform.position):0}m";
        }

        private void ActualizarIconosPersonajes()
        {
            if (_localPlayer != null)
            {
                if (_localPlayer.CharacterId == CharacterIds.Ayla)
                {
                    if (buttonPlayerIcon != null)
                        buttonPlayerIcon.sprite = aylaIcon;
                }
                else if (_localPlayer.CharacterId == CharacterIds.Zendre)
                {
                    if (buttonPlayerIcon != null)
                        buttonPlayerIcon.sprite = zendreIcon;
                }
            }
        }

        private static void ConfigurarImagenCooldown(Image imagen)
        {
            if (imagen == null)
                return;

            if (imagen.sprite == null)
                imagen.sprite = SpriteBlancoCooldown();

            imagen.type = Image.Type.Filled;
            imagen.fillMethod = Image.FillMethod.Radial360;
            imagen.fillOrigin = (int)Image.Origin360.Top;
            imagen.fillClockwise = true;
            imagen.raycastTarget = false;
        }

        private static Sprite _spriteBlancoCooldown;

        private static Sprite SpriteBlancoCooldown()
        {
            if (_spriteBlancoCooldown == null)
            {
                Texture2D textura = Texture2D.whiteTexture;
                _spriteBlancoCooldown = Sprite.Create(
                    textura,
                    new Rect(0f, 0f, textura.width, textura.height),
                    new Vector2(0.5f, 0.5f)
                );
            }
            return _spriteBlancoCooldown;
        }
        private void CrearFondoCooldown(Image frente)
        {
            if (frente == null || frente.transform.parent == null)
                return;

            string nombre = frente.name + "_Fondo";
            if (frente.transform.parent.Find(nombre) != null)
                return;

            var fondoObjeto = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            var fondoRect = (RectTransform)fondoObjeto.transform;
            fondoRect.SetParent(frente.transform.parent, false);

            RectTransform origen = frente.rectTransform;
            fondoRect.anchorMin = origen.anchorMin;
            fondoRect.anchorMax = origen.anchorMax;
            fondoRect.pivot = origen.pivot;
            fondoRect.anchoredPosition = origen.anchoredPosition;
            fondoRect.sizeDelta = origen.sizeDelta;
            fondoRect.localScale = origen.localScale;
            fondoRect.localRotation = origen.localRotation;
            fondoRect.SetSiblingIndex(frente.transform.GetSiblingIndex());

            var fondo = fondoObjeto.GetComponent<Image>();
            fondo.sprite = frente.sprite;
            fondo.color = cooldownFondo;
            fondo.raycastTarget = false;
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
            imagen.fillAmount = 1f - normalizado;
            imagen.color = normalizado <= 0.001f ? cooldownDisponible : cooldownEnUso;
        }
    }
}
