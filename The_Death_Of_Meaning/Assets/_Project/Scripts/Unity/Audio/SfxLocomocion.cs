using TDOM.Data;
using UnityEngine;

namespace TDOM.Unity.Audio
{
    public sealed class SfxLocomocion : MonoBehaviour
    {
        [SerializeField]
        private SfxRed _red;

        [SerializeField]
        private SfxBanco _dash;

        private void Awake()
        {
            if (_red == null)
                _red = GetComponentInParent<SfxRed>();
        }

        public void Dash() => ReproducirSiExiste(_dash);

        private void ReproducirSiExiste(SfxBanco banco)
        {
            if (_red != null && banco != null)
                _red.ReproducirEnEmisor(banco);
        }
    }
}
