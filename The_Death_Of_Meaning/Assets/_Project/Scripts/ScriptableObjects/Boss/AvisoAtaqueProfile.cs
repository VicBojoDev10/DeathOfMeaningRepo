using UnityEngine;

namespace TDOM.Data
{
    /// <summary>
    /// Tuning del parpadeo de las áreas de aviso de AREK (AvisoParpadeo + shader AvisoAtaque).
    /// Se asigna una vez en AtaquesArek (BossArek.prefab) y lo comparten todos los ataques.
    /// </summary>
    [CreateAssetMenu(fileName = "AvisoAtaqueProfile", menuName = "TDOM/Boss/Aviso Ataque Profile")]
    public sealed class AvisoAtaqueProfile : ScriptableObject
    {
        [Header("Color")]
        public Color colorBase = new Color(1f, 0.1f, 0.05f, 0.6f);

        [Tooltip("Multiplica el color; arriba de 1 activa el bloom si el post-proceso lo tiene.")]
        public float intensidad = 4f;

        [Header("Pulso")]
        [Tooltip("Hz al empezar el aviso.")]
        public float frecuenciaInicial = 2f;

        [Tooltip("Hz al terminar el aviso.")]
        public float frecuenciaFinal = 10f;

        [Tooltip("Brillo en el punto bajo del pulso (0 = se apaga, 1 = no parpadea).")]
        [Range(0f, 1f)]
        public float brilloMinimo = 0.15f;

        [Tooltip("Segundos finales del aviso en los que el área queda encendida fija al máximo.")]
        public float tramoFijoFinal = 0.2f;

        [Header("Borde")]
        [Tooltip("Ancho del borde brillante, como fracción del área (0 a 0.5).")]
        [Range(0.01f, 0.5f)]
        public float anchoBorde = 0.12f;

        [Tooltip("Brillo del centro respecto al borde.")]
        [Range(0f, 1f)]
        public float brilloCentro = 0.35f;
    }
}
