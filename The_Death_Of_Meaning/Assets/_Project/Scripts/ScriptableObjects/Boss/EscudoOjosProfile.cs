using UnityEngine;

namespace TDOM.POCO.ScriptableObjects
{
    /// <summary>
    /// Aspecto de los escudos de los ojos del jefe (fases 2 a 5). Todas las medidas son relativas
    /// al tamaño del collider de cada ojo, así que no dependen de la escala del modelo.
    /// </summary>
    [CreateAssetMenu(fileName = "EscudoOjosProfile", menuName = "TDOM/Boss/EscudoOjosProfile")]
    public class EscudoOjosProfile : ScriptableObject
    {
        [Header("Escudo azul (lo rompe Zendre) y rojo (lo rompe Ayla)")]
        public Material MaterialAzul;

        public Material MaterialRojo;

        [Header("Letra al frente del escudo (símbolos fijos de la animación de la fase 4)")]
        public Material LetraAzul;

        public Material LetraRoja;

        [Header("Forma")]
        [Tooltip("Diámetro del escudo en diámetros del ojo.")]
        [Min(0.1f)]
        public float EscalaEscudo = 1.6f;

        [Tooltip("Grosor del escudo respecto a su diámetro (1 = esfera, menor = disco).")]
        [Range(0.05f, 1f)]
        public float GrosorEscudo = 0.3f;

        [Tooltip("Cuánto se adelanta el escudo del centro del ojo, en radios del ojo.")]
        public float DistanciaAlOjo = 0.9f;

        [Tooltip("Separación entre capas cuando un ojo tiene varias (fase 5), en radios del ojo.")]
        [Min(0f)]
        public float SeparacionCapas = 0.7f;

        [Tooltip("Cada capa más cerca del ojo mide esta fracción de la que tiene delante (1 = igual).")]
        [Range(0.3f, 1f)]
        public float EscalaPorCapaInterior = 0.85f;

        [Tooltip("Tamaño de la letra en diámetros del ojo.")]
        [Min(0.05f)]
        public float TamanoLetra = 1f;

        [Tooltip("Cuánto se adelanta la letra del escudo, en radios del ojo.")]
        public float DistanciaLetra = 0.7f;

        [Header("Rotura")]
        [Min(0.01f)]
        public float DuracionRotura = 0.3f;

        [Min(1f)]
        public float EscalaRotura = 1.4f;
    }
}
