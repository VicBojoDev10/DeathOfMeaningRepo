using TDOM.Data;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public class VfxCarga : VfxBase
    {
        [SerializeField]
        private VfxCargaProfile _perfil;

        public override VfxProfile Perfil => _perfil;

        protected override bool TiempoAgotado(float tiempo) => tiempo >= _perfil.duracionMaxima;

        protected override void AlIniciar()
        {
            if (Principal == null)
                return;

            var forma = Principal.shape;
            forma.radius = _perfil.radioCentro;
            var main = Principal.main;
            main.startSpeed = _perfil.velocidadParticulas;
        }

        protected override void Avanzar(float t)
        {
            Visual.position = A;
            Visual.rotation = MirarHacia(TangenteEnCurva(0f));
            Visual.localScale = Vector3.one * _perfil.escala.Evaluate(t);
        }
    }
}
