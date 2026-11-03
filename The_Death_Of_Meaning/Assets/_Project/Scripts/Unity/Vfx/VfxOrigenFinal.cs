using TDOM.Data;
using TDOM.Gameplay.Vfx;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public class VfxOrigenFinal : VfxBase
    {
        [SerializeField]
        private VfxOrigenFinalProfile _perfil;

        public override VfxProfile Perfil => _perfil;

        protected override void AlIniciar()
        {
            if (Principal == null)
                return;

            var emision = Principal.emission;
            emision.rateOverDistance = _perfil.particulasResiduales;
            var main = Principal.main;
            main.startSize = _perfil.ancho;
        }

        protected override float Visibilidad(float tiempo) =>
            VisibilidadVfx.Factor(
                tiempo,
                _perfil.duracion,
                _perfil.aparicion,
                _perfil.desaparicion
            );

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
