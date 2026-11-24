using System;

namespace TDOM.Gameplay.Combat
{
    public sealed class RitmoAtaque
    {
        private const float SinAceleracion = 1f;

        public static readonly RitmoAtaque Normal = new RitmoAtaque(SinAceleracion, SinAceleracion);

        public RitmoAtaque(float frecuencia, float velocidad)
        {
            if (frecuencia <= 0f)
                throw new ArgumentOutOfRangeException(nameof(frecuencia), "Debe ser mayor que 0.");
            if (velocidad <= 0f)
                throw new ArgumentOutOfRangeException(nameof(velocidad), "Debe ser mayor que 0.");

            Frecuencia = frecuencia;
            Velocidad = velocidad;
        }

        public float Frecuencia { get; }

        public float Velocidad { get; }

        public float Espera(float esperaBase) => esperaBase / Frecuencia;

        public float Duracion(float duracionBase) => duracionBase / Velocidad;

        public float VelocidadConAvisoLegible(float avisoBase, float avisoMinimo)
        {
            if (avisoMinimo <= 0f)
                return Velocidad;

            float velocidadMaximaLegible = Math.Max(SinAceleracion, avisoBase / avisoMinimo);
            return Math.Min(Velocidad, velocidadMaximaLegible);
        }
    }
}
