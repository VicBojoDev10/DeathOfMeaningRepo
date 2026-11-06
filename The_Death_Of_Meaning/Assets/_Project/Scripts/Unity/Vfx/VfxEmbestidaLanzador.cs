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
        private Quaternion _rotacionEmbestida = Quaternion.identity;

        private void Awake()
        {
            if (_vfxRed == null)
                _vfxRed = GetComponentInParent<VfxRed>();
        }

        private void Start()
        {
            VfxPool pool = _vfxRed != null ? _vfxRed.Pool : null;
            if (pool == null || _efecto == null)
                return;

            if (pool.Pedir(pool.Indice(_efecto)) is VfxEmbestida instancia)
                instancia.Seguir(transform);
        }

        public void Lanzar(Vector3 posicion, Vector3 velocidad, Quaternion rotacionPorDefecto)
        {
            _rotacionEmbestida = RotacionDe(velocidad, rotacionPorDefecto);
            if (_vfxRed != null && _efecto != null)
                _vfxRed.Lanzar(_efecto, posicion, _rotacionEmbestida);
        }

        public void Actualizar(bool embestidaActiva, Vector3 posicion, Vector3 velocidad)
        {
            if (embestidaActiva)
                _rotacionEmbestida = RotacionDe(velocidad, _rotacionEmbestida);

            _detectorFin.Actualizar(embestidaActiva);
            if (!_detectorFin.Termino || _vfxRed == null)
                return;

            if (_efecto != null)
                _vfxRed.Detener(_efecto);
            if (_impacto != null)
                _vfxRed.Lanzar(_impacto, posicion, _rotacionEmbestida);
        }

        private static Quaternion RotacionDe(Vector3 velocidad, Quaternion rotacionPorDefecto)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(velocidad, Vector3.up);
            return horizontal.sqrMagnitude > 0f
                ? Quaternion.LookRotation(horizontal, Vector3.up)
                : rotacionPorDefecto;
        }
    }
}
