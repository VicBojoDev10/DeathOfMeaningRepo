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
            "Segundos con el boton apretado para que cuente como ataque cargado en vez de "
                + "golpe basico. Mas bajo = responde antes, pero un toque largo ya es cargado."
        )]
        public float HoldThreshold;

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
