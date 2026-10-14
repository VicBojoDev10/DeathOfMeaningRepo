using System.Collections;
using TDOM.Data;
using TDOM.Gameplay.Camera;
using Unity.Cinemachine;
using UnityEngine;

namespace TDOM.Unity.Camera
{
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        private const int CapacidadImpactosApuntado = 16;

        [SerializeField]
        private CinemachineCamera _camara;

        [SerializeField]
        private CinemachineThirdPersonFollow _seguimiento;

        [SerializeField]
        private CinemachineImpulseSource _impulso;

        [SerializeField]
        private ThirdPersonCameraConfig _config;

        [SerializeField]
        private int _prioridadCedida = -10;

        private readonly RaycastHit[] _impactosApuntado = new RaycastHit[CapacidadImpactosApuntado];

        private CameraStanceResolver _stances;
        private Transform _cuerpo;
        private float _fovExtra;
        private float _dutchBase;
        private float _dutchExtra;
        private int _prioridadBase;
        private Coroutine _rutinaFovActual;
        private Coroutine _rutinaTiltActual;

        public ThirdPersonCameraConfig Config => _config;
        public Transform CamaraTransform => _camara != null ? _camara.transform : transform;
        public CameraStance Stance => _stances.Stance;

        private void Awake()
        {
            _cuerpo = transform.root;
            _stances = new CameraStanceResolver(CrearAjustes(_config));
            LeerValoresBaseDeCamara();
            AplicarEncuadre();
        }

        private void LeerValoresBaseDeCamara()
        {
            if (_camara == null)
                return;

            _dutchBase = _camara.Lens.Dutch;
            _prioridadBase = _camara.Priority.Value;
        }

        public void CederPrioridad(bool cedida)
        {
            if (_camara != null)
                _camara.Priority.Value = cedida ? _prioridadCedida : _prioridadBase;
        }

        public void ApplyLook(float yaw, float pitch)
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        public void ActualizarEncuadre(bool apuntando, bool atacando, bool corriendo, float dt)
        {
            _stances.Tick(apuntando, atacando, corriendo, dt);
            AplicarEncuadre();
        }

        public void RegistrarDesplazamientoLateral(Vector2 movimientoRelativoACamara)
        {
            _stances.RegistrarDesplazamientoLateral(movimientoRelativoACamara);
        }

        private void AplicarEncuadre()
        {
            StanceFraming encuadre = _stances.Framing;
            transform.localPosition = Vector3.up * encuadre.PivotHeight;
            AplicarLente();

            if (_seguimiento == null)
                return;

            _seguimiento.CameraDistance = encuadre.Distance;
            _seguimiento.CameraSide = _stances.CameraSide;
            Vector3 hombro = _seguimiento.ShoulderOffset;
            hombro.x = encuadre.ShoulderOffset;
            _seguimiento.ShoulderOffset = hombro;
        }

        private static CameraStanceSettings CrearAjustes(ThirdPersonCameraConfig config)
        {
            return new CameraStanceSettings(
                CrearEncuadre(config.Exploration),
                CrearEncuadre(config.Sprint),
                CrearEncuadre(config.Combat),
                CrearEncuadre(config.Aim),
                config.TransitionSpeed,
                config.CombatLingerSeconds,
                config.ShoulderSwapSpeed,
                config.StrafeSwingThreshold
            );
        }

        private static StanceFraming CrearEncuadre(CameraStanceValues valores)
        {
            return new StanceFraming(
                valores.Distance,
                valores.ShoulderOffsetX,
                valores.PivotHeight,
                valores.FieldOfView
            );
        }

        public AimSolution ResolverApuntado(Vector3 origen, out Collider colliderImpactado)
        {
            Transform camara = CamaraTransform;
            var rayo = new Ray(camara.position, camara.forward);
            colliderImpactado = null;

            if (!TryImpactoMasCercano(rayo, out RaycastHit impacto))
                return AimResolver.ResolveMiss(rayo, _config.AimMaxRange, origen);

            colliderImpactado = impacto.collider;
            return AimResolver.ResolveHit(rayo, impacto.point, origen);
        }

        private bool TryImpactoMasCercano(Ray rayo, out RaycastHit masCercano)
        {
            int cantidad = Physics.RaycastNonAlloc(
                rayo,
                _impactosApuntado,
                _config.AimMaxRange,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );

            masCercano = default;
            bool encontrado = false;
            for (int i = 0; i < cantidad; i++)
            {
                RaycastHit impacto = _impactosApuntado[i];
                if (impacto.collider.transform.IsChildOf(_cuerpo))
                    continue;
                if (encontrado && impacto.distance >= masCercano.distance)
                    continue;

                masCercano = impacto;
                encontrado = true;
            }

            return encontrado;
        }

        public void PunchFov(float grados, float duracion)
        {
            if (_rutinaFovActual != null)
                StopCoroutine(_rutinaFovActual);

            _rutinaFovActual = StartCoroutine(RutinaFov(grados, duracion));
        }

        public void Tilt(float grados, float duracion)
        {
            if (_rutinaTiltActual != null)
                StopCoroutine(_rutinaTiltActual);

            _rutinaTiltActual = StartCoroutine(RutinaTilt(grados, duracion));
        }

        public void Impacto(float fuerza) => _impulso.GenerateImpulse(fuerza);

        private IEnumerator RutinaFov(float grados, float duracion)
        {
            float tiempoTranscurrido = 0f;
            _fovExtra = grados;
            AplicarLente();

            while (tiempoTranscurrido < duracion)
            {
                tiempoTranscurrido += Time.deltaTime;
                float t = Mathf.Clamp01(tiempoTranscurrido / duracion);
                _fovExtra = Mathf.Lerp(grados, 0f, t);
                AplicarLente();
                yield return null;
            }

            _fovExtra = 0f;
            AplicarLente();
            _rutinaFovActual = null;
        }

        private void AplicarLente()
        {
            if (_camara == null)
                return;

            var lente = _camara.Lens;
            lente.FieldOfView = _stances.Framing.FieldOfView + _fovExtra;
            lente.Dutch = _dutchBase + _dutchExtra;
            _camara.Lens = lente;
        }

        private IEnumerator RutinaTilt(float grados, float duracion)
        {
            float tiempoTranscurrido = 0f;
            _dutchExtra = grados;
            AplicarLente();

            while (tiempoTranscurrido < duracion)
            {
                tiempoTranscurrido += Time.deltaTime;
                float t = Mathf.Clamp01(tiempoTranscurrido / duracion);
                _dutchExtra = Mathf.Lerp(grados, 0f, t);
                AplicarLente();
                yield return null;
            }

            _dutchExtra = 0f;
            AplicarLente();
            _rutinaTiltActual = null;
        }

        public void PunchFovSostenido(
            float grados,
            float duracionSostenido,
            float duracionSubida = 0.1f,
            float duracionBajada = 0.15f
        )
        {
            if (_rutinaFovActual != null)
                StopCoroutine(_rutinaFovActual);

            _rutinaFovActual = StartCoroutine(
                RutinaFovSostenido(grados, duracionSostenido, duracionSubida, duracionBajada)
            );
        }

        private IEnumerator RutinaFovSostenido(
            float grados,
            float duracionSostenido,
            float duracionSubida,
            float duracionBajada
        )
        {
            float t = 0f;
            while (t < duracionSubida)
            {
                t += Time.deltaTime;
                _fovExtra = Mathf.Lerp(0f, grados, Mathf.Clamp01(t / duracionSubida));
                AplicarLente();
                yield return null;
            }
            _fovExtra = grados;
            AplicarLente();

            yield return new WaitForSeconds(duracionSostenido);

            t = 0f;
            while (t < duracionBajada)
            {
                t += Time.deltaTime;
                _fovExtra = Mathf.Lerp(grados, 0f, Mathf.Clamp01(t / duracionBajada));
                AplicarLente();
                yield return null;
            }

            _fovExtra = 0f;
            AplicarLente();
            _rutinaFovActual = null;
        }
    }
}
