using TDOM.Data;
using TDOM.Gameplay.Vfx;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public class VfxEmbestida : VfxBase
    {
        [SerializeField]
        private VfxEmbestidaProfile _perfil;

        private ParticleSystemRenderer _rendererLineas;

        public override VfxProfile Perfil => _perfil;

        protected override void Awake()
        {
            base.Awake();
            if (Principal != null)
                _rendererLineas = Principal.GetComponent<ParticleSystemRenderer>();
        }

        protected override void AlIniciar()
        {
            if (Principal == null)
                return;

            var main = Principal.main;
            main.startSize = _perfil.grosorLineas;
            main.startSpeed = _perfil.velocidadLineas;
            main.startLifetime = _perfil.vidaLineas;

            var emision = Principal.emission;
            emision.rateOverTime = _perfil.lineasPorSegundo;

            var forma = Principal.shape;
            forma.radius = _perfil.radioLineas;

            if (_rendererLineas != null)
                _rendererLineas.lengthScale = _perfil.longitudLineas / _perfil.grosorLineas;
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
            Visual.rotation = MirarHacia(TangenteEnCurva(avance));
        }
    }
}
