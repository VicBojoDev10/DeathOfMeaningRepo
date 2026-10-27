using UnityEngine;

namespace TDOM.Gameplay.Combat
{
    /// <summary>
    /// Medidas de la Mano de AREK que no dependen de Unity: el grosor visual del brazo y la
    /// banda vertical de daño.
    /// </summary>
    public static class GeometriaManotazo
    {
        /// <summary>Ancho natural de TercerBrazo_Low entre su largo: 2.3 m / 12.2 m.</summary>
        public const float ProporcionAncho = 0.1926f;

        /// <summary>Alto natural del brazo entre su largo: 4.2 m / 12.2 m.</summary>
        public const float ProporcionAlto = 0.3488f;

        /// <summary>
        /// Grosor visual del brazo para un largo dado: proporcional al largo (como el modelo
        /// natural, por el multiplicador) y sin pasar de <paramref name="maximo"/>. Crece desde 0
        /// junto con el largo, sin saltos.
        /// </summary>
        public static float Grosor(float largo, float proporcion, float multiplicador, float maximo)
        {
            float grosor = Mathf.Max(0f, largo) * proporcion * Mathf.Max(0f, multiplicador);
            return Mathf.Min(grosor, Mathf.Max(0f, maximo));
        }

        /// <summary>
        /// Banda vertical de daño respecto a la raíz del ataque (que está alturaOrigen sobre el
        /// piso). A ras del piso es la banda de siempre (del piso hasta alto); si no, una pared
        /// del piso hasta alto sobre la raíz. Devuelve el centro y la mitad de la altura.
        /// </summary>
        public static (float Centro, float Mitad) BandaDano(
            float alto,
            float alturaOrigen,
            bool alPiso
        )
        {
            float abajo = -Mathf.Max(0f, alturaOrigen);
            float arriba = alPiso ? abajo + alto : alto;
            return ((abajo + arriba) * 0.5f, (arriba - abajo) * 0.5f);
        }
    }
}
