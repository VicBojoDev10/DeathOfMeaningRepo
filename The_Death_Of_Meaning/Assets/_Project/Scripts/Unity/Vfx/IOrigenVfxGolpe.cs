using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public interface IOrigenVfxGolpe
    {
        bool Hoja(out Vector3 baseHoja, out Vector3 punta);

        // Centro y rotacion de la estela del golpe en curso (false si no hay estela reciente).
        bool Estela(out Vector3 centro, out Quaternion rotacion);
    }
}
