using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

namespace TDOM.Unity
{
    public enum CharacterIds
    {
        None,
     Zendre,
     Ayla
    }
    public struct PlayerSelectionState : INetworkSerializable, System.IEquatable<PlayerSelectionState>
    {
        public ulong ClientId;
        public CharacterIds Character;
        public bool Ready;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Character);
            serializer.SerializeValue(ref Ready);
        }

        public bool Equals(PlayerSelectionState other) =>
            ClientId == other.ClientId && Character == other.Character && Ready == other.Ready;
    }
}
