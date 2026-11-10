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

        private void Awake()
        {
            if (_red == null)
                _red = GetComponentInParent<SfxRed>();
        }

        public void Golpe(int comboIndex) => ReproducirDe(_golpesCombo, comboIndex);

        private void ReproducirDe(SfxBanco[] bancos, int indice)
        {
            int elegido = SfxPorIndice.Elegir(indice, bancos.Length);
            if (_red != null && elegido != SfxPorIndice.SinSfx)
                _red.ReproducirEnEmisor(bancos[elegido]);
        }
    }
}
