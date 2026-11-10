using UnityEngine;

namespace TDOM.POCO.ScriptableObjects
{
    [CreateAssetMenu(menuName = "TDOM/Boss/Reacción a Rush", fileName = "ReaccionRushProfile")]
    public sealed class ReaccionRushProfile : ScriptableObject
    {
        [Min(0.1f)]
        public float ventanaSegundos = 5f;

        [Range(0.01f, 1f)]
        public float porcentajeVida = 0.1f;

        [Min(0f)]
        public float radio = 8f;

        [Min(0f)]
        public float distanciaEmpuje = 4f;

        [Min(0f)]
        public float alturaEmpuje = 1f;

        [Min(0.01f)]
        public float duracionEmpuje = 0.45f;

        [Min(0f)]
        public float danio = 0f;

        [Min(0f)]
        public float cooldown = 8f;

        [Min(0.1f)]
        public float duracionReaccion = 1.2f;
        public string triggerAnimator = "ReaccionRush";
    }
}
