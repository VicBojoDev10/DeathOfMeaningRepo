using UnityEngine;

namespace TDOM.Unity.Camera
{
    [DisallowMultipleComponent]
    [AddComponentMenu("TDOM/Camera/Viewmodel Rig (Pendiente)")]
    public sealed class ViewmodelRig : MonoBehaviour
    {
        [Header("Estado de Desarrollo (Pendiente)")]
        [Tooltip("Animator de brazos en primera persona y sway. Pendiente de implementación.")]
        [SerializeField, TextArea(3, 5)]
#pragma warning disable 0414
        private string _estadoPendiente =
            "PENDIENTE DE IMPLEMENTACIÓN:\n"
            + "Animator de brazos en primera persona y sway.\n"
            + "No forma parte del entregable de 3 semanas.";
#pragma warning restore 0414
    }
}
