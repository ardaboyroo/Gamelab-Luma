using System;
using Unity.Netcode;

namespace Luma.QuipService
{
    /// <summary>
    /// RPC/NetworkVariable-compatible payload. All peers must use the same catalog IDs.
    /// Resolve QuipId through QuipCatalog.TryGetQuip to obtain the chat text.
    /// </summary>
    [Serializable]
    public struct QuipMessage : INetworkSerializable, IEquatable<QuipMessage>
    {
        public int QuipId;

        public QuipMessage(int quipId) => QuipId = quipId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref QuipId);
        }

        public bool Equals(QuipMessage other) => QuipId == other.QuipId;
        public override bool Equals(object obj) => obj is QuipMessage other && Equals(other);
        public override int GetHashCode() => QuipId;
    }
}
