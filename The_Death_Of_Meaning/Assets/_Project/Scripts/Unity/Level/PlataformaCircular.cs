using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Level
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10)]
    public sealed class PlataformaCircular : MonoBehaviour
    {
        public enum ModoRotacionPlataforma
        {
            Fija,
            MirarAlCentro,
            TangenteAlMovimiento
        }

        [Tooltip("Transform que marca el centro de la órbita (por ejemplo, el centro de la arena del boss).")]
        [SerializeField] private Transform centerArena;

        [Tooltip("Distancia horizontal de la plataforma al centro de la órbita en metros.")]
        [SerializeField, Min(0f)] private float radius = 2.58f;

        [Tooltip("Velocidad angular en grados por segundo.")]
        [SerializeField] private float angularVelocity = 25f;

        [Tooltip("1 = Sentido antihorario (+), -1 = Sentido horario (-).")]
        [SerializeField] private float sense = 1f;

        [Tooltip("Fase inicial en grados (0 a 360).")]
        [SerializeField] private float initialPhase = 0f;

        [Tooltip("Modo de orientación de la plataforma mientras orbita.")]
        [SerializeField] private ModoRotacionPlataforma rotationMode = ModoRotacionPlataforma.Fija;

        [Tooltip("Si está activo, preserva la altura Y con la que se posicionó en la escena respecto al centro.")]
        [SerializeField] private bool usarAlturaInicial = true;

        [Tooltip("Desfase de altura vertical (Y) respecto al centro. Se autocalcula si usarAlturaInicial está activo.")]
        [SerializeField] private float heightOffset = 0f;

        private Vector3 _lastPosition;
        private Vector3 _frameDelta;

        public Vector3 FrameDelta => _frameDelta;
        public float Radius => radius;
        public float AngularVelocity => angularVelocity;
        public float Sense => sense >= 0f ? 1f : -1f;
        public float InitialPhase => initialPhase;
        public Transform CenterArena => centerArena;
        public float HeightOffset => heightOffset;

        private void Awake()
        {
            InicializarAltura();
        }

        private void OnEnable()
        {
            InicializarAltura();

            double time = ObtenerTiempoRed();
            Vector3 startPos = CalcularPosicion(time);
            transform.position = startPos;
            _lastPosition = startPos;
            _frameDelta = Vector3.zero;

            ActualizarRotacion(time, startPos);
        }

        private void OnDisable()
        {
            _frameDelta = Vector3.zero;
        }

        private void InicializarAltura()
        {
            if (usarAlturaInicial)
            {
                heightOffset = centerArena != null
                    ? transform.position.y - centerArena.position.y
                    : transform.position.y;
            }
        }

        private void Update()
        {
            double time = ObtenerTiempoRed();
            Vector3 currentPos = CalcularPosicion(time);

            _frameDelta = currentPos - _lastPosition;
            transform.position = currentPos;
            _lastPosition = currentPos;

            ActualizarRotacion(time, currentPos);
        }

        private void ActualizarRotacion(double networkTime, Vector3 currentPos)
        {
            if (rotationMode == ModoRotacionPlataforma.Fija)
                return;

            Vector3 centerWorld = centerArena != null ? centerArena.position : Vector3.zero;
            if (rotationMode == ModoRotacionPlataforma.MirarAlCentro)
            {
                Vector3 toCenter = centerWorld - currentPos;
                toCenter.y = 0f;
                if (toCenter.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(toCenter, Vector3.up);
            }
            else if (rotationMode == ModoRotacionPlataforma.TangenteAlMovimiento)
            {
                float dir = Sense;
                float angleDeg = initialPhase + (float)(dir * angularVelocity * networkTime);
                float angleRad = angleDeg * Mathf.Deg2Rad;
                Vector3 tangent = new Vector3(-Mathf.Sin(angleRad) * dir, 0f, Mathf.Cos(angleRad) * dir);
                if (tangent.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
            }
        }

        public static double ObtenerTiempoRed()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                return NetworkManager.Singleton.ServerTime.Time;
            }

            return Time.timeAsDouble;
        }

        public Vector3 CalcularPosicion(double networkTime)
        {
            Vector3 centerWorld = centerArena != null ? centerArena.position : Vector3.zero;
            float dir = Sense;

            // Determinismo puro: Fase inicial + velocidad angular * sentido * tiempo absoluto
            float angleDeg = initialPhase + (float)(dir * angularVelocity * networkTime);
            float angleRad = angleDeg * Mathf.Deg2Rad;

            float x = centerWorld.x + Mathf.Cos(angleRad) * radius;
            float z = centerWorld.z + Mathf.Sin(angleRad) * radius;
            float y = centerWorld.y + heightOffset;

            return new Vector3(x, y, z);
        }

        public void Configurar(Transform center, float newRadius, float newAngularVelocity, float newSense, float newInitialPhase)
        {
            centerArena = center;
            radius = Mathf.Max(0f, newRadius);
            angularVelocity = newAngularVelocity;
            sense = newSense;
            initialPhase = newInitialPhase;
            InicializarAltura();
        }

#if UNITY_EDITOR
        [ContextMenu("Alinear a Posición de Fase Inicial")]
        public void AlinearAFaseInicial()
        {
            InicializarAltura();
            Vector3 initialPos = CalcularPosicion(0d);
            transform.position = initialPos;
            _lastPosition = initialPos;
            _frameDelta = Vector3.zero;
            ActualizarRotacion(0d, initialPos);
        }

        [ContextMenu("Calcular Fase y Radio desde Posición Actual")]
        public void CalcularFaseYRadioDesdePosicion()
        {
            Vector3 centerWorld = centerArena != null ? centerArena.position : Vector3.zero;
            Vector3 offset = transform.position - centerWorld;
            offset.y = 0f;

            radius = offset.magnitude;
            if (radius > 0.001f)
            {
                initialPhase = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
                if (initialPhase < 0f)
                    initialPhase += 360f;
            }
            InicializarAltura();
        }
#endif

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 centerWorld = centerArena != null ? centerArena.position : Vector3.zero;
            float yPos = Application.isPlaying ? transform.position.y : (centerWorld.y + heightOffset);
            Vector3 gizmoCenter = new Vector3(centerWorld.x, yPos, centerWorld.z);

            Gizmos.DrawWireSphere(gizmoCenter, radius);

            // Dibuja radio hacia la fase inicial
            float initialRad = initialPhase * Mathf.Deg2Rad;
            Vector3 initialPos = gizmoCenter + new Vector3(Mathf.Cos(initialRad) * radius, 0f, Mathf.Sin(initialRad) * radius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(gizmoCenter, initialPos);
            Gizmos.DrawSphere(initialPos, 0.25f);
        }
    }
}
