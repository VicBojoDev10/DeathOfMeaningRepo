using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    [DisallowMultipleComponent]
    public sealed class PlataformaCircular : MonoBehaviour
    {
        [Tooltip("Transform que marca el centro de la órbita.")]
        [SerializeField] private Transform centerArena;
        [Tooltip("Distancia horizontal de la plataforma al centro de la órbita.")]
        [SerializeField, Min(0f)] private float radius = 2f;
        [Tooltip("Velocidad angular en grados por segundo.")]
        [SerializeField] private float angularVelocity = 30f;
        [Tooltip("1 = antihorario; -1 = horario.")]
        [SerializeField] private float sense = 1f;

        private Vector3 _lastPosition;
        private Vector3 _frameDelta;
        private Vector3 _startPosition;
        private double _startNetworkTime;

        public Vector3 FrameDelta => _frameDelta;

        private void OnEnable()
        {
            _startPosition = transform.position;
            _lastPosition = _startPosition;
            _frameDelta = Vector3.zero;
            _startNetworkTime = ObtenerTiempoRed();

            if (centerArena == null)
            {
                Debug.LogError($"{nameof(PlataformaCircular)} en '{name}' necesita una referencia a CenterArena.", this);
                enabled = false;
                return;
            }

            Vector3 initialOffset = _startPosition - ObtenerCentroMundial();
            initialOffset.y = 0f;
            float initialRadius = initialOffset.magnitude;
            if (!Mathf.Approximately(initialRadius, radius))
            {
                Debug.LogWarning(
                    $"La plataforma '{name}' empieza a {initialRadius:F2} unidades del centro, " +
                    $"pero su radio configurado es {radius:F2}. Para conservar la posición inicial y girar " +
                    "alrededor de CenterArena, ambos valores deben coincidir.", this);
            }
        }

        private void Update()
        {
            double elapsedTime = ObtenerTiempoRed() - _startNetworkTime;
            Vector3 position = CalcularPosicion(elapsedTime);
            transform.position = position;
            _frameDelta = position - _lastPosition;
            _lastPosition = position;
        }

        private double ObtenerTiempoRed()
        {
            return NetworkManager.Singleton != null
                ? NetworkManager.Singleton.ServerTime.Time
                : Time.timeAsDouble;
        }

        public Vector3 CalcularPosicion(double elapsedTime)
        {
            Vector3 centerWorld = ObtenerCentroMundial();
            Vector3 startOffset = _startPosition - centerWorld;
            startOffset.y = 0f;
            if (startOffset.sqrMagnitude < Mathf.Epsilon)
                startOffset = Vector3.forward;
            else
                startOffset.Normalize();

            float angle = (float)(angularVelocity * sense * elapsedTime);
            Vector3 orbitOffset = Quaternion.Euler(0f, angle, 0f) * (startOffset * radius);
            return new Vector3(centerWorld.x + orbitOffset.x, _startPosition.y, centerWorld.z + orbitOffset.z);
        }

        private Vector3 ObtenerCentroMundial()
        {
            return centerArena.position;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 position = Application.isPlaying ? _startPosition : transform.position;
            Vector3 centerWorld = centerArena != null
                ? ObtenerCentroMundial()
                : transform.position;
            Gizmos.DrawWireSphere(new Vector3(centerWorld.x, position.y, centerWorld.z), radius);
        }
    }
}
