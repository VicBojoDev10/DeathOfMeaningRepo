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

        protected override void Avanzar(float t)
        {
            Visual.position = A;
            Visual.rotation = MirarHacia(TangenteEnCurva(0f));
            Visual.localScale = Vector3.one * _perfil.escala.Evaluate(t);
        }
    }
}
