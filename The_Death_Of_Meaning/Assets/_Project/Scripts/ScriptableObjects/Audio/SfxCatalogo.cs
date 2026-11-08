using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "SfxCatalogo", menuName = "TDOM/Audio/Sfx Catalogo")]
    public sealed class SfxCatalogo : ScriptableObject
    {
        // El índice de cada banco viaja por red: no reordenar ni borrar, solo agregar al final.
        [Tooltip("No reordenar ni borrar bancos: el índice viaja por red. Solo agregar al final")]
        public SfxBanco[] bancos = new SfxBanco[0];
    }
}
