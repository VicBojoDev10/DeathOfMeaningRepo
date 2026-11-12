using System;
using System.Collections.Generic;
using UnityEngine;

namespace TDOM.Unity.UI
{
    /// <summary>
    /// Render-only silhouette layer for a downed player. The duplicate skinned meshes live on
    /// Ignore Raycast so the regular camera skips their surface pass; All In 1's OutlinePass
    /// renderer feature still sees them and draws the contour.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DownedPlayerOutline : MonoBehaviour
    {
        private const int OutlineLayer = 2;
        private const string OutlineShaderName = "AllIn13DShader/AllIn13DShaderOutline";
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineThicknessId = Shader.PropertyToID("_OutlineThickness");
        private readonly List<Layer> _layers = new();
        private Material _material;
        private bool _visible;

        private sealed class Layer
        {
            public Renderer Source;
            public Renderer Duplicate;
        }

        private void Awake()
        {
            Shader shader = Shader.Find(OutlineShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[DownedPlayerOutline] No se encontró el shader {OutlineShaderName}.", this);
                enabled = false;
                return;
            }

            _material = new Material(shader)
            {
                name = "DownedPlayerOutline (Runtime)",
                renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry,
            };
            _material.EnableKeyword("_OUTLINETYPE_SIMPLE");
            _material.SetFloat("_OutlineType", 1f);
            _material.SetFloat(OutlineThicknessId, 2.5f);
            _material.SetColor(OutlineColorId, new Color(1f, 0.34f, 0.08f, 1f));
            CrearCapas();
            AplicarVisibilidad(false);
        }

        public void SetVisible(bool visible)
        {
            if (_visible == visible)
                return;
            _visible = visible;
            AplicarVisibilidad(visible);
        }

        private void CrearCapas()
        {
            foreach (Renderer source in GetComponentsInChildren<Renderer>(true))
            {
                if (source.transform == transform)
                    continue;
                var go = new GameObject($"{source.name}_DownedOutline") { layer = OutlineLayer };
                go.transform.SetParent(source.transform.parent, false);
                CopiarTransform(source.transform, go.transform);
                Renderer duplicate;
                if (source is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                {
                    var duplicateSkinned = go.AddComponent<SkinnedMeshRenderer>();
                    duplicateSkinned.sharedMesh = skinned.sharedMesh;
                    duplicateSkinned.bones = skinned.bones;
                    duplicateSkinned.rootBone = skinned.rootBone;
                    duplicateSkinned.localBounds = skinned.localBounds;
                    duplicateSkinned.quality = skinned.quality;
                    duplicateSkinned.updateWhenOffscreen = skinned.updateWhenOffscreen;
                    duplicate = duplicateSkinned;
                }
                else if (source is MeshRenderer meshRenderer && meshRenderer.TryGetComponent(out MeshFilter sourceFilter) && sourceFilter.sharedMesh != null)
                {
                    go.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
                    duplicate = go.AddComponent<MeshRenderer>();
                }
                else
                {
                    Destroy(go);
                    continue;
                }
                duplicate.sharedMaterials = CrearMateriales(source.sharedMaterials.Length);
                duplicate.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                duplicate.receiveShadows = false;
                duplicate.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                duplicate.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                _layers.Add(new Layer { Source = source, Duplicate = duplicate });
            }
        }

        private Material[] CrearMateriales(int cantidad)
        {
            var materiales = new Material[Math.Max(1, cantidad)];
            for (int i = 0; i < materiales.Length; i++)
                materiales[i] = _material;
            return materiales;
        }

        private static void CopiarTransform(Transform source, Transform target)
        {
            target.localPosition = source.localPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }

        private void LateUpdate()
        {
            foreach (Layer layer in _layers)
            {
                if (layer.Source == null || layer.Duplicate == null)
                    continue;
                CopiarTransform(layer.Source.transform, layer.Duplicate.transform);
                layer.Duplicate.enabled = _visible && layer.Source.enabled && layer.Source.gameObject.activeInHierarchy;
            }
        }

        private void AplicarVisibilidad(bool visible)
        {
            foreach (Layer layer in _layers)
                if (layer.Duplicate != null)
                    layer.Duplicate.enabled = visible && layer.Source != null && layer.Source.enabled;
        }

        private void OnDestroy()
        {
            foreach (Layer layer in _layers)
                if (layer.Duplicate != null)
                    Destroy(layer.Duplicate.gameObject);
            if (_material != null)
                Destroy(_material);
        }
    }
}
