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

        [SerializeField]
        private SfxBanco _gancho;

        private void Awake()
        {
            if (_red == null)
                _red = GetComponentInParent<SfxRed>();
        }

        public void Dash() => ReproducirSiExiste(_dash);

        public void Gancho() => ReproducirSiExiste(_gancho);

        private void ReproducirSiExiste(SfxBanco banco)
        {
            if (_red != null && banco != null)
                _red.ReproducirEnEmisor(banco);
        }
    }
}
