using TDOM.Data;
using TDOM.Gameplay.Audio;
using UnityEngine;

namespace TDOM.Unity.Audio
{
    public sealed class SfxCombate : MonoBehaviour
    {
        [SerializeField]
        private SfxRed _red;

        [SerializeField]
        private SfxBanco[] _golpesCombo = new SfxBanco[0];

        [SerializeField]
        private SfxBanco _cargaLoop;

        [SerializeField]
        private SfxBanco _liberacionCargado;

        private bool _cargando;

        private void Awake()
        {
            if (_red == null)
                _red = GetComponentInParent<SfxRed>();
        }

        public void Golpe(int comboIndex) => ReproducirDe(_golpesCombo, comboIndex);

        public void Carga(bool activa)
        {
            if (_red == null || _cargaLoop == null || activa == _cargando)
                return;

            _cargando = activa;
            if (activa)
                _red.ReproducirEnEmisor(_cargaLoop);
            else
                _red.Detener(_cargaLoop);
        }

        public void LiberacionCargado()
        {
            Carga(false);
            if (_red != null && _liberacionCargado != null)
                _red.ReproducirEnEmisor(_liberacionCargado);
        }

        private void ReproducirDe(SfxBanco[] bancos, int indice)
        {
            int elegido = SfxPorIndice.Elegir(indice, bancos.Length);
            if (_red != null && elegido != SfxPorIndice.SinSfx)
                _red.ReproducirEnEmisor(bancos[elegido]);
        }
    }
}
