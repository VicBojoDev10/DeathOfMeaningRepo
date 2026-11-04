using TDOM.Gameplay.Vfx;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public sealed class VfxDash : MonoBehaviour
    {
        [SerializeField]
        private VfxRed _vfxRed;

        [SerializeField, Min(0f)]
        private float _ventanaTrasGancho = 1f;

        [Header("Efecto por tipo de dash")]
        [SerializeField]
        private VfxBase _efectoSuelo;

        [SerializeField]
        private VfxBase _efectoAire;

        [SerializeField]
        private VfxBase _efectoCancelacion;

        [Header("Nube de polvo en el suelo")]
        [SerializeField]
        private VfxBase _nube;

        [SerializeField]
        private VfxBase _nubeGrande;

        private ContextoDashVfx _contexto;

        private void Awake()
        {
            if (_vfxRed == null)
                _vfxRed = GetComponentInParent<VfxRed>();
            _contexto = new ContextoDashVfx(_ventanaTrasGancho);
        }

        public void Capturar(
            bool enSuelo,
            bool corriendo,
            bool cancelaAtaque,
            bool ganchoActivo,
            float dt
        ) => _contexto.Capturar(enSuelo, corriendo, cancelaAtaque, ganchoActivo, dt);

        public void Lanzar(Vector3 posicion, Vector3 velocidad, Quaternion rotacionPorDefecto)
        {
            if (_vfxRed == null)
                return;

            Quaternion rotacion = RotacionDelDash(velocidad, rotacionPorDefecto);
            LanzarSiExiste(EfectoDe(_contexto.Tipo), posicion, rotacion);
            LanzarSiExiste(NubeDe(_contexto.Nube), posicion, rotacion);
        }

        private VfxBase EfectoDe(TipoDashVfx tipo) =>
            tipo switch
            {
                TipoDashVfx.Aire => _efectoAire,
                TipoDashVfx.Cancelacion => _efectoCancelacion,
                _ => _efectoSuelo,
            };

        private VfxBase NubeDe(NubeDashVfx nube) =>
            nube switch
            {
                NubeDashVfx.Normal => _nube,
                NubeDashVfx.Grande => _nubeGrande,
                _ => null,
            };

        private void LanzarSiExiste(VfxBase efecto, Vector3 posicion, Quaternion rotacion)
        {
            if (efecto != null)
                _vfxRed.Lanzar(efecto, posicion, rotacion);
        }

        private static Quaternion RotacionDelDash(Vector3 velocidad, Quaternion rotacionPorDefecto)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(velocidad, Vector3.up);
            return horizontal.sqrMagnitude > 0f
                ? Quaternion.LookRotation(horizontal, Vector3.up)
                : rotacionPorDefecto;
        }
    }
}
