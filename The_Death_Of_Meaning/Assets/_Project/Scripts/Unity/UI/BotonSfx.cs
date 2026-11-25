using TDOM.Data;
using TDOM.Gameplay.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class BotonSfx : MonoBehaviour
    {
        [SerializeField]
        private TipoSonidoBoton _tipo = TipoSonidoBoton.Neutral;

        [SerializeField]
        private SonidosBotonesUI _sonidos;

        [SerializeField]
        private AudioSource _fuente;

        private Button _boton;

        private void Awake() => _boton = GetComponent<Button>();

        private void OnEnable() => _boton.onClick.AddListener(Reproducir);

        private void OnDisable() => _boton.onClick.RemoveListener(Reproducir);

        private void Reproducir()
        {
            if (_sonidos == null || _fuente == null)
                return;

            AudioClip clip = SonidoBoton.Elegir(
                _tipo,
                _sonidos.positivo,
                _sonidos.negativo,
                _sonidos.neutral,
                _sonidos.especial
            );
            if (clip != null)
                _fuente.PlayOneShot(clip, _sonidos.volumen);
        }
    }
}
