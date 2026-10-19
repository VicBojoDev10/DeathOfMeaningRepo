using System;
using System.Collections;

namespace TDOM.Unity.Flow
{
    public sealed class FadeCinematicExit : ICinematicExit
    {
        private const float AlphaTransparente = 0f;
        private const float AlphaNegro = 1f;

        private readonly Func<float, IEnumerator> _fundirA;
        private readonly Action _detenerDirector;

        public FadeCinematicExit(Func<float, IEnumerator> fundirA, Action detenerDirector)
        {
            _fundirA = fundirA;
            _detenerDirector = detenerDirector;
        }

        public void Preparar() { }

        public IEnumerator Salir()
        {
            yield return _fundirA(AlphaNegro);
            _detenerDirector();
            yield return _fundirA(AlphaTransparente);
        }
    }
}
