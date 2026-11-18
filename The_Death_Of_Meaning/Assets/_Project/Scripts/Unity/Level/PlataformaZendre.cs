using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Level
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10)]
    public sealed class PlataformaZendre : PlataformaMovil
    {
        [Header("Recorrido sobre eje X")]
        [SerializeField] private float _limiteXMinimo = -45f;
        [SerializeField] private float _limiteXMaximo = 5f;
        [SerializeField, Min(0.01f)] private float _velocidad = 2f;

        private NetworkManager _networkManager;
        private float _posicionY;
        private float _posicionZ;
        private Vector3 _ultimaPosicion;
        private Vector3 _frameDelta;

        public override Vector3 FrameDelta => _frameDelta;

        private void Awake()
        {
            _networkManager = FindAnyObjectByType<NetworkManager>();
            _posicionY = transform.localPosition.y;
            _posicionZ = transform.localPosition.z;
            _ultimaPosicion = transform.localPosition;
        }

        private void OnEnable()
        {
            _posicionY = transform.localPosition.y;
            _posicionZ = transform.localPosition.z;
            Vector3 posicionInicial = CalcularPosicion(ObtenerTiempo());
            transform.localPosition = posicionInicial;
            _ultimaPosicion = posicionInicial;
            _frameDelta = Vector3.zero;
        }

        private void OnDisable()
        {
            _frameDelta = Vector3.zero;
        }

        private void Update()
        {
            Vector3 posicionActual = CalcularPosicion(ObtenerTiempo());
            _frameDelta = posicionActual - _ultimaPosicion;
            transform.localPosition = posicionActual;
            _ultimaPosicion = posicionActual;
        }

        private Vector3 CalcularPosicion(double tiempo)
        {
            float minimo = _limiteXMinimo;
            float maximo = Mathf.Max(_limiteXMaximo, minimo + 0.01f);
            float rango = maximo - minimo;
            float desplazamiento = 0f;

            if (_velocidad > 0f)
            {
                double distancia = System.Math.Max(0d, tiempo) * _velocidad;
                float fase = (float)(distancia % (rango * 2f));
                desplazamiento = fase <= rango ? fase : (rango * 2f) - fase;
            }

            return new Vector3(minimo + desplazamiento, _posicionY, _posicionZ);
        }

        private double ObtenerTiempo()
        {
            if (_networkManager == null)
                _networkManager = FindAnyObjectByType<NetworkManager>();

            if (_networkManager != null && _networkManager.IsListening)
                return _networkManager.ServerTime.Time;

            return Time.timeAsDouble;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_limiteXMaximo <= _limiteXMinimo)
                _limiteXMaximo = _limiteXMinimo + 0.01f;
            _velocidad = Mathf.Max(0.01f, _velocidad);
        }
#endif
    }
}