using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "VfxCatalogo", menuName = "TDOM/Vfx/Catalogo")]
    public sealed class VfxCatalogo : ScriptableObject
    {
        public GameObject[] efectos = new GameObject[0];
    }
}
