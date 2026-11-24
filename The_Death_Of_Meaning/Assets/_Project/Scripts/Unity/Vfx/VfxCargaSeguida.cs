using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public sealed class VfxCargaSeguida : VfxCarga
    {
        private Transform _referencia;
        private Transform[] _puntos = new Transform[0];

        public void Seguir(Transform referencia, params Transform[] puntos)
        {
            _referencia = referencia;
            _puntos = puntos ?? new Transform[0];
        }

        protected override void Avanzar(float t)
        {
            base.Avanzar(t);
            ColocarEnObjetivo();
        }

        private void LateUpdate()
        {
            if (Activo)
                ColocarEnObjetivo();
        }

        private void ColocarEnObjetivo()
        {
            if (_referencia == null)
                return;

            Visual.SetPositionAndRotation(
                CentroDeLosPuntos() + _referencia.rotation * Perfil.puntoA,
                _referencia.rotation * Orientacion
            );
        }

        private Vector3 CentroDeLosPuntos()
        {
            Vector3 suma = Vector3.zero;
            int cantidad = 0;
            foreach (Transform punto in _puntos)
            {
                if (punto == null)
                    continue;

                suma += punto.position;
                cantidad++;
            }

            return cantidad > 0 ? suma / cantidad : _referencia.position;
        }
    }
}
