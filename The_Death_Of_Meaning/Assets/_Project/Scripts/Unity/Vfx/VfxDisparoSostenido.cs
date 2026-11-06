using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public sealed class VfxDisparoSostenido : VfxDisparo
    {
        [SerializeField, Min(0f)]
        private float _duracionMaxima = 10f;

        private Transform _origen;
        private Transform _destino;
        private Vector3 _desplazamientoLocal;
        private Quaternion _rotacionLocal = Quaternion.identity;

        public void Seguir(Transform origen) => _origen = origen;

        public void Apuntar(Transform destino) => _destino = destino;

        protected override bool TiempoAgotado(float tiempo) => tiempo >= _duracionMaxima;

        protected override void Avanzar(float t)
        {
            base.Avanzar(t);
            Quaternion inversa = Quaternion.Inverse(RotacionBase);
            _desplazamientoLocal = inversa * (Visual.position - PosicionBase);
            _rotacionLocal = inversa * Visual.rotation;
            ColocarEnOrigen();
        }

        private void LateUpdate()
        {
            if (Activo)
                ColocarEnOrigen();
        }

        private void ColocarEnOrigen()
        {
            if (_origen == null)
                return;

            Quaternion mirada = MiradaHaciaDestino();
            Visual.SetPositionAndRotation(
                _origen.position + mirada * _desplazamientoLocal,
                mirada * _rotacionLocal
            );
        }

        private Quaternion MiradaHaciaDestino()
        {
            if (_destino == null)
                return RotacionBase;

            Vector3 direccion = _destino.position - _origen.position;
            return direccion.sqrMagnitude > 0f
                ? Quaternion.LookRotation(direccion, Vector3.up)
                : RotacionBase;
        }
    }
}
