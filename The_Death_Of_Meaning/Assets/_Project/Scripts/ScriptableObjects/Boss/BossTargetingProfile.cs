using UnityEngine;

namespace TDOM.Data
{
    /// <summary>
    /// Cómo elige AREK a quién atacar. Solo considera jugadores dentro de radioDeteccion y
    /// dentro del rango del ataque que toca en el patrón.
    /// </summary>
    [CreateAssetMenu(fileName = "BossTargetingProfile", menuName = "TDOM/Boss/Targeting Profile")]
    public sealed class BossTargetingProfile : ScriptableObject
    {
        [Tooltip("Distancia (XZ) a la que el jefe detecta jugadores.")]
        public float radioDeteccion = 60f;

        [Tooltip(
            "Segundos hacia atrás que cuenta el daño hecho al jefe para decidir quién ataca más."
        )]
        public float ventanaAgro = 5f;

        [Tooltip(
            "Un jugador ataca 'notablemente más' si su daño reciente es al menos este factor por el del siguiente."
        )]
        public float factorAgroNotable = 1.5f;

        [Tooltip("Grados por segundo que gira el jefe en Y hacia su objetivo.")]
        public float velocidadGiro = 90f;
    }
}
