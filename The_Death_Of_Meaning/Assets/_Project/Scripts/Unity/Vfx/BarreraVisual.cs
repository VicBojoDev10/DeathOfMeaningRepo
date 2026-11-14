using TDOM.Data;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    /// <summary>Panel curvo frontal de la barrera de Zendre, renderizado con el shader VfxBase.</summary>
    public sealed class BarreraVisual : MonoBehaviour
    {
        private const int Segmentos = 28;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int IntensidadId = Shader.PropertyToID("_Intensidad");
        private static readonly int DisolucionId = Shader.PropertyToID("_Disolucion");

        private Transform _panel;
        private MeshRenderer _renderer;
        private Mesh _mesh;
        private MaterialPropertyBlock _bloque;
        private BarrierProfile _perfil;
        private bool _inicializada;

        public void Configurar(BarrierProfile perfil)
        {
            if (_inicializada || perfil == null || perfil.VisualMaterial == null)
                return;

            _perfil = perfil;
            _inicializada = true;
            var panel = new GameObject("Barrera frontal");
            _panel = panel.transform;
            _panel.SetParent(transform, false);
            _panel.localPosition = Vector3.zero;
            _panel.localRotation = Quaternion.identity;

            var filtro = panel.AddComponent<MeshFilter>();
            _renderer = panel.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = perfil.VisualMaterial;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            _mesh = CrearMalla(perfil);
            filtro.sharedMesh = _mesh;
            _bloque = new MaterialPropertyBlock();
            _bloque.SetColor(ColorId, perfil.VisualColor);
            _bloque.SetFloat(IntensidadId, perfil.VisualIntensity);
            _bloque.SetFloat(DisolucionId, 0f);
            _renderer.SetPropertyBlock(_bloque);
            _panel.gameObject.SetActive(false);
        }

        public void Mostrar(bool mostrar)
        {
            if (_panel != null && _panel.gameObject.activeSelf != mostrar)
                _panel.gameObject.SetActive(mostrar);
        }

        private void Update()
        {
            if (_renderer == null || _bloque == null || !_panel.gameObject.activeSelf)
                return;

            _bloque.SetFloat(
                IntensidadId,
                _perfil.VisualIntensity
                    + Mathf.Sin(Time.time * _perfil.VisualPulseSpeed) * _perfil.VisualPulseAmplitude
            );
            _renderer.SetPropertyBlock(_bloque);
        }

        private static Mesh CrearMalla(BarrierProfile perfil)
        {
            float radio = perfil.VisualRadius;
            float baseY = perfil.VisualBaseHeight;
            float altura = perfil.VisualHeight;
            float profundidadCurva = perfil.VisualCurveDepth;
            float offsetZ = perfil.VisualForwardOffset;
            float medioArco = Mathf.Clamp(perfil.FrontArcDegrees, 40f, 170f) * 0.5f;

            var vertices = new Vector3[(Segmentos + 1) * 2];
            var uv = new Vector2[vertices.Length];
            var triangulos = new int[Segmentos * 12];
            for (int i = 0; i <= Segmentos; i++)
            {
                float t = (float)i / Segmentos;
                float angulo = Mathf.Lerp(-medioArco, medioArco, t) * Mathf.Deg2Rad;
                float z = offsetZ + profundidadCurva * Mathf.Cos(angulo);
                float x = Mathf.Sin(angulo) * radio;
                vertices[i * 2] = new Vector3(x, baseY, z);
                vertices[i * 2 + 1] = new Vector3(x, baseY + altura, z);
                uv[i * 2] = new Vector2(t, 0f);
                uv[i * 2 + 1] = new Vector2(t, 1f);

                if (i == Segmentos)
                    continue;
                int v = i * 2;
                int tri = i * 12;
                triangulos[tri] = v;
                triangulos[tri + 1] = v + 1;
                triangulos[tri + 2] = v + 2;
                triangulos[tri + 3] = v + 1;
                triangulos[tri + 4] = v + 3;
                triangulos[tri + 5] = v + 2;
                triangulos[tri + 6] = v + 2;
                triangulos[tri + 7] = v + 1;
                triangulos[tri + 8] = v;
                triangulos[tri + 9] = v + 2;
                triangulos[tri + 10] = v + 3;
                triangulos[tri + 11] = v + 1;
            }

            var mesh = new Mesh { name = "Malla barrera curva Zendre" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangulos;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);
        }
    }
}
