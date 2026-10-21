using TDOM.Data;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public class VfxOrigenFinal : VfxBase
    {
        [SerializeField]
        private VfxOrigenFinalProfile _perfil;

        public override VfxProfile Perfil => _perfil;

        protected override void Avanzar(float t)
        {
            float avance = _perfil.avance.Evaluate(t);
            Visual.position = PuntoEnCurva(avance);
            Visual.rotation = _perfil.orientarATrayectoria
                ? MirarHacia(TangenteEnCurva(avance))
                : RotacionBase * Orientacion;
        }
    }
}
