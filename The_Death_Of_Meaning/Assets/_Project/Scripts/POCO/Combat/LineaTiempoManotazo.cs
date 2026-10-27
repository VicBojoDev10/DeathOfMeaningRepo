using UnityEngine;

namespace TDOM.Gameplay.Combat
{
    public enum FaseManotazo
    {
        Burbuja,
        Salida,
        Barrido,
        Retraccion,
        Terminado,
    }

    /// <summary>Estado visual y de daño de la Mano en un instante del ataque.</summary>
    public readonly struct EstadoManotazo
    {
        public EstadoManotazo(FaseManotazo fase, float longitud, float angulo, float burbuja)
        {
            Fase = fase;
            Longitud = longitud;
            Angulo = angulo;
            Burbuja = burbuja;
        }

        public FaseManotazo Fase { get; }

        /// <summary>Fracción del radio que la columna lleva extendida (0 a 1).</summary>
        public float Longitud { get; }

        /// <summary>Grados respecto a la rotación base (con el sentido aplicado).</summary>
        public float Angulo { get; }

        /// <summary>Intensidad de la burbuja de lava del origen (0 a 1).</summary>
        public float Burbuja { get; }

        /// <summary>El daño solo existe durante el barrido, con la columna completa.</summary>
        public bool DanoActivo => Fase == FaseManotazo.Barrido;
    }

    /// <summary>
    /// Línea de tiempo de la Mano de AREK: el aviso se reparte en burbuja de lava (la primera
    /// parte) y salida de la columna (el resto); luego barrido con la columna completa y por
    /// último la retracción, que repite la salida al revés. Servidor y clientes la evalúan
    /// con el mismo tiempo para ver lo mismo.
    /// </summary>
    public sealed class LineaTiempoManotazo
    {
        /// <summary>Semiángulo del barrido: va de -90° a +90° (por el sentido).</summary>
        public const float AnguloMaximo = 90f;

        private readonly float _finBurbuja;
        private readonly float _finAviso;
        private readonly float _finBarrido;
        private readonly float _finRetraccion;

        public LineaTiempoManotazo(
            float aviso,
            float fraccionBurbuja,
            float barrido,
            float retraccion
        )
        {
            _finAviso = Mathf.Max(0f, aviso);
            _finBurbuja = _finAviso * Mathf.Clamp01(fraccionBurbuja);
            _finBarrido = _finAviso + Mathf.Max(0f, barrido);
            _finRetraccion = _finBarrido + Mathf.Max(0f, retraccion);
        }

        /// <summary>Duración total: aviso + barrido + retracción.</summary>
        public float Duracion => _finRetraccion;

        /// <param name="tiempo">Segundos desde que empezó el ataque.</param>
        /// <param name="sentido">+1 barre de -90° a +90°; -1 al revés.</param>
        public EstadoManotazo Evaluar(float tiempo, int sentido)
        {
            float signo = sentido < 0 ? -1f : 1f;
            float t = Mathf.Max(0f, tiempo);

            if (t < _finBurbuja)
            {
                float intensidad = Suavizar(Progreso(t, 0f, _finBurbuja));
                return new EstadoManotazo(
                    FaseManotazo.Burbuja,
                    0f,
                    -AnguloMaximo * signo,
                    intensidad
                );
            }

            if (t < _finAviso)
            {
                float p = Progreso(t, _finBurbuja, _finAviso);
                return new EstadoManotazo(
                    FaseManotazo.Salida,
                    Salida(p),
                    -AnguloMaximo * signo,
                    1f
                );
            }

            if (t < _finBarrido)
            {
                float p = Progreso(t, _finAviso, _finBarrido);
                float angulo = Mathf.Lerp(-AnguloMaximo, AnguloMaximo, p) * signo;
                return new EstadoManotazo(FaseManotazo.Barrido, 1f, angulo, 1f);
            }

            if (t < _finRetraccion)
            {
                float p = Progreso(t, _finBarrido, _finRetraccion);
                return new EstadoManotazo(
                    FaseManotazo.Retraccion,
                    Salida(1f - p),
                    AnguloMaximo * signo,
                    1f - p
                );
            }

            return new EstadoManotazo(FaseManotazo.Terminado, 0f, AnguloMaximo * signo, 0f);
        }

        // Sale rápido y se frena al llegar: 1 - (1 - p)².
        private static float Salida(float p)
        {
            float resto = 1f - p;
            return 1f - resto * resto;
        }

        private static float Suavizar(float p) => p * p * (3f - 2f * p);

        private static float Progreso(float tiempo, float inicio, float fin)
        {
            float duracion = fin - inicio;
            return duracion > 0f ? Mathf.Clamp01((tiempo - inicio) / duracion) : 1f;
        }
    }
}
