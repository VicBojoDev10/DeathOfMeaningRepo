using UnityEngine;

namespace TDOM.Data
{
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

        [Header("Bloqueo de movimiento durante los ataques")]
        [Tooltip(
            "Segundos que el jefe espera, tras terminar el flujo completo de un ataque, antes de volver a rotar (fase 1) o trasladarse (fase 2 en adelante). Durante el ataque no se mueve."
        )]
        [Min(0f)]
        public float esperaTrasAtaque = 0.5f;

        [Tooltip(
            "Seguridad: si un ataque lleva este tiempo sin terminar (se destruyó o nunca avisó de su fin), el jefe se libera igualmente. Debe ser mayor que el ataque más largo (hoy ~2.6 s). 0 desactiva el límite."
        )]
        [Min(0f)]
        public float tiempoMaximoAtaque = 15f;
    }
}
