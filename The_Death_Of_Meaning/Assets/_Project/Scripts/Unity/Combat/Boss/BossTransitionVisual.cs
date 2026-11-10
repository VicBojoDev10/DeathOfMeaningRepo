using TDOM.POCO.ScriptableObjects;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public sealed class BossTransitionVisual : MonoBehaviour
    {
        [SerializeField]
        private BossTransitionVisualProfile _perfil;

        [SerializeField]
        private Renderer[] _fuentes;

        [SerializeField]
        private Renderer[] _contornos;

        [SerializeField]
        private Renderer _onda;

        private static readonly int IdRimColor = Shader.PropertyToID("_RimColor");
        private static readonly int IdAlpha = Shader.PropertyToID("_GeneralAlpha");
        private static readonly int IdShake = Shader.PropertyToID("_ShakeBlend");
        private static readonly int IdDisplacement = Shader.PropertyToID("_ShakeMaxDisplacement");
        private static readonly int IdTime = Shader.PropertyToID("_TimingSeed");
        private static readonly int IdColor = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock _bloque;
        private bool _visible;

        public bool Corresponde(int fase) =>
            isActiveAndEnabled && _perfil != null && _perfil.Fase == fase;

        public float MezclaColor(float progreso) => _perfil.MezclaColor.Evaluate(progreso);

        public void Mostrar(float progreso, float duracion, Color color)
        {
            if (_perfil == null || _onda == null)
                return;

            _bloque ??= new MaterialPropertyBlock();
            _visible = true;
            float carga = Mathf.Max(0f, _perfil.Carga.Evaluate(progreso));
            _bloque.Clear();
            _bloque.SetColor(IdRimColor, color * _perfil.IntensidadContorno);
            _bloque.SetFloat(IdAlpha, carga);
            _bloque.SetFloat(IdShake, carga);
            _bloque.SetFloat(IdTime, progreso * duracion - Time.time);
            for (int i = 0; i < _contornos.Length; i++)
            {
                var contorno = _contornos[i];
                var fuente = _fuentes[i];
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
        }

        private float DesplazamientoLocal(float escala) =>
            Mathf.Abs(escala) > Mathf.Epsilon ? _perfil.Vibracion / Mathf.Abs(escala) : 0f;

        public void Ocultar()
        {
            if (!_visible)
                return;
            foreach (var contorno in _contornos)
                contorno.enabled = false;
            if (_onda != null)
                _onda.enabled = false;
            _visible = false;
        }

        private void OnDisable() => Ocultar();
    }
}
