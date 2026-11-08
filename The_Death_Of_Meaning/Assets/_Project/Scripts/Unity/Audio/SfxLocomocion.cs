using TDOM.Data;
using TDOM.Gameplay.Vfx;
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

        [SerializeField]
        private SfxBanco _dobleSalto;

        [SerializeField]
        private SfxBanco _embestidaArranque;

        [SerializeField]
        private SfxBanco _embestidaAvance;

        [SerializeField]
        private SfxBanco _embestidaFinal;

        private readonly DetectorFinEmbestida _finEmbestida = new DetectorFinEmbestida();

        private void Awake()
        {
            if (_red == null)
                _red = GetComponentInParent<SfxRed>();
        }

        public void Dash() => ReproducirSiExiste(_dash);

        public void Gancho() => ReproducirSiExiste(_gancho);

        public void DobleSalto() => ReproducirSiExiste(_dobleSalto);

        public void EmbestidaInicio()
        {
            ReproducirSiExiste(_embestidaArranque);
            ReproducirSiExiste(_embestidaAvance);
        }

        public void ActualizarEmbestida(bool activa)
        {
            _finEmbestida.Actualizar(activa);
            if (!_finEmbestida.Termino)
                return;

            if (_red != null && _embestidaAvance != null)
                _red.Detener(_embestidaAvance);
            ReproducirSiExiste(_embestidaFinal);
        }

        private void ReproducirSiExiste(SfxBanco banco)
        {
            if (_red != null && banco != null)
                _red.ReproducirEnEmisor(banco);
        }
    }
}
