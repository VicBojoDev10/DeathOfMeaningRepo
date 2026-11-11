using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(
        fileName = "SniperMinionProfile",
        menuName = "TDOM/Boss/Sniper Minion Profile"
    )]
    public sealed class SniperMinionProfile : ScriptableObject
    {
        [Header("Stats (Confluence: vida 100, daño 15, disparo)")]
        [Min(1f)]
        public float vida = 100f;

        [Min(0f)]
        public float danio = 15f;

        [Min(.5f)]
        public float cadenciaDisparo = 4;

        [Header("Movimiento")]
        [Min(0f)]
        public float velocidad = 3.5f;

        [Tooltip(
            "Radio máximo de movimiento desde el punto donde apareció, dentro de su plataforma."
        )]
        [Min(0.2f)]
        public float radioMovimiento = 0.8f;

        [Tooltip("Frecuencia con la que ajusta su posición hacia el jugador.")]
        [Min(0.2f)]
        public float intervaloMovimiento = 2f;

        [Header("Ataque")]
        [Tooltip("Distancia en XZ a la que deja de perseguir y empieza el combo.")]
        [Min(0.1f)]
        public float rangoAtaque = 1.6f;

        [Min(0.01f)]
        public float duracionAviso = 0.4f;

        [Min(0f)]
        public float duracionRecuperar = 1f;

        [Tooltip("Tamaño de la caja del golpe, enfrente del minion.")]
        public Vector3 tamanioGolpe = new Vector3(1.2f, 1.2f, 1.5f);

        [Header("Aviso visual")]
        public Color colorAviso = Color.red;

        [Tooltip("Grados que el cubo se inclina hacia delante durante el aviso.")]
        public float inclinacionAviso = 20f;

        [Header("Spawn")]
        [Tooltip("Radio del círculo alrededor de la plataforma donde aparecen los minions.")]
        [Min(0f)]
        public float radioSpawn = 0.8f;

        [Tooltip(
            "Si no hay NavMesh en el radio base, se prueba esta distancia más lejos cada vez."
        )]
        [Min(0.1f)]
        public float pasoRadioSpawn = 2f;

        [Tooltip("Cuántos radios se prueban por minion antes de saltarlo.")]
        [Min(1)]
        public int intentosSpawn = 5;

        [Header("Muerte")]
        [Tooltip("Segundos que tarda el cubo en encogerse antes del Despawn.")]
        [Min(0f)]
        public float duracionMuerte = 0.5f;
    }
}
