using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "SfxCatalogo", menuName = "TDOM/Audio/Sfx Catalogo")]
    public sealed class SfxCatalogo : ScriptableObject
    {
        [Tooltip("No reordenar ni borrar bancos: el índice viaja por red. Solo agregar al final")]
        public SfxBanco[] bancos = new SfxBanco[0];
    }
}
