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
        private readonly float _distanciaFrenadoJefe;
        private readonly float _velocidadReboteJefe;
        private readonly float _anguloReboteJefe;
        private readonly CooldownTimer _cooldown;
        private Vector3 _punto;
        private float _tiempoTranscurrido;
        private bool _haciaJefe;

        public bool Activo { get; private set; }
        public Vector3 Punto => _punto;
        public float CooldownNormalizado => _cooldown.Normalizado;

        public GrappleResolver(
            float alcance,
            float velocidadTraccion,
            float cooldown,
            float distanciaLlegada,
            float duracionMax,
            float distanciaFrenadoJefe = 0f,
            float velocidadReboteJefe = 0f,
            float anguloReboteJefe = 45f
        )
        {
            _distanciaFrenadoJefe = distanciaFrenadoJefe;
            _velocidadReboteJefe = velocidadReboteJefe;
            _anguloReboteJefe = anguloReboteJefe;
            _alcance = alcance;
            _velocidadTraccion = velocidadTraccion;
            _distanciaLlegada = distanciaLlegada;
            _duracionMax = duracionMax;
            _cooldown = new CooldownTimer(cooldown);
        }

        public GrappleResolver(GrappleProfile p)
            : this(
                p.Range,
                p.PullSpeed,
                p.Cooldown,
                p.ArrivalDistance,
                p.MaxDuration,
                p.BossStopDistance,
                p.BossReboundSpeed,
                p.BossReboundAngle
            ) { }

        /// <summary>
        /// <paramref name="haciaJefe"/>: el punto está en el jefe. El jalón se corta a la distancia
        /// de frenado y el jugador sale rebotado hacia atrás, sin llegar a tocarlo.
        /// </summary>
        public bool TryIniciar(Vector3 origen, Vector3 punto, bool haciaJefe = false)
        {
            if (Activo || !_cooldown.Listo || Vector3.Distance(origen, punto) > _alcance)
                return false;

            Activo = true;
            _tiempoTranscurrido = 0f;
            _punto = punto;
            _haciaJefe = haciaJefe;
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

            if (_haciaJefe && _distanciaFrenadoJefe > 0f && d.magnitude <= _distanciaFrenadoJefe)
            {
                Rebotar(e, d);
                return;
            }

            if (d.magnitude <= _distanciaLlegada || _tiempoTranscurrido > _duracionMax)
            {
                Activo = false;
                e.Phase = LocomotionPhase.Airborne;
                return;
            }

            e.Velocity = d.normalized * _velocidadTraccion;
            e.Phase = LocomotionPhase.Grappling;
        }

        // Salto automático hacia atrás (alejándose del punto) con la elevación configurada, para
        // quedar a distancia de golpe sin tocar al jefe.
        private void Rebotar(LocomotionState e, Vector3 haciaElPunto)
        {
            Activo = false;
            e.Phase = LocomotionPhase.Airborne;

            Vector3 atras = -new Vector3(haciaElPunto.x, 0f, haciaElPunto.z);
            atras = atras.sqrMagnitude > 1e-6f ? atras.normalized : Vector3.zero;

            float angulo = _anguloReboteJefe * Mathf.Deg2Rad;
            e.Velocity =
                atras * (_velocidadReboteJefe * Mathf.Cos(angulo))
                + Vector3.up * (_velocidadReboteJefe * Mathf.Sin(angulo));
        }
    }
}
