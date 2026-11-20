using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "AtaqueRayoVfxProfile", menuName = "TDOM/Vfx/Ataque Rayo Profile")]
    public sealed class AtaqueRayoVfxProfile : ScriptableObject
    {
        [Header("Materiales y colores")]
        [Tooltip("Material y color del núcleo del rayo.")]
        public Material materialNucleo;
        public Color colorNucleo = new Color(0.45f, 0.85f, 1f, 1f);

        [Tooltip("Segundo material y color, usado para el halo exterior.")]
        public Material materialHalo;
        public Color colorHalo = new Color(1f, 0.55f, 0.12f, 0.85f);

        [Header("Tamaño")]
        [Tooltip("Desplazamiento vertical aplicado a todos los visuales del rayo, en unidades de mundo.")]
        public float desplazamientoVisualY;
        [Min(0f)] public float anchoNucleo = 0.026f;
        [Min(0f)] public float anchoHalo = 0.13f;
        [Min(0f)] public float amplitudOndulacion = 0.12f;

        [Header("Geometría para shaders de MeshRenderer")]
        [Tooltip("Material AllIn13DShader opcional sobre una cinta mesh que sigue el rayo.")]
        public Material materialMesh;
        [Min(0f)] public float anchoMesh = 0.35f;
        [Tooltip("Multiplicador de la forma ondulada; 0 produce un rayo recto.")]
        [Min(0f)] public float formaOndulada = 1f;
        [Min(0.1f)] public float frecuenciaForma = 1f;
        [Min(0f)] public float ondulacionVertical = 0.7f;

        [Header("Partículas")]
        [Tooltip("Prefab opcional que se instancia al final de cada rayo al activarse.")]
        public ParticleSystem particulasImpacto;
        [Min(0f)] public float escalaParticulas = 1f;
    }
}
