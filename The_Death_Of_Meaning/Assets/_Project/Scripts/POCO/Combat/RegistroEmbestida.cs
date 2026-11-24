using System;
using System.Collections.Generic;

namespace TDOM.Gameplay.Combat
{
    public sealed class RegistroEmbestida
    {
        private const float Tolerancia = 0.001f;
        private readonly HashSet<ulong> _alcanzados = new HashSet<ulong>();
        private float _siguienteInicio;
        private float _duracion;
        private float _distanciaRestante;
        private float _tiempo;

        public bool TryIniciar(float ahora, float cooldown, float duracion, float distancia)
        {
            if (
                ahora < _siguienteInicio
                || !EsFinito(ahora)
                || !EsFinito(cooldown)
                || !EsFinito(duracion)
                || !EsFinito(distancia)
                || cooldown < 0f
                || duracion <= 0f
                || distancia <= 0f
            )
                return false;

            _siguienteInicio = ahora + Math.Max(cooldown, duracion);
            _duracion = duracion;
            _distanciaRestante = distancia;
            _tiempo = 0f;
            _alcanzados.Clear();
            return true;
        }

        public bool TryAvanzar(float tiempo, float distancia)
        {
            if (
                !EsFinito(tiempo)
                || !EsFinito(distancia)
                || tiempo <= _tiempo
                || tiempo > _duracion + Tolerancia
                || distancia < 0f
                || distancia > _distanciaRestante + Tolerancia
            )
                return false;

            _tiempo = tiempo;
            _distanciaRestante -= distancia;
            return true;
        }

        public bool TryRegistrar(ulong id) => _alcanzados.Add(id);

        private static bool EsFinito(float valor) =>
            !float.IsNaN(valor) && !float.IsInfinity(valor);
    }
}
