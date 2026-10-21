using TDOM.Data;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public class VfxDisparo : VfxBase
    {
        [SerializeField]
        private VfxDisparoProfile _perfil;

        public override VfxProfile Perfil => _perfil;

        protected override void Avanzar(float t)
        {
            Visual.position = A;
            Visual.rotation = MirarHacia(TangenteEnCurva(0f));
            Visual.localScale = Vector3.one * (_perfil.tamano * _perfil.escala.Evaluate(t));
        }
    }
}
