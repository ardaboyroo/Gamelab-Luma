using System;
using Unity.Netcode;

namespace UserModels.Display
{

    [Serializable]
    public struct PlayerProfile : INetworkSerializable, IEquatable<PlayerProfile>
    {
        public string Nickname;

        public PlayerProfile(string nickname)
        {
            Nickname = nickname;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Nickname);
        }

        public bool Equals(PlayerProfile other) => Nickname == other.Nickname;
    }
}
