using UnityEngine;

namespace TDOM.Gameplay.Combat
{
    /// <summary>
    /// Aproxima la silueta del jefe, vista desde arriba, con una elipse y calcula dónde la
    /// cruza un rayo que sale desde adentro. Sirve para que el origen de la Mano nazca en la
    /// orilla del modelo, en la dirección del ataque.
    /// </summary>
    public static class BordeSilueta
    {
        /// <summary>
        /// Distancia desde <paramref name="origen"/> (dentro de la elipse) hasta el borde, en la
        /// dirección <paramref name="direccion"/>. Devuelve <paramref name="respaldo"/> si la
        /// elipse no es válida, la dirección es nula o el origen queda fuera de la silueta.
        /// </summary>
        public static float Distancia(
            Vector2 origen,
            Vector2 centro,
            Vector2 semiejes,
            Vector2 direccion,
            float respaldo
        )
        {
            if (semiejes.x <= 0f || semiejes.y <= 0f || direccion.sqrMagnitude < 1e-8f)
                return respaldo;

            Vector2 d = direccion.normalized;
            var q = new Vector2(
                (origen.x - centro.x) / semiejes.x,
                (origen.y - centro.y) / semiejes.y
            );
            var u = new Vector2(d.x / semiejes.x, d.y / semiejes.y);

            float uu = Vector2.Dot(u, u);
            float qu = Vector2.Dot(q, u);
            float qq = Vector2.Dot(q, q);
            if (qq > 1f)
                return respaldo;

            float discriminante = qu * qu - uu * (qq - 1f);
            if (discriminante < 0f)
                return respaldo;

            return (-qu + Mathf.Sqrt(discriminante)) / uu;
        }
    }
}
