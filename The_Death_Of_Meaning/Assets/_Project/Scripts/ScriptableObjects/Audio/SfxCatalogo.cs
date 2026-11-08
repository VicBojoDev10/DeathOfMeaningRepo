using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "SfxCatalogo", menuName = "TDOM/Audio/Sfx Catalogo")]
    public sealed class SfxCatalogo : ScriptableObject
    {
        public SfxBanco[] bancos = new SfxBanco[0];
    }
}
