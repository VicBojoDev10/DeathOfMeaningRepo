using System;
using System.Collections.Generic;

namespace TDOM.Gameplay.Combat
{
    public sealed class RegistroDanioVentana
    {
        private readonly Queue<(float tiempo, float danio)> _eventos = new Queue<(float, float)>();
        private float _acumulado;

        public float Acumulado => _acumulado;

        public bool Registrar(
            float danio,
            float tiempo,
            float ventana,
            float vidaMaxima,
            float porcentaje
        )
        {
            if (ventana <= 0f || vidaMaxima <= 0f || porcentaje <= 0f)
                throw new ArgumentOutOfRangeException(
                    "La ventana, vida máxima y porcentaje deben ser positivos."
                );
            while (_eventos.Count > 0 && tiempo - _eventos.Peek().tiempo > ventana)
                _acumulado -= _eventos.Dequeue().danio;
            if (danio > 0f)
            {
                _eventos.Enqueue((tiempo, danio));
                _acumulado += danio;
            }
            if (_acumulado + 1e-5f < vidaMaxima * porcentaje)
                return false;
            Reiniciar();
            return true;
        }

        public void Reiniciar()
        {
            _eventos.Clear();
            _acumulado = 0f;
        }
    }
}
