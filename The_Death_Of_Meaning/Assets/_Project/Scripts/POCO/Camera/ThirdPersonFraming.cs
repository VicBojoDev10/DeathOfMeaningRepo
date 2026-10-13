using UnityEngine;

namespace TDOM.Gameplay.Camera
{
    public sealed class ThirdPersonFraming
    {
        private const float MezclaLibre = 0f;
        private const float MezclaApuntando = 1f;

        private readonly float _distanciaLibre;
        private readonly float _distanciaApuntando;
        private readonly float _hombroLibre;
        private readonly float _hombroApuntando;
        private readonly float _velocidad;

        private float _mezcla;

        public float Distance => Mathf.Lerp(_distanciaLibre, _distanciaApuntando, _mezcla);
        public float ShoulderOffset => Mathf.Lerp(_hombroLibre, _hombroApuntando, _mezcla);

        public ThirdPersonFraming(
            float distanciaLibre,
            float distanciaApuntando,
            float hombroLibre,
            float hombroApuntando,
            float velocidad
        )
        {
            _distanciaLibre = distanciaLibre;
            _distanciaApuntando = distanciaApuntando;
            _hombroLibre = hombroLibre;
            _hombroApuntando = hombroApuntando;
            _velocidad = velocidad;
        }

        public void Tick(bool apuntando, float dt)
        {
            float objetivo = apuntando ? MezclaApuntando : MezclaLibre;
            float factor = 1f - Mathf.Exp(-_velocidad * dt);
            _mezcla = Mathf.Lerp(_mezcla, objetivo, factor);
        }
    }
}
