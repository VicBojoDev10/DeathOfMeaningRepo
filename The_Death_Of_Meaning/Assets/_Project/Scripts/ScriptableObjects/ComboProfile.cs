using TDOM.Contracts;
using UnityEngine;

namespace TDOM.POCO.ScriptableObjects
{
    [CreateAssetMenu(fileName = "ComboProfile", menuName = "TDOM/ComboProfile")]
    public class ComboProfile : ScriptableObject
    {
        [Header("Configuración del Combo")]
        public AttackStep[] attackSteps;   
        public bool pasoCargado;           
        public float HoldThreshold;        
        public float MaxChargeTime;        
        public float BufferWindow;         
    }
}
