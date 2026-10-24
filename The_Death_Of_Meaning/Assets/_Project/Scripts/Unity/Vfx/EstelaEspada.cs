using System;
using TDOM.Data;
using TDOM.Gameplay.Vfx;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    /// <summary>
    /// Estela de la espada: una cinta entre la base y la punta de la hoja. Solo deja muestras
    /// mientras el ataque la permite (<see cref="Permitir"/>) y la punta se mueve más rápido
    /// que el umbral de velocidad. Cada vez que se corta (cancelación de ataque, cambio de
    /// estado, salto brusco de la hoja) el tramo siguiente empieza aparte y no se une al
    /// anterior. Corre después de AylaEspada para muestrear la pose final del socket.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class EstelaEspada : MonoBehaviour
    {
        private const int VerticesPorMuestra = 2;
        private const int IndicesPorSegmento = 6;

        // La muestra mas nueva no debe ser mas vieja que esto para usar la estela como marco
        // (evita colocar un efecto sobre la cola de un tajo anterior).
        private const float EdadMaximaParaMarco = 0.3f;

        [Serializable]
        private struct PerfilEstela
        {
            public string Id;
            public EstelaEspadaProfile Perfil;
        }

        [SerializeField]
        private Transform _base;

        [SerializeField]
        private Transform _punta;

        [SerializeField]
        private EstelaEspadaProfile _perfilPorDefecto;

        [SerializeField]
        private PerfilEstela[] _perfiles = new PerfilEstela[0];

        [Header("Cuándo deja estela")]
        [
            SerializeField,
            Min(0f),
            Tooltip("Velocidad de la punta (m/s) a partir de la cual empieza a dejar estela.")
        ]
        private float _velocidadEncendido = 3f;

        [
            SerializeField,
            Min(0f),
            Tooltip("Velocidad (m/s) por debajo de la cual deja de dejar estela.")
        ]
        private float _velocidadApagado = 1.5f;

        [
            SerializeField,
            Min(0f),
            Tooltip(
                "Si la punta se mueve más que esto (metros) en un solo frame se considera un "
                    + "salto (cambio brusco de pose) y se corta la estela en vez de unirla."
            )
        ]
        private float _saltoMaximo = 0.75f;

        private EstelaEspadaProfile _perfil;
        private MuestrasEstela _muestras;
        private Mesh _malla;
        private Vector3[] _vertices = new Vector3[0];
        private Vector2[] _uvs = new Vector2[0];
        private Color[] _colores = new Color[0];
        private int[] _indices = new int[0];
        private CompuertaEstela _compuerta;
        private Vector3 _puntaPrevia;
        private bool _tienePrevia;
        private bool _permitida;
        private bool _emitiendo;

        /// <summary>True mientras esta dejando muestras (permitida y con velocidad).</summary>
        public bool Emitiendo => _emitiendo;

        private void Awake()
        {
            _malla = new Mesh { name = "EstelaEspada" };
            _malla.MarkDynamic();
            _compuerta = new CompuertaEstela(_velocidadEncendido, _velocidadApagado);
            UsarPerfil(_perfilPorDefecto);
        }

        private void OnEnable()
        {
            _tienePrevia = false;
        }

        /// <summary>
        /// Lo llama el dueño de la espada cada frame: true si el ataque actual deja estela.
        /// El perfil solo se cambia mientras esta permitida, para no borrar la cola que se
        /// esta desvaneciendo al terminar el ataque.
        /// </summary>
        public void Permitir(bool permitida, string idPerfil = "")
        {
            _permitida = permitida;
            if (!permitida)
                return;

            EstelaEspadaProfile nuevo = BuscarPerfil(idPerfil);
            if (nuevo == null || nuevo == _perfil)
                return;

            Cortar();
            UsarPerfil(nuevo);
        }

        /// <summary>
        /// Termina el tramo actual. La siguiente muestra empieza un tramo nuevo, sin unirse al
        /// anterior; lo ya dibujado se desvanece solo.
        /// </summary>
        public void Cortar()
        {
            _muestras?.Cortar();
            _emitiendo = false;
        }

        /// <summary>
        /// Pone la base y la punta en los extremos de la hoja, tomados de la malla (su lado más
        /// largo). La base es el extremo más cercano al mango. Devuelve false si no se pudo.
        /// </summary>
        public bool AjustarAHoja(MeshFilter hoja, Vector3 centroMango)
        {
            if (hoja == null || hoja.sharedMesh == null || _base == null || _punta == null)
                return false;

            Bounds limites = hoja.sharedMesh.bounds;
            Vector3 tam = limites.size;
            int eje = 0;
            if (tam.y > tam[eje])
                eje = 1;
            if (tam.z > tam[eje])
                eje = 2;

            Vector3 extremoA = limites.center;
            Vector3 extremoB = limites.center;
            extremoA[eje] = limites.min[eje];
            extremoB[eje] = limites.max[eje];
            Vector3 mundoA = hoja.transform.TransformPoint(extremoA);
            Vector3 mundoB = hoja.transform.TransformPoint(extremoB);

            bool aEsBase =
                (mundoA - centroMango).sqrMagnitude <= (mundoB - centroMango).sqrMagnitude;
            _base.position = aEsBase ? mundoA : mundoB;
            _punta.position = aEsBase ? mundoB : mundoA;
            _tienePrevia = false;
            return true;
        }

        public bool Hoja(out Vector3 baseHoja, out Vector3 punta)
        {
            baseHoja = _base != null ? _base.position : Vector3.zero;
            punta = _punta != null ? _punta.position : Vector3.zero;
            return _base != null && _punta != null;
        }

        /// <summary>
        /// Posición y rotación de la estela vigente (ver <see cref="MarcoEstela"/>), para poner
        /// efectos sobre ella. False si no hay estela reciente.
        /// </summary>
        public bool Marco(out Vector3 centro, out Quaternion rotacion) =>
            MarcoEstela.Calcular(_muestras, EdadMaximaParaMarco, out centro, out rotacion);

        private void OnDisable()
        {
            _emitiendo = false;
            _tienePrevia = false;
            _compuerta?.Reiniciar();
            _muestras?.Vaciar();
            _malla?.Clear();
        }

        private void OnDestroy()
        {
            if (_malla != null)
                Destroy(_malla);
        }

        private void LateUpdate()
        {
            if (_perfil == null || _muestras == null)
                return;

            float dt = Time.deltaTime;
            _muestras.Envejecer(dt, _perfil.duracionVida);
            ActualizarEmision(dt);

            ConstruirMalla();
            Dibujar();
        }

        private EstelaEspadaProfile BuscarPerfil(string idPerfil)
        {
            if (string.IsNullOrEmpty(idPerfil))
                return _perfilPorDefecto;

            foreach (var entrada in _perfiles)
            {
                if (entrada.Id == idPerfil && entrada.Perfil != null)
                    return entrada.Perfil;
            }

            return _perfilPorDefecto;
        }

        private void UsarPerfil(EstelaEspadaProfile perfil)
        {
            if (perfil == null || perfil == _perfil)
                return;

            _perfil = perfil;
            int capacidad = Mathf.Max(2, perfil.maxPuntos);
            if (_muestras != null && _muestras.Capacidad == capacidad)
                return;

            _muestras = new MuestrasEstela(capacidad);
            _vertices = new Vector3[capacidad * VerticesPorMuestra];
            _uvs = new Vector2[_vertices.Length];
            _colores = new Color[_vertices.Length];
            _indices = new int[(capacidad - 1) * IndicesPorSegmento];
        }

        private void ActualizarEmision(float dt)
        {
            if (_base == null || _punta == null || dt <= 1e-5f)
                return;

            Vector3 punta = _punta.position;
            float distancia = _tienePrevia ? Vector3.Distance(punta, _puntaPrevia) : 0f;
            bool salto = distancia > _saltoMaximo;
            _puntaPrevia = punta;
            _tienePrevia = true;

            bool abierta = _compuerta.Evaluar(distancia / dt, _permitida);
            if (abierta && !salto)
            {
                _muestras.Agregar(_base.position, punta, _perfil.distanciaMinima);
                _emitiendo = true;
                return;
            }

            if (_emitiendo || salto)
                Cortar();
        }

        private void ConstruirMalla()
        {
            _malla.Clear();
            int cantidad = _muestras.Cantidad;
            if (cantidad < 2)
                return;

            int indicesUsados = 0;
            for (int i = 1; i < cantidad; i++)
            {
                if (!_muestras[i].ConectaConAnterior)
                    continue;

                int v = (i - 1) * VerticesPorMuestra;
                _indices[indicesUsados++] = v;
                _indices[indicesUsados++] = v + 1;
                _indices[indicesUsados++] = v + 2;
                _indices[indicesUsados++] = v + 2;
                _indices[indicesUsados++] = v + 1;
                _indices[indicesUsados++] = v + 3;
            }

            if (indicesUsados == 0)
                return;

            for (int i = 0; i < cantidad; i++)
            {
                MuestraEstela muestra = _muestras[i];
                float edad = Mathf.Clamp01(muestra.Edad / _perfil.duracionVida);
                float ancho = FormaEstela.Ancho(
                    _perfil.curvaAncho.Evaluate(edad),
                    _perfil.escalaAncho
                );
                Color color = _perfil.gradienteColor.Evaluate(edad);
                color.a = FormaEstela.Opacidad(color.a, _perfil.opacidadMinima);
                int v = i * VerticesPorMuestra;

                // El borde de la punta no se mueve; el otro borde va de la base (ancho 1) hacia
                // la punta (ancho 0). La muestra mas nueva (junto a la espada) es la mas ancha
                // y la estela se cierra en punta hacia el final de su vida.
                _vertices[v] = Vector3.LerpUnclamped(muestra.Punta, muestra.BaseHoja, ancho);
                _vertices[v + 1] = muestra.Punta;
                _uvs[v] = new Vector2(edad, 0f);
                _uvs[v + 1] = new Vector2(edad, 1f);
                _colores[v] = color;
                _colores[v + 1] = color;
            }

            int totalVertices = cantidad * VerticesPorMuestra;
            _malla.SetVertices(_vertices, 0, totalVertices);
            _malla.SetUVs(0, _uvs, 0, totalVertices);
            _malla.SetColors(_colores, 0, totalVertices);
            _malla.SetTriangles(_indices, 0, indicesUsados, 0);
            _malla.RecalculateBounds();
        }

        private void Dibujar()
        {
            if (_perfil.material == null || _malla.vertexCount == 0)
                return;

            var parametros = new RenderParams(_perfil.material) { layer = gameObject.layer };
            Graphics.RenderMesh(parametros, _malla, 0, Matrix4x4.identity);
        }
    }
}
