using System;

namespace TDOM.Gameplay.Vfx
{
    public class CurvaDestelloGolpe
    {
        private readonly float _duracion;
        private float _transcurrido;

        public bool Activo { get; private set; }

        public CurvaDestelloGolpe(float duracion)
        {
            _duracion = Math.Max(0f, duracion);
        }

        public static bool EsGolpe(float vidaAnterior, float vidaActual) =>
            vidaActual < vidaAnterior;

        public void Reiniciar()
        {
            _transcurrido = 0f;
            Activo = true;
        }

        public float Avanzar(float dt, Func<float, float> curva, float intensidadMaxima)
        {
            if (!Activo)
                return 0f;

            float progreso = _duracion > 0f ? Math.Min(1f, _transcurrido / _duracion) : 1f;
            if (progreso >= 1f)
            {
                Activo = false;
                return 0f;
            }

            _transcurrido += Math.Max(0f, dt);
            return Math.Clamp(curva(progreso), 0f, 1f) * Math.Max(0f, intensidadMaxima);
        }
    }
}
