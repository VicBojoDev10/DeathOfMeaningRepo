using TDOM.Contracts;
using UnityEngine;

namespace TDOM.Data
{
    /// <summary>
    /// Datos de un power-up (TW-276). Un solo prefab de pickup sirve para todos: lo que cambia
    /// es este perfil. Se registra en un PowerUpCatalogo; el pickup viaja por red con su índice.
    /// </summary>
    [CreateAssetMenu(fileName = "PowerUpProfile", menuName = "TDOM/PowerUps/PowerUp Profile")]
    public sealed class PowerUpProfile : ScriptableObject
    {
        public string Nombre = "Power-up";

        [Tooltip(
            "Potenciador = beneficio, Afectacion = efecto negativo. Hoy solo define el color."
        )]
        public NaturalezaPowerUp Naturaleza = NaturalezaPowerUp.Potenciador;

        [Tooltip("QuienLoToca = solo el jugador que lo toca. AmbosJugadores = todos los vivos.")]
        public ModoAlcancePowerUp Alcance = ModoAlcancePowerUp.QuienLoToca;

        public TipoEfectoPowerUp Efecto = TipoEfectoPowerUp.Curar;

        [Tooltip("Cantidad del efecto (puntos de vida para Curar).")]
        [Min(0f)]
        public float Magnitud = 15f;

        [Tooltip(
            "Segundos que dura el efecto. 0 = instantáneo. Los efectos con duración los implementan tickets posteriores."
        )]
        [Min(0f)]
        public float DuracionEfecto = 0f;

        [Tooltip("Segundos que el orbe espera en el suelo antes de desaparecer.")]
        [Min(0.1f)]
        public float DuracionEnSuelo = 15f;

        [Tooltip("Radio del trigger de recogida, en metros.")]
        [Min(0.1f)]
        public float RadioRecogida = 1.2f;

        [Header("Visual (greybox)")]
        public Color Color = Color.green;

        [Tooltip("Diámetro del orbe, en metros.")]
        [Min(0.05f)]
        public float EscalaVisual = 0.5f;
    }
}
