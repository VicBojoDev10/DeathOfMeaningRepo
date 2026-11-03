using UnityEngine;

namespace TDOM.Data
{
    /// <summary>
    /// Tuning de los minions de AREK (TW-125). Lo leen MinionController (en el prefab del
    /// minion) y MinionSpawner (en BossArek.prefab).
    /// </summary>
    [CreateAssetMenu(fileName = "MinionProfile", menuName = "TDOM/Boss/Minion Profile")]
    public sealed class MinionProfile : ScriptableObject
    {
        [Header("Stats (Confluence: vida 100, daño 15, combo B-B)")]
        [Min(1f)]
        public float vida = 100f;

        // Por ahora solo se reporta en el log: no hay sistema de vida en los jugadores.
        [Min(0f)]
        public float danio = 15f;

        [Min(1)]
        public int golpesPorCombo = 2;

        [Header("Movimiento")]
        [Min(0f)]
        public float velocidad = 3.5f;

        [Header("Ataque")]
        [Tooltip("Distancia en XZ a la que deja de perseguir y empieza el combo.")]
        [Min(0.1f)]
        public float rangoAtaque = 1.6f;

        [Min(0.01f)]
        public float duracionAviso = 0.4f;

        [Min(0f)]
        public float pausaEntreGolpes = 0.25f;

        [Min(0f)]
        public float duracionRecuperar = 1f;

        [Tooltip("Tamaño de la caja del golpe, enfrente del minion.")]
        public Vector3 tamanioGolpe = new Vector3(1.2f, 1.2f, 1.5f);

        [Header("Aviso visual")]
        public Color colorAviso = Color.red;

        [Tooltip("Grados que el cubo se inclina hacia delante durante el aviso.")]
        public float inclinacionAviso = 20f;

        [Header("Spawn")]
        [Tooltip("Radio del círculo alrededor del jefe donde aparecen los minions.")]
        [Min(0f)]
        public float radioSpawn = 8f;

        [Tooltip(
            "Distancia máxima para buscar un punto del NavMesh cerca de cada punto del círculo."
        )]
        [Min(0.1f)]
        public float distanciaBusquedaNavMesh = 4f;

        [Tooltip(
            "Distancia hacia abajo para buscar el piso desde la altura del jefe (su pivote puede quedar lejos del piso)."
        )]
        [Min(0f)]
        public float alturaBusquedaPiso = 60f;

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
