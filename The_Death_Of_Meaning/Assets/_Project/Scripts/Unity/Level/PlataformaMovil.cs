using UnityEngine;

namespace TDOM.Unity.Level
{
    /// <summary>Base común para plataformas que informan su desplazamiento del último frame.</summary>
    public abstract class PlataformaMovil : MonoBehaviour
    {
        public abstract Vector3 FrameDelta { get; }
    }
}
