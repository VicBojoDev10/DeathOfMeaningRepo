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
        private Transform _objetivo;
        private Vector3 _posicionPrevia;
        private Vector3 _direccion = Vector3.forward;

        public override VfxProfile Perfil => _perfil;

        private bool Detenido => Principal != null && !Principal.isEmitting;

        public void Seguir(Transform objetivo) => _objetivo = objetivo;

        protected override void Awake()
        {
            base.Awake();
            if (Principal != null)
                _rendererLineas = Principal.GetComponent<ParticleSystemRenderer>();
        }

        protected override void AlIniciar()
        {
            _direccion = RotacionBase * Vector3.forward;
            if (_objetivo != null)
                _posicionPrevia = _objetivo.position;

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
            if (t > 0f && Detenido)
                return;

            if (_objetivo != null)
            {
                ColocarEnObjetivo();
                return;
            }

            float avance = _perfil.avance.Evaluate(t);
            Visual.position = PuntoEnCurva(avance);
            Visual.rotation = MirarHacia(TangenteEnCurva(avance));
        }

        private void LateUpdate()
        {
            if (!Activo || _objetivo == null || Detenido)
                return;

            _direccion = DireccionEmbestida.Calcular(
                _objetivo.position - _posicionPrevia,
                _direccion,
                _perfil.velocidadMinimaGiro,
                _perfil.suavizadoGiro,
                Time.deltaTime
            );
            _posicionPrevia = _objetivo.position;
            ColocarEnObjetivo();
        }

        private void ColocarEnObjetivo()
        {
            Quaternion mirada = Quaternion.LookRotation(_direccion, Vector3.up);
            Visual.position = _objetivo.position + mirada * _perfil.puntoA;
            Visual.rotation = MirarHacia(_direccion);
        }
    }
}
