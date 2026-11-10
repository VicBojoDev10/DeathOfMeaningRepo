using TDOM.Data;
using UnityEngine;

namespace TDOM.Unity.Audio
{
    public sealed class SfxAnimacion : MonoBehaviour
    {
        [SerializeField]
        private SfxRed _red;

        private void Awake()
        {
            if (_red == null)
                _red = GetComponentInParent<SfxRed>();
        }

        public void ReproducirSfx(Object banco)
        {
            if (TieneAutoridad() && banco is SfxBanco sfx)
                _red.ReproducirEnEmisor(sfx);
        }

        public void DetenerSfx(Object banco)
        {
            if (TieneAutoridad() && banco is SfxBanco sfx)
                _red.Detener(sfx);
        }

        private bool TieneAutoridad() => _red != null && _red.IsSpawned && _red.IsOwner;
    }
}
