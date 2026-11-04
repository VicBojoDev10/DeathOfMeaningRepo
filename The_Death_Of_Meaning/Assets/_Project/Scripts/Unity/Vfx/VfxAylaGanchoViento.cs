using System;
using TDOM.Gameplay.Vfx;
using TDOM.Unity.Locomotion;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    /// <summary>
    /// Viento y sensacion de velocidad mientras Ayla es jalada por el gancho (TW-255). Vive como
    /// hijo del jugador y lo corre cada cliente por su cuenta, sin RPC: el estado del jalon sale
    /// de <see cref="GrappleVisual.Activo"/> (el unico que ya conocen todos los clientes) y la
    /// velocidad se estima con el desplazamiento del propio objeto, igual en el dueno y en el
    /// companero. Las particulas se simulan en espacio mundo, asi que Ayla las va dejando atras.
    /// La emision sube con la velocidad y, al soltar el gancho, baja sola sin cortarse.
    /// No usa VfxRed ni el catalogo de efectos A a B.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VfxAylaGanchoViento : MonoBehaviour
    {
        private const float VelocidadMinimaParaOrientar = 1.5f;
        private const float UmbralOrientarCuadrado =
            VelocidadMinimaParaOrientar * VelocidadMinimaParaOrientar;

        // Un salto mayor entre frames es un teletransporte (respawn), no una velocidad real.
        private const float VelocidadMaximaCreible = 120f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Serializable]
        private struct Capa
        {
            public ParticleSystem Particulas;

            [Min(0f)]
            public float TasaMaxima;

            public bool EstirarConLongitud;
        }

        [Header("Estado del gancho")]
        [SerializeField]
        [Tooltip("Si queda vacio se busca en el jugador (padre).")]
        private GrappleVisual _gancho;

        [Header("Parametros")]
        [SerializeField, Range(0f, 2f)]
        [Tooltip("Multiplica la emision. 1 = la de cada capa.")]
        private float _intensidad = 1f;

        [SerializeField, Min(0f)]
        [Tooltip("Largo de las lineas de velocidad (Length Scale).")]
        private float _longitud = 8f;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("Opacidad maxima del efecto.")]
        private float _opacidad = 0.8f;

        [SerializeField]
        [Tooltip("Color del viento. El alfa se ignora: usa Opacidad.")]
        private Color _color = new Color(0.85f, 0.95f, 1f, 1f);

        [Header("Velocidad y rampas")]
        [SerializeField, Min(0.1f)]
        [Tooltip("Velocidad (m/s) con la que llega al maximo. PullSpeed de Ayla = 25.")]
        private float _velocidadReferencia = 25f;

        [SerializeField, Min(0f)]
        [Tooltip("Rapidez del filtro de la velocidad estimada (1/s). Mas alto = mas directo.")]
        private float _suavizadoVelocidad = 12f;

        [SerializeField, Min(0f)]
        [Tooltip("Segundos que tarda en llegar al maximo al empezar el jalon.")]
        private float _tiempoSubida = 0.15f;

        [SerializeField, Min(0f)]
        [Tooltip("Segundos que tarda en apagarse al terminar el jalon.")]
        private float _tiempoBajada = 0.4f;

        [Header("Capas de particulas")]
        [SerializeField]
        private Capa[] _capas = new Capa[0];

        private VientoGancho _viento;
        private ParticleSystemRenderer[] _renderers = new ParticleSystemRenderer[0];
        private MaterialPropertyBlock _bloque;
        private Vector3 _posicionPrevia;
        private Vector3 _velocidad;
        private bool _tienePosicionPrevia;
        private bool _probando;
        private bool _emitiendo;

        /// <summary>Intensidad actual del efecto, de 0 a 1.</summary>
        public float IntensidadActual => _viento != null ? _viento.Intensidad : 0f;

        /// <summary>True mientras alguna capa esta emitiendo.</summary>
        public bool Emitiendo => _emitiendo;

        private void Awake()
        {
            if (_gancho == null)
                _gancho = GetComponentInParent<GrappleVisual>();

            _viento = new VientoGancho(_velocidadReferencia, _tiempoSubida, _tiempoBajada);
            _bloque = new MaterialPropertyBlock();
            _renderers = new ParticleSystemRenderer[_capas.Length];

            for (int i = 0; i < _capas.Length; i++)
            {
                ParticleSystem particulas = _capas[i].Particulas;
                if (particulas == null)
                    continue;

                _renderers[i] = particulas.GetComponent<ParticleSystemRenderer>();
                var principal = particulas.main;
                principal.simulationSpace = ParticleSystemSimulationSpace.World;
                principal.loop = true;
                principal.playOnAwake = false;
            }

            DetenerCapas(true);
        }

        private void OnEnable()
        {
            _tienePosicionPrevia = false;
            _velocidad = Vector3.zero;
        }

        private void OnDisable()
        {
            _viento?.Reiniciar();
            _probando = false;
            DetenerCapas(true);
        }

        /// <summary>
        /// Enciende o apaga el efecto sin necesidad del gancho (para probarlo en el editor).
        /// Simula la velocidad de referencia si Ayla esta quieta.
        /// </summary>
        public void Probar(bool activo)
        {
            _probando = activo;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            ActualizarVelocidad(dt);

            bool activo = _probando || (_gancho != null && _gancho.Activo);
            float rapidez = _velocidad.magnitude;
            if (_probando)
                rapidez = Mathf.Max(rapidez, _velocidadReferencia);

            float intensidad = _viento.Actualizar(activo, rapidez, dt);
            if (!_viento.Visible)
            {
                if (_emitiendo)
                    DetenerCapas(false);
                return;
            }

            if (_velocidad.sqrMagnitude >= UmbralOrientarCuadrado)
                transform.rotation = Quaternion.LookRotation(_velocidad);

            Aplicar(intensidad);
        }

        private void ActualizarVelocidad(float dt)
        {
            Vector3 posicion = transform.position;
            if (_tienePosicionPrevia)
            {
                Vector3 instantanea = (posicion - _posicionPrevia) / dt;
                if (instantanea.sqrMagnitude > VelocidadMaximaCreible * VelocidadMaximaCreible)
                {
                    _velocidad = Vector3.zero;
                }
                else
                {
                    float suavizado = 1f - Mathf.Exp(-_suavizadoVelocidad * dt);
                    _velocidad = Vector3.Lerp(_velocidad, instantanea, suavizado);
                }
            }

            _posicionPrevia = posicion;
            _tienePosicionPrevia = true;
        }

        private void Aplicar(float intensidad)
        {
            var color = new Color(_color.r, _color.g, _color.b, _opacidad * intensidad);
            float escalaEmision = intensidad * _intensidad;

            for (int i = 0; i < _capas.Length; i++)
            {
                ParticleSystem particulas = _capas[i].Particulas;
                if (particulas == null)
                    continue;

                if (!particulas.isEmitting)
                    particulas.Play(false);

                var emision = particulas.emission;
                emision.rateOverTime = _capas[i].TasaMaxima * escalaEmision;

                ParticleSystemRenderer render = _renderers[i];
                if (render == null)
                    continue;

                if (_capas[i].EstirarConLongitud)
                    render.lengthScale = _longitud;

                render.GetPropertyBlock(_bloque);
                _bloque.SetColor(ColorId, color);
                render.SetPropertyBlock(_bloque);
            }

            _emitiendo = true;
        }

        /// <summary>
        /// Deja de emitir. Con <paramref name="borrar"/> falso las particulas que ya salieron
        /// terminan su vida (apagado gradual); con verdadero se limpian de golpe.
        /// </summary>
        private void DetenerCapas(bool borrar)
        {
            for (int i = 0; i < _capas.Length; i++)
            {
                ParticleSystem particulas = _capas[i].Particulas;
                if (particulas == null)
                    continue;

                var emision = particulas.emission;
                emision.rateOverTime = 0f;
                if (borrar)
                    particulas.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                else
                    particulas.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }

            _emitiendo = false;
        }
    }
}
