using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "BossIdentity", menuName = "TDOM/Boss/Boss Identity")]
    public sealed class BossIdentity : ScriptableObject
    {
        public string Nombre;
    }
}
