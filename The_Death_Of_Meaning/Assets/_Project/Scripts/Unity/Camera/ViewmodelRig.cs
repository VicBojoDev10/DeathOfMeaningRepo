using UnityEngine;

namespace TDOM.Unity.Camera
{
    [DisallowMultipleComponent]
    public sealed class ViewmodelRig : MonoBehaviour
    {
        private static bool _avisoMostrado;

        private void Awake()
        {
            if (_avisoMostrado)
                return;

            _avisoMostrado = true;
            Debug.LogWarning(
                "[ViewmodelRig] Pendiente de implementar (TW-58): todavía no hay animación de brazos ni sway.",
                this
            );
        }
    }
}
