using TDOM.Contracts;
using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "ComboProfile", menuName = "TDOM/ComboProfile")]
    public class ComboProfile : ScriptableObject
    {
        public AttackStep[] Steps;
        public AttackStep Charged;

        [Tooltip(
            "Segundos de carga antes de mostrar el efecto visual. Tambien clasifica una "
                + "soltada como cargado cuando ReleaseChargeThreshold es 0."
        )]
        public float HoldThreshold;

        [Tooltip(
            "Tiempo minimo para que soltar el boton ejecute un cargado. 0 = usa HoldThreshold. "
                + "Mantener hasta MaxChargeTime ejecuta el cargado automaticamente."
        )]
        public float ReleaseChargeThreshold;

        [Tooltip(
            "Segundos de carga tras los que el cargado se ejecuta solo, aunque sigas apretando. "
                + "Tambien es el tope de la carga (ratio 1)."
        )]
        public float MaxChargeTime;

        [Tooltip("Segundos que se recuerda un press para encadenarlo al siguiente golpe.")]
        public float BufferWindow;

        [Tooltip("Segundos tras completar el combo en los que se ignora todo input de ataque.")]
        public float CooldownFinCombo = 0.3f;
    }
}
