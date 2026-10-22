using TDOM.Data;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public class VfxExplosion : VfxBase
    {
        [SerializeField]
        private VfxExplosionProfile _perfil;

        public override VfxProfile Perfil => _perfil;

        protected override void Avanzar(float t)
        {
            Visual.position = B;
            Visual.rotation = MirarHacia(TangenteEnCurva(1f));
            Visual.localScale = Vector3.one * (_perfil.radio * _perfil.escala.Evaluate(t));
        }
    }
}
