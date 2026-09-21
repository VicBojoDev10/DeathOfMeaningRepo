namespace TDOM.Contracts
{
    [System.Serializable]
    public struct AttackStep
    {
        public float WindupTime;
        public float ActiveTime;
        public float RecoveryTime;
        public float ComboWindowTime;
        public float Damage;
        public string AnimTrigger;
    }
}
