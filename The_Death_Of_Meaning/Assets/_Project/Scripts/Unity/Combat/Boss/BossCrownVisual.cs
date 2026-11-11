using TDOM.POCO.ScriptableObjects;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public sealed class BossCrownVisual : BossPhaseVisual
    {
        [SerializeField]
        private BossCrownVisualProfile _perfil;

        [SerializeField]
        private Transform _corona;

        [SerializeField]
        private Renderer _exterior;

        [SerializeField]
        private Renderer _resplandor;

        [SerializeField]
        private Renderer _interior;

        [SerializeField]
        private Renderer _simbolo;

        [SerializeField]
        private Renderer _aura;
        private MaterialPropertyBlock _bloque;
        private static readonly int IdColor = Shader.PropertyToID("_Color");
        private static readonly int IdAlpha = Shader.PropertyToID("_GeneralAlpha");
        private static readonly int IdTime = Shader.PropertyToID("_TimingSeed");
        private static readonly int IdTrazo = Shader.PropertyToID("_Trazo");
        private static readonly int IdIntensity = Shader.PropertyToID("_Intensity");
        private const float Vuelta = 360f;

        public override bool Corresponde(int fase) =>
            isActiveAndEnabled && _perfil != null && _perfil.Fase == fase;

        public override float MezclaColor(float progreso) => _perfil.Ascenso.Evaluate(progreso);

        public override void Mostrar(float progreso, float duracion)
        {
            _corona.localPosition = Vector3.Lerp(
                _perfil.Origen,
                _perfil.Cabeza,
                _perfil.Ascenso.Evaluate(progreso)
            );
            MostrarCorona(
                _perfil.Aparicion.Evaluate(progreso),
                _perfil.Expansion.Evaluate(progreso)
            );
            MostrarAura(_perfil.CargaAura.Evaluate(progreso));
        }

        public override void MostrarPersistente()
        {
            _corona.localPosition = _perfil.Cabeza;
            MostrarCorona(1f, 1f);
            MostrarAura(_perfil.AuraPersistente);
        }

        private void MostrarCorona(float alpha, float escala)
        {
            _bloque ??= new MaterialPropertyBlock();
            float pulso =
                1f
                + Mathf.Sin((float)(TiempoSincronizado * _perfil.FrecuenciaPulso))
                    * _perfil.Pulsacion;
            _corona.localScale = Vector3.one * escala * pulso;
            _exterior.transform.localRotation = Quaternion.Euler(
                0f,
                (float)(TiempoSincronizado * _perfil.VelocidadGiro % Vuelta),
                0f
            );
            _exterior.transform.localScale = Vector3.one * _perfil.Radio;
            _interior.transform.localRotation = Quaternion.Euler(
                _perfil.Inclinacion,
                (float)(TiempoSincronizado * _perfil.VelocidadInterior % Vuelta),
                0f
            );
            _interior.transform.localScale = Vector3.one * _perfil.RadioInterior;
            _simbolo.transform.localRotation = _exterior.transform.localRotation;
            _bloque.Clear();
            _bloque.SetColor(IdColor, _perfil.Color * _perfil.Intensidad);
            _bloque.SetFloat(IdAlpha, alpha);
            _bloque.SetFloat(IdTime, (float)TiempoSincronizado - Time.time);
            _exterior.enabled = alpha > 0f;
            _interior.enabled = alpha > 0f;
            _exterior.SetPropertyBlock(_bloque);
            _interior.SetPropertyBlock(_bloque);
            _resplandor.transform.localRotation = _exterior.transform.localRotation;
            _resplandor.transform.localScale = _exterior.transform.localScale;
            _resplandor.enabled = alpha > 0f;
            _bloque.SetColor(IdColor, _perfil.Color * _perfil.IntensidadResplandor);
            _resplandor.SetPropertyBlock(_bloque);
            _bloque.Clear();
            _bloque.SetFloat(IdAlpha, alpha);
            _bloque.SetFloat(IdTrazo, alpha);
            _bloque.SetFloat(IdIntensity, _perfil.Intensidad);
            _simbolo.enabled = alpha > 0f;
            _simbolo.SetPropertyBlock(_bloque);
        }

        private void MostrarAura(float carga)
        {
            _aura.transform.localScale = _perfil.EscalaAura;
            _aura.enabled = carga > 0f;
            _bloque.Clear();
            _bloque.SetColor(IdColor, _perfil.Color * _perfil.IntensidadAura);
            _bloque.SetFloat(IdAlpha, carga);
            _bloque.SetFloat(IdTime, (float)TiempoSincronizado - Time.time);
            _aura.SetPropertyBlock(_bloque);
        }

        public override void Ocultar()
        {
            if (_exterior != null)
                _exterior.enabled = false;
            if (_resplandor != null)
                _resplandor.enabled = false;
            if (_interior != null)
                _interior.enabled = false;
            if (_simbolo != null)
                _simbolo.enabled = false;
            if (_aura != null)
                _aura.enabled = false;
        }

        private void OnDisable() => Ocultar();
    }
}
