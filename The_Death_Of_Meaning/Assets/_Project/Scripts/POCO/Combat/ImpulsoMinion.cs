using System;

namespace TDOM.Gameplay.Combat
{
    public sealed class ImpulsoMinion
    {
        private readonly float _altura;
        private readonly float _duracionVuelo;
        private readonly float _duracionAturdimiento;
        private float _tiempo;

        public ImpulsoMinion(float altura, float gravedad, float aturdimiento)
        {
            if (
                !EsPositivoFinito(altura)
                || !EsPositivoFinito(gravedad)
                || !EsPositivoFinito(aturdimiento)
            )
                throw new ArgumentOutOfRangeException(nameof(altura));

            _altura = altura;
            _duracionVuelo = (float)Math.Sqrt(8f * altura / gravedad);
            _duracionAturdimiento = Math.Max(aturdimiento, _duracionVuelo);
        }

        public float Progreso => Math.Min(1f, _tiempo / _duracionVuelo);
        public float Altura => 4f * _altura * Progreso * (1f - Progreso);
        public bool EnVuelo => _tiempo < _duracionVuelo;
        public bool Aturdido => _tiempo < _duracionAturdimiento;

        public void Tick(float dt)
        {
            if (EsPositivoFinito(dt))
                _tiempo = Math.Min(_duracionAturdimiento, _tiempo + dt);
        }

        private static bool EsPositivoFinito(float valor) =>
            valor > 0f && !float.IsInfinity(valor) && !float.IsNaN(valor);
    }
}
