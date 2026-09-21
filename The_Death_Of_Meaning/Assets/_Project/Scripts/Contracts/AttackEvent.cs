using Unity.Netcode;
namespace TDOM.Contracts
{
    public enum AttackKind
    {
        Light,
        Charged
    }

    public struct AttackEvent : INetworkSerializable
    {
        public AttackKind Kind;
        public int ComboIndex;
        public float ChargeRatio;
        public float Damage;

        public AttackEvent(AttackKind kind, int comboIndex, float chargeRatio, float damage)
        {
            Kind = kind;
            ComboIndex = comboIndex;
            ChargeRatio = chargeRatio;
            Damage = damage;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref Kind);
            serializer.SerializeValue(ref ComboIndex);
            serializer.SerializeValue(ref ChargeRatio);
            serializer.SerializeValue(ref Damage);
        }
    }

}
