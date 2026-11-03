using System.Collections.Generic;
using UnityEngine;

namespace TDOM.Data
{
    /// <summary>
    /// Lista de todos los power-ups del juego. El pickup viaja por red con el índice de su perfil
    /// en esta lista (un byte), no con el perfil. El orden no debe cambiar entre builds.
    /// </summary>
    [CreateAssetMenu(fileName = "PowerUpCatalogo", menuName = "TDOM/PowerUps/PowerUp Catalogo")]
    public sealed class PowerUpCatalogo : ScriptableObject
    {
        [SerializeField]
        private PowerUpProfile[] _perfiles = new PowerUpProfile[0];

        public int Cantidad => _perfiles != null ? _perfiles.Length : 0;

        public IReadOnlyList<PowerUpProfile> Perfiles => _perfiles;

        /// <summary>Perfil en ese índice, o null si el índice no existe.</summary>
        public PowerUpProfile Obtener(int indice) =>
            _perfiles != null && indice >= 0 && indice < _perfiles.Length
                ? _perfiles[indice]
                : null;

        /// <summary>Índice del perfil en el catálogo, o -1 si no está (o si no cabe en un byte).</summary>
        public int IndiceDe(PowerUpProfile perfil)
        {
            if (perfil == null || _perfiles == null)
                return -1;

            int limite = System.Math.Min(_perfiles.Length, byte.MaxValue + 1);
            for (int i = 0; i < limite; i++)
            {
                if (_perfiles[i] == perfil)
                    return i;
            }
            return -1;
        }
    }
}
