using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public sealed class VfxDisparoSostenido : VfxDisparo
    {
        private const float GiroEslabonAlterno = 90f;
        private const int PuntosDelCable = 2;

        [SerializeField, Min(0f)]
        private float _duracionMaxima = 10f;

        [SerializeField]
        private ParticleSystem _cadena;

        [SerializeField, Min(0.01f)]
        private float _separacionEslabones = 0.2f;

        [SerializeField, Min(0f)]
        private float _tamanoEslabon = 0.3f;

        [SerializeField, Min(0.01f)]
        private float _vidaEslabon = 0.15f;

        private Transform _origen;
        private Transform _destino;
        private LineRenderer _cable;
        private Vector3 _desplazamientoLocal;
        private Quaternion _rotacionLocal = Quaternion.identity;
        private ParticleSystem.Particle[] _eslabones = new ParticleSystem.Particle[0];

        public void Seguir(Transform origen) => _origen = origen;

        public void Apuntar(Transform destino)
        {
            _destino = destino;
            _cable = destino != null ? destino.GetComponent<LineRenderer>() : null;
        }

        protected override void Awake()
        {
            base.Awake();
            if (_cadena != null)
                _eslabones = new ParticleSystem.Particle[_cadena.main.maxParticles];
        }

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
            if (!Activo)
                return;

            ColocarEnOrigen();
            ActualizarCadena();
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

        private void ActualizarCadena()
        {
            if (
                _cadena == null
                || _cable == null
                || _cable.positionCount < PuntosDelCable
                || !_cadena.isEmitting
            )
                return;

            Vector3 inicio = _cable.GetPosition(0);
            Vector3 tramo = _cable.GetPosition(1) - inicio;
            float distancia = tramo.magnitude;
            if (distancia < _separacionEslabones)
                return;

            Quaternion mirada = Quaternion.LookRotation(tramo, Vector3.up);
            int cantidad = Mathf.Min(
                Mathf.FloorToInt(distancia / _separacionEslabones) + 1,
                _eslabones.Length
            );

            for (int i = 0; i < cantidad; i++)
            {
                float giro = i % 2 * GiroEslabonAlterno;
                _eslabones[i] = new ParticleSystem.Particle
                {
                    position = inicio + tramo / distancia * (i * _separacionEslabones),
                    rotation3D = (mirada * Quaternion.Euler(0f, 0f, giro)).eulerAngles,
                    startSize = _tamanoEslabon,
                    startColor = Color.white,
                    startLifetime = _vidaEslabon,
                    remainingLifetime = _vidaEslabon,
                };
            }

            _cadena.SetParticles(_eslabones, cantidad);
        }
    }
}
