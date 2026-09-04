using UnityEngine;

namespace TDOM.Gameplay.Core
{
    public class CooldownTimer
    {
        private readonly float _duracion;
        private float _restante;
        public CooldownTimer(float duracion) => _duracion = duracion;
        public bool Listo => _restante <= 0f;
        public float Normalizado => Mathf.Clamp01(_restante / _duracion);
        public void Tick(float dt) => _restante -= dt;
        public void Disparar()     => _restante = _duracion;
    }
}
