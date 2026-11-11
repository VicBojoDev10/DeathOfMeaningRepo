using TDOM.POCO.ScriptableObjects;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public sealed class BossTransitionVisual : BossPhaseVisual
    {
        [SerializeField]
        private BossTransitionVisualProfile _perfil;

        [SerializeField]
        private Renderer[] _fuentes = System.Array.Empty<Renderer>();

        [SerializeField]
        private Renderer[] _contornos = System.Array.Empty<Renderer>();

        [SerializeField]
        private Renderer _onda;

        [SerializeField]
        private Renderer _runa;

        [SerializeField]
        private Renderer _halo;

        private static readonly int IdRimColor = Shader.PropertyToID("_RimColor");
        private static readonly int IdAlpha = Shader.PropertyToID("_GeneralAlpha");
        private static readonly int IdShake = Shader.PropertyToID("_ShakeBlend");
        private static readonly int IdDisplacement = Shader.PropertyToID("_ShakeMaxDisplacement");
        private static readonly int IdTime = Shader.PropertyToID("_TimingSeed");
        private static readonly int IdColor = Shader.PropertyToID("_Color");
        private static readonly int IdFade = Shader.PropertyToID("_FadeAmount");
        private MaterialPropertyBlock _bloque;
        private bool _visible;

        public override bool Corresponde(int fase) =>
            isActiveAndEnabled && _perfil != null && _perfil.Fase == fase;

        public override float MezclaColor(float progreso) => _perfil.MezclaColor.Evaluate(progreso);

        public override void Mostrar(float progreso, float duracion)
        {
            if (_perfil == null || _onda == null)
                return;

            _bloque ??= new MaterialPropertyBlock();
            _visible = true;
            Color color = _perfil.ColorRitual;
            float carga = Mathf.Max(0f, _perfil.Carga.Evaluate(progreso));
            _bloque.Clear();
            _bloque.SetColor(IdRimColor, color * _perfil.IntensidadContorno);
            _bloque.SetFloat(IdAlpha, carga);
            _bloque.SetFloat(IdShake, carga);
            _bloque.SetFloat(IdTime, progreso * duracion - Time.time);
            for (int i = 0; i < _contornos.Length; i++)
            {
                var contorno = _contornos[i];
                if (contorno == null)
                    continue;

                var fuente = i < _fuentes.Length ? _fuentes[i] : null;
                if (fuente == null)
                {
                    contorno.enabled = false;
                    continue;
                }
                contorno.enabled =
                    carga > 0f && fuente.enabled && fuente.gameObject.activeInHierarchy;
                Vector3 escala = fuente.transform.lossyScale;
                _bloque.SetVector(
                    IdDisplacement,
                    new Vector4(
                        DesplazamientoLocal(escala.x),
                        DesplazamientoLocal(escala.y),
                        DesplazamientoLocal(escala.z),
                        0f
                    )
                );
                contorno.SetPropertyBlock(_bloque);
            }

            float opacidad = Mathf.Clamp01(_perfil.OpacidadOnda.Evaluate(progreso));
            _onda.enabled = opacidad > 0f;
            _onda.transform.localScale =
                Vector3.one
                * _perfil.RadioOnda
                * Mathf.Max(0f, _perfil.ExpansionOnda.Evaluate(progreso));
            _bloque.Clear();
            _bloque.SetColor(IdColor, color * _perfil.IntensidadContorno);
            _bloque.SetFloat(IdAlpha, opacidad);
            _onda.SetPropertyBlock(_bloque);
            MostrarRuna(progreso, color);
            MostrarHalo(progreso, duracion, color);
        }

        private void MostrarHalo(float progreso, float duracion, Color color)
        {
            if (_halo == null)
                return;

            float opacidad = Mathf.Clamp01(_perfil.OpacidadHalo.Evaluate(progreso));
            _halo.enabled = opacidad > 0f;
            _halo.transform.localScale = new Vector3(
                _perfil.RadioHalo,
                _perfil.AlturaHalo,
                _perfil.RadioHalo
            );
            _bloque.Clear();
            _bloque.SetColor(IdColor, color * _perfil.IntensidadHalo);
            _bloque.SetFloat(IdAlpha, opacidad);
            _bloque.SetFloat(IdTime, progreso * duracion - Time.time);
            _halo.SetPropertyBlock(_bloque);
        }

        private void MostrarRuna(float progreso, Color color)
        {
            if (_runa == null)
                return;

            float opacidad = Mathf.Clamp01(_perfil.OpacidadRuna.Evaluate(progreso));
            _runa.enabled = opacidad > 0f;
            _runa.transform.localScale = Vector3.one * _perfil.RadioRuna;
            _bloque.Clear();
            _bloque.SetColor(IdColor, color * _perfil.IntensidadRuna);
            _bloque.SetFloat(IdAlpha, opacidad);
            _bloque.SetFloat(IdFade, 1f - Mathf.Clamp01(_perfil.TrazoRuna.Evaluate(progreso)));
            _runa.SetPropertyBlock(_bloque);
        }

        private float DesplazamientoLocal(float escala) =>
            Mathf.Abs(escala) > Mathf.Epsilon ? _perfil.Vibracion / Mathf.Abs(escala) : 0f;

        public override void Ocultar()
        {
            if (!_visible)
                return;
            foreach (var contorno in _contornos)
                if (contorno != null)
                    contorno.enabled = false;
            if (_onda != null)
                _onda.enabled = false;
            if (_runa != null)
                _runa.enabled = false;
            if (_halo != null)
                _halo.enabled = false;
            _visible = false;
        }

        private void OnDisable() => Ocultar();

        private void OnValidate()
        {
            _fuentes ??= System.Array.Empty<Renderer>();
            _contornos ??= System.Array.Empty<Renderer>();
            if (_fuentes.Length != _contornos.Length)
                Debug.LogWarning(
                    "Cada contorno de transición debe tener una fuente asignada.",
                    this
                );
        }
    }
}
