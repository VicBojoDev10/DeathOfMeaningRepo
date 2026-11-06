using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public sealed class VfxCargaSeguida : VfxCarga
    {
        private Transform _objetivo;

        public void Seguir(Transform objetivo) => _objetivo = objetivo;

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
            if (_objetivo == null)
                return;

            Visual.SetPositionAndRotation(
                _objetivo.position + _objetivo.rotation * Perfil.puntoA,
                _objetivo.rotation * Orientacion
            );
        }
    }
}
