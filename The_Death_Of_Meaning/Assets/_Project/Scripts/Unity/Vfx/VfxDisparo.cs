using TDOM.Data;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public class VfxDisparo : VfxBase
    {
        [SerializeField]
        private VfxDisparoProfile _perfil;

        [SerializeField]
        private Transform _salida;

        public override VfxProfile Perfil => _perfil;

        protected override void AlIniciar()
        {
            if (Principal == null)
                return;

            var estela = Principal.trails;
            estela.lifetime = _perfil.largoEstela;
        }

        protected override void Avanzar(float t)
        {
            float avance = _perfil.avance.Evaluate(t);
            Visual.position = PuntoEnCurva(avance);
            Visual.rotation = MirarHacia(TangenteEnCurva(avance));
            Visual.localScale = Vector3.one * (_perfil.tamano * _perfil.escala.Evaluate(t));

            if (_salida != null)
                _salida.SetPositionAndRotation(A, MirarHacia(TangenteEnCurva(0f)));
        }
    }
}
