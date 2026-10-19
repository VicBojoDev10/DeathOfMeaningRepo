using System.Collections;

namespace TDOM.Unity.Flow
{
    public interface ICinematicExit
    {
        void Preparar();
        IEnumerator Salir();
    }
}
