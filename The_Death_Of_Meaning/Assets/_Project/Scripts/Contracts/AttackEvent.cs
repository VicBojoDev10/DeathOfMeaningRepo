namespace TDOM.Contracts
{
    public readonly struct AttackEvent
    {
        public readonly AttackStep Step;
        public readonly bool IsCharged;
        public readonly float ChargeRatio;

        public AttackEvent(AttackStep step, bool isCharged, float chargeRatio = 0f)
        {
            Step = step;
            IsCharged = isCharged;
            ChargeRatio = chargeRatio;
        }
    }
}
