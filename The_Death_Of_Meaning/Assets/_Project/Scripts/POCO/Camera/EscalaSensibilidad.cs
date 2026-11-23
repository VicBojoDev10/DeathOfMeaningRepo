using UnityEngine;

namespace TDOM.Gameplay.Camera
{
    public readonly struct EscalaSensibilidad
    {
        private readonly float _nivelMinimo;
        private readonly float _nivelMaximo;
        private readonly float _pasoNivel;
        private readonly float _gradosPorNivel;

        public EscalaSensibilidad(
            float nivelMinimo,
            float nivelMaximo,
            float pasoNivel,
            float gradosPorNivel
        )
        {
            _pasoNivel = Mathf.Max(Mathf.Epsilon, pasoNivel);
            _nivelMinimo = Mathf.Max(_pasoNivel, nivelMinimo);
            _nivelMaximo = Mathf.Max(_nivelMinimo, nivelMaximo);
            _gradosPorNivel = Mathf.Max(0f, gradosPorNivel);
        }

        public int PasoMinimo => Mathf.RoundToInt(_nivelMinimo / _pasoNivel);
        public int PasoMaximo => Mathf.RoundToInt(_nivelMaximo / _pasoNivel);

        public float NivelDesdePaso(int paso)
        {
            return Mathf.Clamp(paso, PasoMinimo, PasoMaximo) * _pasoNivel;
        }

        public float GradosDesdePaso(int paso)
        {
            return NivelDesdePaso(paso) * _gradosPorNivel;
        }

        public int PasoDesdeGrados(float grados)
        {
            if (_gradosPorNivel <= 0f)
                return PasoMinimo;

            int paso = Mathf.RoundToInt(grados / _gradosPorNivel / _pasoNivel);
            return Mathf.Clamp(paso, PasoMinimo, PasoMaximo);
        }
    }
}
