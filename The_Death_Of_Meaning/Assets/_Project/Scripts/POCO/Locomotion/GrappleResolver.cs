using TDOM.Contracts;
using TDOM.Data;
using TDOM.Gameplay.Core;
using UnityEngine;

namespace TDOM.Gameplay
{
    public class GrappleResolver
    {
        private readonly float _alcance;
        private readonly float _velocidadTraccion;
        private readonly float _distanciaLlegada;
        private readonly float _duracionMax;
        private readonly CooldownTimer _cooldown;
        private Vector3 _punto;
        private float _tiempoTranscurrido;

        public bool Activo { get; private set; }
        public Vector3 Punto => _punto;

        public GrappleResolver(float alcance, float velocidadTraccion, float cooldown, float distanciaLlegada, float duracionMax)
        {
            _alcance = alcance;
            _velocidadTraccion = velocidadTraccion;
            _distanciaLlegada = distanciaLlegada;
            _duracionMax = duracionMax;
            _cooldown = new CooldownTimer(cooldown);
        }

        public GrappleResolver(GrappleProfile p)
            : this(p.Range, p.PullSpeed, p.Cooldown, p.ArrivalDistance, p.MaxDuration)
        {
        }

        public bool TryIniciar(Vector3 origen, Vector3 punto)
        {
            if (Activo || !_cooldown.Listo || Vector3.Distance(origen, punto) > _alcance)
                return false;

            Activo = true;
            _tiempoTranscurrido = 0f;
            _punto = punto;
            _cooldown.Disparar();
            return true;
        }

        public void Cancelar()
        {
            Activo = false;
        }

        public void Tick(LocomotionState e, Vector3 posicion, float dt)
        {
            _cooldown.Tick(dt);

            if (!Activo)
                return;

            _tiempoTranscurrido += dt;
            Vector3 d = _punto - posicion;

            if (d.magnitude <= _distanciaLlegada || _tiempoTranscurrido > _duracionMax)
            {
                Activo = false;
                e.Phase = LocomotionPhase.Airborne;
                return;
            }

            e.Velocity = d.normalized * _velocidadTraccion;
            e.Phase = LocomotionPhase.Grappling;
        }
    }
}
