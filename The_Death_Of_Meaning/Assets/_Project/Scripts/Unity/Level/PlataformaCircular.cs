using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    [DisallowMultipleComponent]
    public sealed class PlataformaCircular : MonoBehaviour
    {
        [SerializeField] private Vector3 center;
        [SerializeField, Min(0f)] private float radius = 2f;
        [Tooltip("Velocidad angular en grados por segundo.")]
        [SerializeField] private float angularVelocity = 30f;
        [Tooltip("1 = antihorario; -1 = horario.")]
        [SerializeField] private float sense = 1f;
        [Tooltip("Fase inicial en grados.")]
        [SerializeField] private float initialPhase;

        private Vector3 _lastPosition;
        private Vector3 _frameDelta;
        private Vector3 _startPosition;

        public Vector3 FrameDelta => _frameDelta;

        private void OnEnable()
        {
            _startPosition = transform.position;
            _lastPosition = _startPosition;
            _frameDelta = Vector3.zero;
        }

        private void Update()
        {
            double networkTime = NetworkManager.Singleton != null
                ? NetworkManager.Singleton.ServerTime.Time
                : 0d;
            Vector3 position = CalcularPosicion(networkTime);
            transform.position = position;
            _frameDelta = position - _lastPosition;
            _lastPosition = position;
        }

        public Vector3 CalcularPosicion(double networkTime)
        {
            double phase = initialPhase * Mathf.Deg2Rad;
            double angle = phase + angularVelocity * sense * networkTime * Mathf.Deg2Rad;
            Vector3 centerWorld = _startPosition + center;
            Vector3 startOffset = new Vector3(
                (float)System.Math.Cos(phase) * radius,
                0f,
                (float)System.Math.Sin(phase) * radius
            );
            Vector3 currentOffset = new Vector3(
                (float)System.Math.Cos(angle) * radius,
                0f,
                (float)System.Math.Sin(angle) * radius
            );
            return centerWorld + currentOffset - startOffset;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 startPosition = Application.isPlaying ? _startPosition : transform.position;
            Gizmos.DrawWireSphere(startPosition + center, radius);
        }
    }
}
