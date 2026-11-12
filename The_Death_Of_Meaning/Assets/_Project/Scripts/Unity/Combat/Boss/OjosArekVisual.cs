using TDOM.Gameplay.Combat;
using TDOM.POCO.ScriptableObjects;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    /// <summary>
    /// Pinta el estado de cada ojo del jefe cambiando materiales, sin animaciones. Cada HitZone
    /// (collider) va pegado al hueso de su ojo y el color se aplica al renderer del modelo
    /// (OjoLow_nn), que lleva un segundo material aditivo (OjoGlow) encima del original. Un ojo
    /// con la vida llena lleva además un contorno blanco y un halo (Rim, inflado hacia afuera) que
    /// lo marcan como interactuable; en cuanto recibe daño se quitan y el ojo se va poniendo rojo
    /// (glow); al destruirse todo el renderer pasa a un material negro sin iluminación con la
    /// superficie inflada hacia adentro (se hunde). Si un
    /// ojo no tiene renderer de modelo asignado se pinta el renderer del propio HitZone
    /// (placeholder). De la fase 2 a la 5 también dibuja los escudos de cada ojo
    /// (EscudosOjosVisual), si OjosArek los tiene activados.
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

        [Header("Materiales por estado")]
        [Tooltip(
            "Contorno blanco (AllIn13DShaderOutline, solo perímetro). Se agrega al ojo mientras tiene la vida llena. Vacío = sin borde."
        )]
        [SerializeField]
        private Material _materialBorde;

        [Tooltip(
            "Halo del ojo intacto (AllIn13D con Rim e inflado hacia afuera, aditivo). Se agrega junto con el contorno mientras el ojo tiene la vida llena. Vacío = sin halo."
        )]
        [SerializeField]
        private Material _materialAura;

        [Tooltip(
            "Material del ojo destruido (negro, AllIn13D sin iluminación y con la superficie inflada hacia adentro). Reemplaza todos los materiales del renderer. Vacío = el ojo solo se apaga."
        )]
        [SerializeField]
        private Material _materialDestruido;

        [Header("Escudos (fases 2 a 5)")]
        [Tooltip("Aspecto de los escudos. Vacío = no se dibujan.")]
        [SerializeField]
        private EscudoOjosProfile _perfilEscudo;

        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");

        // Daño (0 a 1) por debajo del cual el ojo todavía cuenta como intacto.
        private const float UmbralDanio = 0.001f;

        private struct OjoPintable
        {
            public Renderer Render;
            public int IndiceMaterial; // -1 = placeholder: todo el renderer, color base
            public Material[] Normal; // materiales del modelo tal como vienen
            public Material[] ConBorde; // lo mismo más el contorno al final
            public Material[] Destruido; // todos los espacios con el material negro
            public Material[] Actual; // lista que tiene puesta ahora el renderer
        }

        private OjoPintable[] _pintables;
        private EstadoOjo[] _estadoOjos;
        private float[] _vidaOjos; // vida que le queda a cada ojo, de 0 a 1
        private MaterialPropertyBlock _bloque;
        private EscudosOjosVisual _escudos;

        private void Awake()
        {
            if (_ojos == null)
                _ojos = GetComponent<OjosArek>();
            if (_jefe == null)
                _jefe = GetComponent<BossController>();

            _bloque = new MaterialPropertyBlock();
            PrepararRenderers();
        }

        private void OnEnable()
        {
            _ojos.EstadoOjoCambiado += Pintar;
            _ojos.VidaOjoCambiada += PintarVida;
            _ojos.EscudoOjoCambiado += PintarEscudo;
        }

        private void OnDisable()
        {
            _ojos.EstadoOjoCambiado -= Pintar;
            _ojos.VidaOjoCambiada -= PintarVida;
            _ojos.EscudoOjoCambiado -= PintarEscudo;
        }

        private void LateUpdate() => _escudos?.Actualizar(Time.deltaTime);

        private void OnDestroy() => _escudos?.Destruir();

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
            _estadoOjos = new EstadoOjo[zonas.Length];
            _vidaOjos = new float[zonas.Length];
            for (int i = 0; i < zonas.Length; i++)
            {
                _estadoOjos[i] = EstadoOjo.Abierto;
                _vidaOjos[i] = 1f;
            }

            for (int ojo = 0; ojo < zonas.Length; ojo++)
            {
                Renderer modelo =
                    _renderersModelo != null && ojo < _renderersModelo.Length
                        ? _renderersModelo[ojo]
                        : null;

                if (modelo != null && modelo.sharedMaterials.Length > _indiceMaterialGlow)
                {
                    var pintable = new OjoPintable
                    {
                        Render = modelo,
                        IndiceMaterial = _indiceMaterialGlow,
                    };
                    PrepararMateriales(
                        ref pintable,
                        _materialBorde,
                        _materialAura,
                        _materialDestruido
                    );
                    _pintables[ojo] = pintable;
                    continue;
                }

                _pintables[ojo] = new OjoPintable
                {
                    Render = zonas[ojo] != null ? zonas[ojo].GetComponent<Renderer>() : null,
                    IndiceMaterial = -1,
                };
            }
        }

        private void PrepararEscudos()
        {
            if (_perfilEscudo == null)
                return;

            var zonas = _jefe.HitZones;
            if (zonas == null || zonas.Length == 0)
                zonas = GetComponentsInChildren<HitZone>();

            _escudos = new EscudosOjosVisual(_perfilEscudo, transform, zonas);
        }

        private void PintarEscudo(int ojo, byte capas)
        {
            // Los objetos de los escudos se crean la primera vez que hace falta uno: con los
            // escudos apagados no se crea nada.
            if (_escudos == null)
            {
                if (capas == 0)
                    return;

                PrepararEscudos();
            }

            _escudos?.Poner(ojo, capas);
        }

        private static void PrepararMateriales(
            ref OjoPintable pintable,
            Material borde,
            Material aura,
            Material destruido
        )
        {
            var originales = pintable.Render.sharedMaterials;
            pintable.Normal = originales;
            pintable.Actual = originales;

            // Lo que lleva el ojo intacto: sus materiales, el contorno y el halo.
            int extras = (borde != null ? 1 : 0) + (aura != null ? 1 : 0);
            if (extras > 0)
            {
                var conBorde = new Material[originales.Length + extras];
                originales.CopyTo(conBorde, 0);
                int siguiente = originales.Length;
                if (borde != null)
                    conBorde[siguiente++] = borde;
                if (aura != null)
                    conBorde[siguiente] = aura;
                pintable.ConBorde = conBorde;
            }

            if (destruido != null)
            {
                var negros = new Material[originales.Length];
                for (int i = 0; i < negros.Length; i++)
                    negros[i] = destruido;
                pintable.Destruido = negros;
            }
        }

        private void Pintar(int ojo, EstadoOjo estado)
        {
            if (ojo < 0 || ojo >= _pintables.Length)
                return;

            _estadoOjos[ojo] = estado;

            ActualizarMateriales(ojo);
            PintarColor(ojo);
        }

        // Qué lista de materiales lleva el ojo: negro si está destruido, con contorno y halo
        // mientras conserva toda la vida, y el modelo normal (más el glow rojo) en cuanto lo
        // golpean.
        // Se hace antes de pintar el color: cambiar la lista no debe pisar el bloque de
        // propiedades del material de glow.
        private void ActualizarMateriales(int ojo)
        {
            ref OjoPintable pintable = ref _pintables[ojo];
            if (pintable.Render == null || pintable.IndiceMaterial < 0)
                return;

            Material[] deseados;
            if (_estadoOjos[ojo] != EstadoOjo.Abierto)
                deseados = pintable.Destruido ?? pintable.Normal;
            else if (1f - _vidaOjos[ojo] <= UmbralDanio)
                deseados = pintable.ConBorde ?? pintable.Normal;
            else
                deseados = pintable.Normal;

            if (deseados == pintable.Actual)
                return;

            pintable.Render.sharedMaterials = deseados;
            pintable.Actual = deseados;
        }

        private void PintarVida(int ojo, float vidaNormalizada)
        {
            if (ojo < 0 || ojo >= _pintables.Length)
                return;

            _vidaOjos[ojo] = Mathf.Clamp01(vidaNormalizada);
            ActualizarMateriales(ojo);
            PintarColor(ojo);
        }

        // Un ojo con la vida llena se ve sin color (se ve el modelo con su contorno); mientras más
        // daño lleva más rojo se pone, y al quedarse sin vida se cierra el párpado y se apaga.
        private void PintarColor(int ojo)
        {
            var perfil = _ojos.Perfil;
            if (perfil == null)
                return;

            var pintable = _pintables[ojo];
            if (pintable.Render == null)
                return;

            EstadoOjo estado = _estadoOjos[ojo];
            float danio = 1f - _vidaOjos[ojo];

            if (pintable.IndiceMaterial < 0)
            {
                pintable.Render.GetPropertyBlock(_bloque);
                _bloque.SetColor(IdColorBase, ColorPlaceholder(perfil, estado, danio));
                pintable.Render.SetPropertyBlock(_bloque);
                return;
            }

            // Destruido: el material negro no lleva color de glow.
            if (estado != EstadoOjo.Abierto && pintable.Destruido != null)
            {
                pintable.Render.SetPropertyBlock(null, pintable.IndiceMaterial);
                return;
            }

            pintable.Render.GetPropertyBlock(_bloque, pintable.IndiceMaterial);
            _bloque.SetColor(IdColorBase, ColorGlow(perfil, estado, danio));
            pintable.Render.SetPropertyBlock(_bloque, pintable.IndiceMaterial);
        }

        // Cerrado o sin daño = sin glow (se ve la textura original); con daño suma rojo encima.
        private static Color ColorGlow(WeakPointProfile perfil, EstadoOjo estado, float danio)
        {
            if (estado != EstadoOjo.Abierto || danio <= UmbralDanio)
                return new Color(0f, 0f, 0f, 0f);

            Color rojo = perfil.ColorAbierto;
            float fuerza = Mathf.Max(0f, perfil.IntensidadGlow) * danio;
            return new Color(rojo.r * fuerza, rojo.g * fuerza, rojo.b * fuerza, 1f);
        }

        // Placeholder (el renderer del propio HitZone): del color de cerrado al de abierto.
        private static Color ColorPlaceholder(WeakPointProfile perfil, EstadoOjo estado, float danio)
        {
            if (estado != EstadoOjo.Abierto)
                return perfil.ColorCerrado;

            return Color.Lerp(perfil.ColorCerrado, perfil.ColorAbierto, danio);
        }
    }
}
