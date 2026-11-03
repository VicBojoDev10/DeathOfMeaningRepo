using System;
using UnityEngine;

namespace TDOM.Data
{
    /// <summary>Un power-up posible dentro de una tabla de drops, con su peso relativo.</summary>
    [Serializable]
    public struct EntradaDropPowerUp
    {
        public PowerUpProfile Perfil;

        [Min(0f)]
        public float Peso;
    }

    /// <summary>
    /// Tabla de drops reutilizable (TW-276): qué power-ups puede soltar un enemigo, con qué
    /// probabilidad y con qué límites. La usan los minions de AREK y, después, cualquier otro
    /// enemigo o el jefe.
    /// </summary>
    [CreateAssetMenu(fileName = "TablaDrop", menuName = "TDOM/PowerUps/Tabla Drop")]
    public sealed class TablaDropProfile : ScriptableObject
    {
        public EntradaDropPowerUp[] Entradas = new EntradaDropPowerUp[0];

        [Tooltip("Probabilidad (0 a 1) de soltar un power-up en cada muerte.")]
        [Range(0f, 1f)]
        public float ProbabilidadDrop = 0.3f;

        [Tooltip("Tras estas muertes seguidas sin drop, la siguiente suelta seguro.")]
        [Min(1)]
        public int MuertesParaGarantizado = 4;

        [Tooltip("Máximo de pickups de esta tabla vivos a la vez; con ese tope no suelta más.")]
        [Min(1)]
        public int MaxVivos = 4;
    }
}
