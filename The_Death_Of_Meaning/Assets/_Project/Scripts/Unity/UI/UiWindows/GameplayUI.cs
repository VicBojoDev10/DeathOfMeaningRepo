using TDOM.Contracts;
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

        private PlayerRoot _localPlayer;
        private RectTransform _crosshairRect;
        private Vector3 _escalaBase;

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

        private void OnDisable()
        {
            _localPlayer = null;
            if (crosshairImage != null)
                crosshairImage.gameObject.SetActive(false);
            if (crosshairDefaultImage != null)
                crosshairDefaultImage.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            // Si no tenemos referencia al jugador local, lo buscamos en la escena
            if (_localPlayer == null)
            {
                BuscarJugadorLocal();
                if (_localPlayer == null)
                    return;
            }

            var camTransform = _localPlayer.CameraTransform;
            if (camTransform == null)
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

            // Raycast desde el centro de la cámara hacia adelante
            Ray ray = new Ray(camTransform.position, camTransform.forward);
            bool valido = false;

            if (
                Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    alcance,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Ignore
                )
            )
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
