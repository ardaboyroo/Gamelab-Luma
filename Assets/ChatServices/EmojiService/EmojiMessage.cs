using System;
using Unity.Netcode;

namespace Luma.EmojiService
{
    /// <summary>RPC/NetworkVariable payload; all peers must share the same emoji catalog IDs.</summary>
    [Serializable]
    public struct EmojiMessage : INetworkSerializable, IEquatable<EmojiMessage>
    {
        public int EmojiId;
        public EmojiMessage(int emojiId) => EmojiId = emojiId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref EmojiId);
        }

        public bool Equals(EmojiMessage other) => EmojiId == other.EmojiId;
        public override bool Equals(object obj) => obj is EmojiMessage other && Equals(other);
        public override int GetHashCode() => EmojiId;
    }
}
