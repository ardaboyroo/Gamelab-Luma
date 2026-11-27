using System;
using Unity.Netcode;

namespace Luma.BubbleChatService
{
    [Serializable]
    public struct ChatMessage : INetworkSerializable
    {
        public ChatMessageType Type;
        public int Id;

        public ChatMessage(ChatMessageType type, int id) { Type = type; Id = id; }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Type);
            serializer.SerializeValue(ref Id);
        }
    }
}
