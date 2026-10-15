using System.Collections.Generic;
using TDOM.Data;
using UnityEngine;

namespace TDOM.Unity
{
    /// <summary>
    /// Parpadeo local (sin red) de las áreas de aviso de un ataque de AREK. Cada ataque lo inicia
    /// desde su AvisoRpc, así que corre igual en todas las máquinas. Actualiza las propiedades del
    /// shader AvisoAtaque con un MaterialPropertyBlock, sin instanciar materiales.
    /// </summary>
    public class AvisoParpadeo : MonoBehaviour
    {
        /// <summary>Cómo se mide la distancia al borde en las UV del renderer.</summary>
        public enum Forma
        {
            Disco = 0, // radial desde el centro de las UV (tapa del cilindro)
            FranjaU = 1, // bordes a lo ancho en U (cara superior del cubo)
            FranjaV = 2, // bordes a lo ancho en V (LineRenderer)
        }

        private static readonly int IdProgreso = Shader.PropertyToID("_Progreso");
        private static readonly int IdColorBase = Shader.PropertyToID("_ColorBase");
        private static readonly int IdIntensidad = Shader.PropertyToID("_Intensidad");
        private static readonly int IdDuracion = Shader.PropertyToID("_Duracion");
        private static readonly int IdFrecuenciaInicial = Shader.PropertyToID("_FrecuenciaInicial");
        private static readonly int IdFrecuenciaFinal = Shader.PropertyToID("_FrecuenciaFinal");
        private static readonly int IdInicioFijo = Shader.PropertyToID("_InicioFijo");
        private static readonly int IdBrilloMinimo = Shader.PropertyToID("_BrilloMinimo");
        private static readonly int IdAnchoBorde = Shader.PropertyToID("_AnchoBorde");
        private static readonly int IdBrilloCentro = Shader.PropertyToID("_BrilloCentro");
        private static readonly int IdForma = Shader.PropertyToID("_Forma");

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private MaterialPropertyBlock _bloque;
        private AvisoAtaqueProfile _perfil;
        private Forma _forma;
        private float _duracion;
        private float _tiempo;
        private bool _activo;

        /// <summary>
        /// Empieza el aviso sobre esos renderers (reemplaza a los anteriores). Al llegar a la
        /// duración el área queda encendida fija al máximo hasta que el ataque la apague.
        /// </summary>
        public void Iniciar(
            AvisoAtaqueProfile perfil,
            float duracion,
            IEnumerable<Renderer> renderers,
            Forma forma
        )
        {
            if (perfil == null)
            {
                Debug.LogWarning("[AvisoParpadeo] Falta el AvisoAtaqueProfile en AtaquesArek.");
                return;
            }

            _bloque ??= new MaterialPropertyBlock();
            _perfil = perfil;
            _forma = forma;
            _duracion = Mathf.Max(0f, duracion);
            _tiempo = 0f;

            _renderers.Clear();
            foreach (var renderer in renderers)
                if (renderer != null)
                    _renderers.Add(renderer);

            _activo = true;
            Aplicar(0f);
        }

        private void Update()
        {
            if (!_activo)
                return;

            _tiempo += Time.deltaTime;
            float progreso = _duracion > 0f ? Mathf.Clamp01(_tiempo / _duracion) : 1f;
            Aplicar(progreso);

            // Con progreso 1 el shader queda fijo al máximo: no hace falta seguir actualizando.
            if (progreso >= 1f)
                _activo = false;
        }

        private void Aplicar(float progreso)
        {
            float inicioFijo =
                _duracion > 0f ? Mathf.Clamp01(1f - _perfil.tramoFijoFinal / _duracion) : 0f;

            foreach (var renderer in _renderers)
            {
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(_bloque);
                _bloque.SetFloat(IdProgreso, progreso);
                _bloque.SetColor(IdColorBase, _perfil.colorBase);
                _bloque.SetFloat(IdIntensidad, _perfil.intensidad);
                _bloque.SetFloat(IdDuracion, _duracion);
                _bloque.SetFloat(IdFrecuenciaInicial, _perfil.frecuenciaInicial);
                _bloque.SetFloat(IdFrecuenciaFinal, _perfil.frecuenciaFinal);
                _bloque.SetFloat(IdInicioFijo, inicioFijo);
                _bloque.SetFloat(IdBrilloMinimo, _perfil.brilloMinimo);
                _bloque.SetFloat(IdAnchoBorde, _perfil.anchoBorde);
                _bloque.SetFloat(IdBrilloCentro, _perfil.brilloCentro);
                _bloque.SetFloat(IdForma, (float)_forma);
                renderer.SetPropertyBlock(_bloque);
            }
        }
    }
}
