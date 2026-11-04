using TDOM.Gameplay.Vfx;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public class VfxExplosionSuelo : VfxExplosion
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        [SerializeField]
        private ParticleSystem _polvo;

        [SerializeField]
        private LayerMask _capasSuelo = ~0;

        [SerializeField]
        private float _alturaRayo = 0.5f;

        [SerializeField]
        private float _distanciaRayo = 1.5f;

        [SerializeField]
        private Color _colorSinSuelo = new Color(0.6f, 0.6f, 0.6f, 1f);

        [SerializeField]
        [Range(0f, 1f)]
        private float _aclarado = 0.15f;

        protected override void AlIniciar()
        {
            if (_polvo == null)
                return;

            bool haySuelo = LeerColorSuelo(out Color colorSuelo);
            ParticleSystem.MainModule principal = _polvo.main;
            principal.startColor = ColorPolvoSuelo.Calcular(
                haySuelo,
                colorSuelo,
                _colorSinSuelo,
                _aclarado
            );
        }

        private bool LeerColorSuelo(out Color color)
        {
            color = _colorSinSuelo;
            Vector3 origen = transform.position + Vector3.up * _alturaRayo;
            if (
                !Physics.Raycast(
                    origen,
                    Vector3.down,
                    out RaycastHit impacto,
                    _alturaRayo + _distanciaRayo,
                    _capasSuelo,
                    QueryTriggerInteraction.Ignore
                )
            )
                return false;

            Renderer render = impacto.collider.GetComponentInParent<Renderer>();
            Material material = render != null ? render.sharedMaterial : null;
            if (material == null)
                return false;

            color = ColorDeMaterial(material) * ColorDeTextura(material, impacto);
            return true;
        }

        private static Color ColorDeMaterial(Material material)
        {
            if (material.HasProperty(BaseColorId))
                return material.GetColor(BaseColorId);

            return material.HasProperty(ColorId) ? material.GetColor(ColorId) : Color.white;
        }

        private static Color ColorDeTextura(Material material, RaycastHit impacto)
        {
            if (!(impacto.collider is MeshCollider) || !material.HasProperty(BaseMapId))
                return Color.white;

            Texture2D textura = material.GetTexture(BaseMapId) as Texture2D;
            if (textura == null || !textura.isReadable)
                return Color.white;

            return textura.GetPixelBilinear(impacto.textureCoord.x, impacto.textureCoord.y);
        }
    }
}
