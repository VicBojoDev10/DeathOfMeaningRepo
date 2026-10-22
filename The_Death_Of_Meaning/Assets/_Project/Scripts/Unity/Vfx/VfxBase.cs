using System;
using TDOM.Data;
using TDOM.Gameplay.Vfx;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public abstract class VfxBase : MonoBehaviour
    {
        private const int SegmentosGizmo = 24;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int IntensidadId = Shader.PropertyToID("_Intensidad");
        private static readonly int DisolucionId = Shader.PropertyToID("_Disolucion");

        [SerializeField]
        private Transform _visual;

        private ParticleSystem[] _particulas = new ParticleSystem[0];
        private Renderer[] _renderers = new Renderer[0];
        private MaterialPropertyBlock _bloque;

        private bool _entrePuntos;
        private Vector3 _aFijo;
        private Vector3 _bFijo;
        private bool _deteniendo;

        public abstract VfxProfile Perfil { get; }

        public event Action<VfxBase> Terminado;

        public bool Activo { get; private set; }
        public float Tiempo { get; private set; }
        public Vector3 A { get; private set; }
        public Vector3 B { get; private set; }
        public Vector3 Control { get; private set; }
        public Vector3 PosicionBase { get; private set; }
        public Quaternion RotacionBase { get; private set; } = Quaternion.identity;

        protected Transform Visual => _visual != null ? _visual : transform;
        protected ParticleSystem Principal { get; private set; }
        protected Quaternion Orientacion =>
            Perfil != null ? Quaternion.Euler(Perfil.orientacion) : Quaternion.identity;

        protected virtual void Awake()
        {
            _particulas = GetComponentsInChildren<ParticleSystem>(true);
            _renderers = GetComponentsInChildren<Renderer>(true);
            _bloque = new MaterialPropertyBlock();
            Principal = Visual.GetComponent<ParticleSystem>();
        }

        public void Reproducir(Vector3 posicion, Quaternion rotacion)
        {
            _entrePuntos = false;
            Iniciar(posicion, rotacion);
        }

        public void ReproducirEntre(Vector3 a, Vector3 b)
        {
            _entrePuntos = true;
            _aFijo = a;
            _bFijo = b;
            Vector3 direccion = b - a;
            Quaternion rotacion =
                direccion.sqrMagnitude > 0f
                    ? Quaternion.LookRotation(direccion, Vector3.up)
                    : Quaternion.identity;
            Iniciar(a, rotacion);
        }

        public void Detener()
        {
            if (!Activo || _deteniendo)
                return;

            _deteniendo = true;
            foreach (var particulas in _particulas)
                particulas.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        public static void CalcularTrayectoria(
            VfxProfile perfil,
            Vector3 posicion,
            Quaternion rotacion,
            out Vector3 a,
            out Vector3 control,
            out Vector3 b
        )
        {
            a = posicion + rotacion * perfil.puntoA;
            b = posicion + rotacion * perfil.puntoB;
            control = TrayectoriaVfx.Control(
                a,
                b,
                rotacion * Vector3.up,
                perfil.curvatura,
                perfil.rotacion
            );
        }

        public static Vector3 NormalDeCurva(
            VfxProfile perfil,
            Vector3 a,
            Vector3 b,
            Vector3 arriba
        ) => TrayectoriaVfx.Normal(a, b, arriba, perfil.rotacion);

        public static float CurvaturaDesdeControl(
            VfxProfile perfil,
            Vector3 a,
            Vector3 b,
            Vector3 arriba,
            Vector3 control
        ) => TrayectoriaVfx.Curvatura(a, b, arriba, perfil.rotacion, control);

        protected Vector3 PuntoEnCurva(float t) => TrayectoriaVfx.Punto(A, Control, B, t);

        protected Vector3 TangenteEnCurva(float t) => TrayectoriaVfx.Tangente(A, Control, B, t);

        protected Quaternion MirarHacia(Vector3 direccion)
        {
            Quaternion mirada =
                direccion.sqrMagnitude > 0f
                    ? Quaternion.LookRotation(direccion, RotacionBase * Vector3.up)
                    : RotacionBase;
            return mirada * Orientacion;
        }

        protected virtual bool TiempoAgotado(float tiempo) => tiempo >= Perfil.duracion;

        protected virtual float Visibilidad(float tiempo) => 1f;

        protected virtual void AlIniciar() { }

        protected abstract void Avanzar(float t);

        private void Iniciar(Vector3 posicion, Quaternion rotacion)
        {
            if (Perfil == null)
                return;

            transform.SetPositionAndRotation(posicion, rotacion);
            PosicionBase = posicion;
            RotacionBase = rotacion;
            Tiempo = 0f;
            _deteniendo = false;
            Activo = true;
            gameObject.SetActive(true);
            RecalcularTrayectoria();
            AlIniciar();
            Avanzar(0f);
            AplicarMaterial(0f);

            foreach (var particulas in _particulas)
            {
                particulas.Clear(true);
                particulas.Play(true);
            }
        }

        private void Update()
        {
            if (!Activo || Perfil == null)
                return;

            Tiempo += Time.deltaTime;
            RecalcularTrayectoria();

            float t = Perfil.duracion > 0f ? Mathf.Clamp01(Tiempo / Perfil.duracion) : 1f;
            Avanzar(t);
            AplicarMaterial(t);

            if (!_deteniendo && TiempoAgotado(Tiempo))
                Detener();

            if (_deteniendo && !QuedanParticulas())
                Finalizar();
        }

        private void RecalcularTrayectoria()
        {
            if (_entrePuntos)
            {
                A = _aFijo;
                B = _bFijo;
                Control = TrayectoriaVfx.Control(
                    A,
                    B,
                    Vector3.up,
                    Perfil.curvatura,
                    Perfil.rotacion
                );
                return;
            }

            CalcularTrayectoria(
                Perfil,
                PosicionBase,
                RotacionBase,
                out var a,
                out var control,
                out var b
            );
            A = a;
            Control = control;
            B = b;
        }

        private void AplicarMaterial(float t)
        {
            if (_bloque == null)
                return;

            float visibilidad = Visibilidad(Tiempo);
            Color color = Perfil.color.Evaluate(t);
            color.a *= visibilidad;
            float intensidad = Perfil.intensidad.Evaluate(t) * visibilidad;
            float disolucion = Perfil.disolucion.Evaluate(t);

            foreach (var render in _renderers)
            {
                if (render == null)
                    continue;

                render.GetPropertyBlock(_bloque);
                _bloque.SetColor(ColorId, color);
                _bloque.SetFloat(IntensidadId, intensidad);
                _bloque.SetFloat(DisolucionId, disolucion);
                render.SetPropertyBlock(_bloque);
            }
        }

        private bool QuedanParticulas()
        {
            foreach (var particulas in _particulas)
            {
                if (particulas.IsAlive(true))
                    return true;
            }

            return false;
        }

        private void Finalizar()
        {
            Activo = false;
            _deteniendo = false;
            gameObject.SetActive(false);
            Terminado?.Invoke(this);
        }

        private void OnDrawGizmosSelected()
        {
            if (Perfil == null)
                return;

            Vector3 a;
            Vector3 control;
            Vector3 b;
            if (Activo)
            {
                a = A;
                control = Control;
                b = B;
            }
            else
            {
                CalcularTrayectoria(
                    Perfil,
                    transform.position,
                    transform.rotation,
                    out a,
                    out control,
                    out b
                );
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(a, 0.1f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(b, 0.1f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(a, control);
            Gizmos.DrawLine(control, b);
            Gizmos.color = Color.cyan;

            Vector3 anterior = a;
            for (int i = 1; i <= SegmentosGizmo; i++)
            {
                Vector3 siguiente = TrayectoriaVfx.Punto(a, control, b, (float)i / SegmentosGizmo);
                Gizmos.DrawLine(anterior, siguiente);
                anterior = siguiente;
            }
        }
    }
}
