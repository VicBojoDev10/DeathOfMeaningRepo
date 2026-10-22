using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(
        fileName = "AtaqueMorteroProfile",
        menuName = "TDOM/Boss/Ataque Mortero Profile"
    )]
    public sealed class AtaqueMorteroProfile : AtaqueArekProfile
    {
        [Header("Tiempos")]
        public float aviso = 1f;
        public float caida = 0.3f;
        public float retirada = 0.2f;

        [Header("Disparos")]
        public int disparos = 3;
        public float radioArea = 10f;
        public float radioArena = 25f;
        public int intentosPorPunto = 30;
        public float alturaCaida = 15f;
        public float altoPiedra = 6f;

        [Header("Daño")]
        public float dano = 9999f;
        public bool esInstaKill = true;

        // TW-126: cada piedra se queda como columna en el punto de impacto.
        [Header("Columnas")]
        public float alturaColumnaMin = 3f;
        public float alturaColumnaMax = 6f;
        public float diametroColumna = 2.5f;
        public int maxColumnas = 9;

        [Tooltip(
            "Ataques de Arek (de cualquier tipo, contando el Mortero que la creó) tras los que la columna se hunde."
        )]
        public int ataquesDeVidaColumna = 2;
        public float duracionAscenso = 0.2f;
        public float duracionHundimiento = 0.5f;
        public bool limpiarColumnasAlCambiarFase = false;

        [Tooltip("Distancia extra al borde al sacar a un jugador atrapado en la columna.")]
        public float margenSacarJugador = 0.3f;

        [Header("Efectos de impacto")]
        public float sacudidaMaxima = 1f;
        public float radioSacudida = 30f;
        public float diametroGrieta = 4f;

        public override float Rango => radioArena;

        public override float Dano => dano;

        public override bool EsInstaKill => esInstaKill;
    }
}
