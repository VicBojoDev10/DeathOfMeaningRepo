using TDOM.POCO.ScriptableObjects;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public sealed class BossAbsorptionVisual : BossPhaseVisual
    {
        [SerializeField]
        private BossAbsorptionVisualProfile _perfil;

        [SerializeField]
        private Renderer[] _simbolos = System.Array.Empty<Renderer>();

        [SerializeField]
        private Renderer[] _fragmentos = System.Array.Empty<Renderer>();

        [SerializeField]
        private Renderer _aura;

        [SerializeField]
        private Renderer[] _contornos = System.Array.Empty<Renderer>();
        private static readonly int IdColor = Shader.PropertyToID("_Color");
        private static readonly int IdAlpha = Shader.PropertyToID("_GeneralAlpha");
        private static readonly int IdTime = Shader.PropertyToID("_TimingSeed");
        private static readonly int IdTrazo = Shader.PropertyToID("_Trazo");
        private static readonly int IdIntensity = Shader.PropertyToID("_Intensity");
        private static readonly int IdRim = Shader.PropertyToID("_RimColor");
        private const float VueltaCompleta = Mathf.PI * 2f;
        private const int FilasOrbita = 2;
        private MaterialPropertyBlock _bloque;
        private UnityEngine.Camera _camara;

        public override bool Corresponde(int fase) =>
            isActiveAndEnabled && _perfil != null && _perfil.Fase == fase;

        public override float MezclaColor(float progreso) => _perfil.MezclaColor.Evaluate(progreso);

        private void Awake() => _camara = UnityEngine.Camera.main;

        public override void Mostrar(float progreso, float duracion)
        {
            _bloque ??= new MaterialPropertyBlock();
            MostrarFragmentos(progreso);
            MostrarSimbolos(progreso);
            MostrarAura(progreso, duracion);
        }

        private void MostrarSimbolos(float progreso)
        {
            int porFila = Mathf.CeilToInt((float)_simbolos.Length / FilasOrbita);
            for (int i = 0; i < _simbolos.Length; i++)
            {
                float orden = (float)i / _simbolos.Length;
                float absorcion = Mathf.Clamp01(
                    _perfil.Absorcion.Evaluate(progreso - orden * _perfil.DesfaseAbsorcion)
                );
                float angulo =
                    VueltaCompleta
                    * (
                        (float)(i % porFila) / porFila
                        + progreso * _perfil.Vueltas
                        + (float)(i / porFila) / _simbolos.Length
                    );
                var radial = new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo));
                var origen = _perfil.CentroOrbita + radial * _perfil.RadioOrbita;
                origen.y += (i / porFila) * _perfil.SeparacionVertical;
                var simbolo = _simbolos[i];
                simbolo.transform.localPosition = Vector3.Lerp(
                    origen,
                    _perfil.DestinoAbsorcion,
                    absorcion
                );
                simbolo.transform.localScale =
                    Vector3.one * _perfil.TamanoSimbolo * (1f - absorcion);
                if (_camara != null)
                    simbolo.transform.rotation = _camara.transform.rotation;
                float alpha = _perfil.OpacidadSimbolos.Evaluate(progreso) * (1f - absorcion);
                simbolo.enabled = alpha > 0f;
                _bloque.Clear();
                _bloque.SetFloat(IdAlpha, alpha);
                _bloque.SetFloat(IdIntensity, _perfil.IntensidadSimbolos);
                _bloque.SetFloat(IdTrazo, _perfil.AparicionSimbolos.Evaluate(progreso));
                simbolo.SetPropertyBlock(_bloque);
            }
        }

        private void MostrarFragmentos(float progreso)
        {
            float ruptura = _perfil.Ruptura.Evaluate(progreso);
            float alpha = _perfil.OpacidadRuna.Evaluate(progreso);
            _bloque.Clear();
            _bloque.SetColor(IdColor, _perfil.ColorRuna * _perfil.IntensidadRuna);
            _bloque.SetFloat(IdAlpha, alpha);
            for (int i = 0; i < _fragmentos.Length; i++)
            {
                var fragmento = _fragmentos[i];
                float angulo = VueltaCompleta * i / _fragmentos.Length;
                var radial = new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo));
                fragmento.transform.localPosition =
                    radial * ruptura * _perfil.RadioRuna
                    + Vector3.up * ruptura * _perfil.AlturaFragmentos;
                fragmento.transform.localRotation = Quaternion.AngleAxis(
                    ruptura * _perfil.GiroFragmentos,
                    radial
                );
                fragmento.transform.localScale = Vector3.one * _perfil.RadioRuna;
                fragmento.enabled = alpha > 0f;
                fragmento.SetPropertyBlock(_bloque);
            }
        }

        private void MostrarAura(float progreso, float duracion)
        {
            Color color = _perfil.ColorAura.Evaluate(progreso);
            float carga = _perfil.CargaAura.Evaluate(progreso);
            _aura.transform.localScale = _perfil.EscalaAura;
            _aura.enabled = carga > 0f;
            _bloque.Clear();
            _bloque.SetColor(IdColor, Color.white * _perfil.IntensidadAura);
            _bloque.SetFloat(IdAlpha, carga);
            _bloque.SetFloat(IdTime, progreso * duracion - Time.time);
            _aura.SetPropertyBlock(_bloque);
            _bloque.Clear();
            _bloque.SetColor(IdRim, color * _perfil.IntensidadAura);
            _bloque.SetFloat(IdAlpha, carga);
            foreach (var contorno in _contornos)
            {
                contorno.enabled = carga > 0f;
                contorno.SetPropertyBlock(_bloque);
            }
        }

        public override void Ocultar()
        {
            foreach (var simbolo in _simbolos)
                simbolo.enabled = false;
            foreach (var fragmento in _fragmentos)
                fragmento.enabled = false;
            foreach (var contorno in _contornos)
                contorno.enabled = false;
            if (_aura != null)
                _aura.enabled = false;
        }

        private void OnDisable() => Ocultar();
    }
}
