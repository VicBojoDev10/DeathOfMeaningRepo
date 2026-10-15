using UnityEngine;

namespace TDOM.Data
{
    /// <summary>
    /// Base de los perfiles de tuning de los ataques de AREK. Se asignan en AtaquesArek
    /// (BossArek.prefab) y cada ataque lee el suyo al spawnear, en servidor y clientes.
    /// </summary>
    public abstract class AtaqueArekProfile : ScriptableObject
    {
        /// <summary>Distancia máxima (en XZ desde el jefe) a la que este ataque alcanza a un jugador.</summary>
        public abstract float Rango { get; }
    }
}
