using TDOM.Gameplay.Combat;
using TDOM.POCO.ScriptableObjects;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    /// <summary>
    /// Pinta el estado de cada ojo del jefe. Con el modelo animado, cada HitZone (collider) va
    /// pegado al hueso de su ojo y el color se aplica al renderer del modelo (OjoLow_nn): el
    /// ojo lleva un segundo material aditivo (OjoGlow) que se dibuja encima del original, así
    /// el glow sigue la animación y no se pierde la textura. Si un ojo no tiene renderer de
    /// modelo asignado se pinta el renderer del propio HitZone (placeholder).
    /// </summary>
    [RequireComponent(typeof(OjosArek), typeof(BossController))]
    public sealed class OjosArekVisual : MonoBehaviour
    {
        [SerializeField]
        private OjosArek _ojos;

        [SerializeField]
        private BossController _jefe;

        [Header("Ojos del modelo (OjoLow)")]
        [Tooltip(
            "Renderer del modelo de cada ojo, en el mismo orden que los HitZones de BossController."
        )]
        [SerializeField]
        private Renderer[] _renderersModelo;

        [Tooltip(
            "Posición del material de glow en el renderer (el segundo, que se dibuja encima del original)."
        )]
        [SerializeField]
        private int _indiceMaterialGlow = 1;

        [Header("Párpados")]
        [Tooltip(
            "Párpado animado de cada ojo, en el mismo orden que los HitZones de BossController. Un hueco = ese ojo sin párpado."
        )]
        [SerializeField]
        private ParpadoOjo[] _parpados;

        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");

        private struct OjoPintable
        {
            public Renderer Render;
            public int IndiceMaterial; // -1 = placeholder: todo el renderer, color base
        }

        private OjoPintable[] _pintables;
        private MaterialPropertyBlock _bloque;

        private void Awake()
        {
            if (_ojos == null)
                _ojos = GetComponent<OjosArek>();
            if (_jefe == null)
                _jefe = GetComponent<BossController>();

            _bloque = new MaterialPropertyBlock();
            PrepararRenderers();
        }

        private void OnEnable() => _ojos.EstadoOjoCambiado += Pintar;

        private void OnDisable() => _ojos.EstadoOjoCambiado -= Pintar;

        /// <summary>True si el renderer es un ojo del modelo (BossController no lo trata como cuerpo).</summary>
        public bool UsaRenderer(Renderer render)
        {
            if (_renderersModelo == null)
                return false;

            foreach (var ojo in _renderersModelo)
            {
                if (ojo != null && ojo == render)
                    return true;
            }
            return false;
        }

        private void PrepararRenderers()
        {
            var zonas = _jefe.HitZones;
            if (zonas == null || zonas.Length == 0)
                zonas = GetComponentsInChildren<HitZone>();

            _pintables = new OjoPintable[zonas.Length];
            for (int ojo = 0; ojo < zonas.Length; ojo++)
            {
                Renderer modelo =
                    _renderersModelo != null && ojo < _renderersModelo.Length
                        ? _renderersModelo[ojo]
                        : null;

                if (modelo != null && modelo.sharedMaterials.Length > _indiceMaterialGlow)
                {
                    _pintables[ojo] = new OjoPintable
                    {
                        Render = modelo,
                        IndiceMaterial = _indiceMaterialGlow,
                    };
                    continue;
                }

                _pintables[ojo] = new OjoPintable
                {
                    Render = zonas[ojo] != null ? zonas[ojo].GetComponent<Renderer>() : null,
                    IndiceMaterial = -1,
                };
            }
        }

        private void Pintar(int ojo, EstadoOjo estado)
        {
            if (_parpados != null && ojo >= 0 && ojo < _parpados.Length && _parpados[ojo] != null)
                _parpados[ojo].Aplicar(estado);

            var perfil = _ojos.Perfil;
            if (perfil == null || ojo < 0 || ojo >= _pintables.Length)
                return;

            var pintable = _pintables[ojo];
            if (pintable.Render == null)
                return;

            if (pintable.IndiceMaterial < 0)
            {
                pintable.Render.GetPropertyBlock(_bloque);
                _bloque.SetColor(IdColorBase, ColorDe(perfil, estado));
                pintable.Render.SetPropertyBlock(_bloque);
                return;
            }

            pintable.Render.GetPropertyBlock(_bloque, pintable.IndiceMaterial);
            _bloque.SetColor(IdColorBase, ColorGlow(perfil, estado));
            pintable.Render.SetPropertyBlock(_bloque, pintable.IndiceMaterial);
        }

        // Cerrado = sin glow (se ve la textura original); el resto suma su color encima.
        private static Color ColorGlow(WeakPointProfile perfil, EstadoOjo estado)
        {
            if (estado == EstadoOjo.Cerrado)
                return new Color(0f, 0f, 0f, 0f);

            Color color = ColorDe(perfil, estado);
            float intensidad = Mathf.Max(0f, perfil.IntensidadGlow);
            return new Color(color.r * intensidad, color.g * intensidad, color.b * intensidad, 1f);
        }

        private static Color ColorDe(WeakPointProfile perfil, EstadoOjo estado)
        {
            switch (estado)
            {
                case EstadoOjo.Brillando:
                    return perfil.ColorBrillando;
                case EstadoOjo.Abierto:
                    return perfil.ColorAbierto;
                case EstadoOjo.Enfriando:
                    return perfil.ColorEnfriando;
                default:
                    return perfil.ColorCerrado;
            }
        }
    }
}
