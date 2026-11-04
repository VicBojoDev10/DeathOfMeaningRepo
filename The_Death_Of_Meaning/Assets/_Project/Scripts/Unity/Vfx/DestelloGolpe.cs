using System.Collections.Generic;
using TDOM.Gameplay.Vfx;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public class DestelloGolpe : MonoBehaviour
    {
        private static readonly int ColorGolpeId = Shader.PropertyToID("_ColorGolpe");
        private static readonly int IntensidadGolpeId = Shader.PropertyToID("_IntensidadGolpe");
        private static readonly int[] ColoresBaseIds =
        {
            Shader.PropertyToID("_BaseColor"),
            Shader.PropertyToID("_Color"),
        };

        private readonly struct Tinte
        {
            public Renderer Renderer { get; }
            public int PropiedadId { get; }
            public Color Original { get; }

            public Tinte(Renderer renderer, int propiedadId, Color original)
            {
                Renderer = renderer;
                PropiedadId = propiedadId;
                Original = original;
            }
        }

        [SerializeField]
        private float _duracion = 0.3f;

        [SerializeField]
        private AnimationCurve _curva = new AnimationCurve(
            new Keyframe(0f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -0.4f, -0.4f)
        );

        [SerializeField]
        [ColorUsage(false, true)]
        private Color _colorGolpe = new Color(4f, 0.4f, 0.2f, 1f);

        [SerializeField]
        [Range(0f, 1f)]
        private float _intensidadMaxima = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _mezclaTinteSinShader = 0.6f;

        [SerializeField]
        private Renderer[] _renderers;

        private readonly List<Renderer> _conShaderGolpe = new List<Renderer>();
        private readonly List<Tinte> _tintes = new List<Tinte>();
        private CurvaDestelloGolpe _destello;
        private MaterialPropertyBlock _bloque;

        private void Awake()
        {
            _destello = new CurvaDestelloGolpe(_duracion);
            _bloque = new MaterialPropertyBlock();
            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Clasificar();
            enabled = false;
        }

        private void Clasificar()
        {
            foreach (Renderer renderer in _renderers)
            {
                Material material = renderer != null ? renderer.sharedMaterial : null;
                if (material == null)
                    continue;

                if (material.HasProperty(IntensidadGolpeId))
                {
                    _conShaderGolpe.Add(renderer);
                    continue;
                }

                foreach (int propiedadId in ColoresBaseIds)
                {
                    if (!material.HasProperty(propiedadId))
                        continue;

                    _tintes.Add(new Tinte(renderer, propiedadId, material.GetColor(propiedadId)));
                    break;
                }
            }
        }

        public void Disparar()
        {
            if (_destello == null)
                return;

            _destello.Reiniciar();
            enabled = true;
        }

        private void Update()
        {
            float intensidad = _destello.Avanzar(
                Time.deltaTime,
                _curva.Evaluate,
                _intensidadMaxima
            );
            Aplicar(intensidad);
            if (!_destello.Activo)
                enabled = false;
        }

        private void OnDisable()
        {
            if (_destello != null && _destello.Activo)
                Aplicar(0f);
        }

        private void Aplicar(float intensidad)
        {
            foreach (Renderer renderer in _conShaderGolpe)
            {
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(_bloque);
                _bloque.SetColor(ColorGolpeId, _colorGolpe);
                _bloque.SetFloat(IntensidadGolpeId, intensidad);
                renderer.SetPropertyBlock(_bloque);
            }

            float mezcla =
                _intensidadMaxima > 0f
                    ? intensidad / _intensidadMaxima * _mezclaTinteSinShader
                    : 0f;
            Color colorTinte =
                _colorGolpe.maxColorComponent > 1f
                    ? _colorGolpe / _colorGolpe.maxColorComponent
                    : _colorGolpe;
            foreach (Tinte tinte in _tintes)
            {
                if (tinte.Renderer == null)
                    continue;

                tinte.Renderer.GetPropertyBlock(_bloque);
                _bloque.SetColor(tinte.PropiedadId, Color.Lerp(tinte.Original, colorTinte, mezcla));
                tinte.Renderer.SetPropertyBlock(_bloque);
            }
        }
    }
}
