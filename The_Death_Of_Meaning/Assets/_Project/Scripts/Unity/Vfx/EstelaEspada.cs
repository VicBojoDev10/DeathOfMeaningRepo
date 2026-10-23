using System;
using TDOM.Data;
using TDOM.Gameplay.Vfx;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public sealed class EstelaEspada : MonoBehaviour
    {
        private const int VerticesPorMuestra = 2;
        private const int IndicesPorSegmento = 6;

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

        private EstelaEspadaProfile _perfil;
        private MuestrasEstela _muestras;
        private Mesh _malla;
        private Vector3[] _vertices = new Vector3[0];
        private Vector2[] _uvs = new Vector2[0];
        private Color[] _colores = new Color[0];
        private int[] _indices = new int[0];
        private bool _encendida;

        public bool Encendida => _encendida;

        private void Awake()
        {
            _malla = new Mesh { name = "EstelaEspada" };
            _malla.MarkDynamic();
            UsarPerfil(_perfilPorDefecto);
        }

        public void Encender(string idPerfil = "")
        {
            UsarPerfil(BuscarPerfil(idPerfil));
            if (_perfil == null || _base == null || _punta == null)
                return;

            _encendida = true;
            Muestrear();
        }

        public void Apagar()
        {
            _encendida = false;
        }

        private void OnDisable()
        {
            _encendida = false;
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

            _muestras.Envejecer(Time.deltaTime, _perfil.duracionVida);
            if (_encendida)
                Muestrear();

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

        private void Muestrear()
        {
            if (_base == null || _punta == null)
                return;

            _muestras.Agregar(_base.position, _punta.position, _perfil.distanciaMinima);
        }

        private void ConstruirMalla()
        {
            _malla.Clear();
            int cantidad = _muestras.Cantidad;
            if (cantidad < 2)
                return;

            for (int i = 0; i < cantidad; i++)
            {
                MuestraEstela muestra = _muestras[i];
                float edad = Mathf.Clamp01(muestra.Edad / _perfil.duracionVida);
                float ancho = Mathf.Clamp01(_perfil.curvaAncho.Evaluate(edad));
                Color color = _perfil.gradienteColor.Evaluate(edad);
                int v = i * VerticesPorMuestra;

                _vertices[v] = Vector3.Lerp(muestra.Punta, muestra.BaseHoja, ancho);
                _vertices[v + 1] = muestra.Punta;
                _uvs[v] = new Vector2(edad, 0f);
                _uvs[v + 1] = new Vector2(edad, 1f);
                _colores[v] = color;
                _colores[v + 1] = color;
            }

            int segmentos = cantidad - 1;
            for (int s = 0; s < segmentos; s++)
            {
                int v = s * VerticesPorMuestra;
                int k = s * IndicesPorSegmento;
                _indices[k] = v;
                _indices[k + 1] = v + 1;
                _indices[k + 2] = v + 2;
                _indices[k + 3] = v + 2;
                _indices[k + 4] = v + 1;
                _indices[k + 5] = v + 3;
            }

            int totalVertices = cantidad * VerticesPorMuestra;
            _malla.SetVertices(_vertices, 0, totalVertices);
            _malla.SetUVs(0, _uvs, 0, totalVertices);
            _malla.SetColors(_colores, 0, totalVertices);
            _malla.SetTriangles(_indices, 0, segmentos * IndicesPorSegmento, 0);
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
