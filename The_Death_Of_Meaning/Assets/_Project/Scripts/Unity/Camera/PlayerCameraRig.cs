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
        private CinemachineImpulseSource _impulsoAterrizaje;

        [SerializeField]
        private ThirdPersonCameraConfig _config;

        [SerializeField]
        private int _prioridadCedida = -10;

        private readonly RaycastHit[] _impactosApuntado = new RaycastHit[CapacidadImpactosApuntado];

        private CameraStanceResolver _stances;
        private Transform _cuerpo;
        private float _fovExtra;
        private float _pulsoNivel;
        private float _dashNivel;
        private float _dashZoomObjetivo;
        private float _dashZoomInicio;
        private float _dashZoomTiempo;
        private float _dashZoomDuracion = 0.1f;
        private float _dashZoomAmount;
        private AnimationCurve _dashZoomCurva;
        private Coroutine _rutinaPulso;
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
            CrearFuenteImpulsoAterrizaje();
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
            // Rotacion absoluta en mundo: el yaw/pitch del jugador no se suma al de la camara.
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        public void SetDashFeedback(
            bool activo,
            float amount,
            float entrada,
            float salida,
            AnimationCurve curva,
            float multiplicador
        )
        {
            float objetivo = activo ? 1f : 0f;
            if (!Mathf.Approximately(objetivo, _dashZoomObjetivo))
            {
                _dashZoomInicio = _dashNivel;
                _dashZoomObjetivo = objetivo;
                _dashZoomTiempo = 0f;
                _dashZoomDuracion = Mathf.Max(0.01f, activo ? entrada : salida);
            }
            _dashZoomAmount = Mathf.Max(0f, amount) * Mathf.Clamp01(multiplicador);
            _dashZoomCurva = curva;
        }

        private void ActualizarDashCerca(float dt)
        {
            if (_config == null)
                return;

            if (Mathf.Approximately(_dashNivel, _dashZoomObjetivo))
                return;
            _dashZoomTiempo += dt;
            float t = Mathf.Clamp01(_dashZoomTiempo / _dashZoomDuracion);
            float eased = _dashZoomCurva != null ? Mathf.Clamp01(_dashZoomCurva.Evaluate(t)) : t;
            _dashNivel = Mathf.Lerp(_dashZoomInicio, _dashZoomObjetivo, eased);
        }

        public void ActualizarEncuadre(bool apuntando, bool atacando, bool corriendo, float dt)
        {
            ActualizarDashCerca(dt);
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

            float acercamientoDash = _dashNivel * _dashZoomAmount;
            _seguimiento.CameraDistance =
                encuadre.Distance + (_pulsoNivel * DistanciaPulso()) - acercamientoDash;
            _seguimiento.CameraSide = _stances.CameraSide;
            Vector3 hombro = _seguimiento.ShoulderOffset;
            hombro.x = encuadre.ShoulderOffset;
            _seguimiento.ShoulderOffset = hombro;
        }

        private static CameraStanceSettings CrearAjustes(ThirdPersonCameraConfig config)
        {
            CameraStanceValues combate = config.Combat;
            if (!config.CombatZoomSostenido)
            {
                combate = new CameraStanceValues(
                    config.Exploration.Distance,
                    config.Combat.ShoulderOffsetX,
                    config.Combat.PivotHeight,
                    config.Exploration.FieldOfView
                );
            }

            return new CameraStanceSettings(
                CrearEncuadre(config.Exploration),
                CrearEncuadre(config.Sprint),
                CrearEncuadre(combate),
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

        public void ImpactoAterrizaje(float fuerza, float duracion)
        {
            if (_impulsoAterrizaje == null || _impulsoAterrizaje.ImpulseDefinition == null)
                return;

            _impulsoAterrizaje.ImpulseDefinition.ImpulseDuration = Mathf.Max(0.01f, duracion);
            _impulsoAterrizaje.GenerateImpulse(fuerza);
        }

        private void CrearFuenteImpulsoAterrizaje()
        {
            _impulsoAterrizaje = gameObject.AddComponent<CinemachineImpulseSource>();
            var definicion = _impulsoAterrizaje.ImpulseDefinition;
            definicion.ImpulseChannel =
                _impulso != null && _impulso.ImpulseDefinition != null
                    ? _impulso.ImpulseDefinition.ImpulseChannel
                    : 1;
            definicion.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            definicion.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            definicion.DissipationDistance = 100f;
            definicion.ImpulseDuration = 0.14f;
            _impulsoAterrizaje.DefaultVelocity = Vector3.down;
        }

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
            lente.FieldOfView =
                _stances.Framing.FieldOfView + _fovExtra + (_pulsoNivel * FovPulso());
            lente.Dutch = _dutchBase + _dutchExtra;
            _camara.Lens = lente;
        }

        private float DistanciaPulso()
        {
            if (_config == null)
                return 0f;
            return (_config.Combat.Distance - _config.Exploration.Distance)
                * _config.CombatPulsoFactor;
        }

        private float FovPulso()
        {
            if (_config == null)
                return 0f;
            return (_config.Combat.FieldOfView - _config.Exploration.FieldOfView)
                * _config.CombatPulsoFactor;
        }

        public void PulsoCombate()
        {
            if (_config == null || _config.CombatPulsoFactor <= 0f)
                return;

            if (_rutinaPulso != null)
                StopCoroutine(_rutinaPulso);
            _rutinaPulso = StartCoroutine(RutinaPulso());
        }

        private IEnumerator RutinaPulso()
        {
            float subida = Mathf.Max(0.01f, _config.CombatPulsoSubida);
            float bajada = Mathf.Max(0.01f, _config.CombatPulsoBajada);

            while (_pulsoNivel < 1f)
            {
                _pulsoNivel = Mathf.Min(1f, _pulsoNivel + (Time.deltaTime / subida));
                yield return null;
            }

            while (_pulsoNivel > 0f)
            {
                _pulsoNivel = Mathf.Max(0f, _pulsoNivel - (Time.deltaTime / bajada));
                yield return null;
            }

            _rutinaPulso = null;
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
