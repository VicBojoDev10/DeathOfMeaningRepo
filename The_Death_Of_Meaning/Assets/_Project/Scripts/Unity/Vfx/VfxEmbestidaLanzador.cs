using TDOM.Gameplay.Vfx;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public sealed class VfxEmbestidaLanzador : MonoBehaviour
    {
        [SerializeField]
        private VfxRed _vfxRed;

        [SerializeField]
        private VfxBase _efecto;

        [SerializeField]
        private VfxBase _impacto;

        private readonly DetectorFinEmbestida _detectorFin = new DetectorFinEmbestida();

        private void Awake()
        {
            if (_vfxRed == null)
                _vfxRed = GetComponentInParent<VfxRed>();
        }

        public void Lanzar(Vector3 posicion, Vector3 velocidad, Quaternion rotacionPorDefecto)
        {
            if (_vfxRed == null || _efecto == null)
                return;

            Vector3 horizontal = Vector3.ProjectOnPlane(velocidad, Vector3.up);
            Quaternion rotacion =
                horizontal.sqrMagnitude > 0f
                    ? Quaternion.LookRotation(horizontal, Vector3.up)
                    : rotacionPorDefecto;
            _vfxRed.Lanzar(_efecto, posicion, rotacion);
        }

        public void Actualizar(bool embestidaActiva, Vector3 posicion, Quaternion rotacion)
        {
            _detectorFin.Actualizar(embestidaActiva);
            if (!_detectorFin.Termino || _vfxRed == null)
                return;

            if (_efecto != null)
                _vfxRed.Detener(_efecto);
            if (_impacto != null)
                _vfxRed.Lanzar(_impacto, posicion, rotacion);
        }
    }
}
