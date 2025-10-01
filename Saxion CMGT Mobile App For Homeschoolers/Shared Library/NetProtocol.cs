// SharedLibrary/NetProtocol.cs
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Resolvers;

namespace SharedLibrary.Net
{
    public enum PacketType : ushort
    {
        LoginRequest = 1,
        LoginResponse = 2,
        SessionInit = 3,
        PlayerStateUdp = 10,
        ReliableEvent = 11,
        Ack = 12,
        ChatMessage = 20,
        RpcRequest = 30,
        RpcResponse = 31,
    }

    [MessagePackObject]
    public class LoginRequest
    {
        [Key(0)] public string Username { get; set; }
        [Key(1)] public string PasswordOrToken { get; set; }
    }

    [MessagePackObject]
    public class LoginResponse
    {
        [Key(0)] public bool Success { get; set; }
        [Key(1)] public string Message { get; set; }
    }

    [MessagePackObject]
    public class SessionInit
    {
        [Key(0)] public Guid SessionId { get; set; }
        [Key(1)] public byte[] UdpKey { get; set; } // 32 bytes
        [Key(2)] public uint UdpNoncePrefix { get; set; } // 4 bytes used to build 12-byte nonces with sequence number
        [Key(3)] public int ServerUdpPort { get; set; }
    }

    // example UDP state update — consider using tighter packing for high-frequency updates (see notes)
    [MessagePackObject]
    public class PlayerStateUdp
    {
        [Key(0)] public ulong Sequence { get; set; }
        [Key(1)] public int EntityId { get; set; }
        [Key(2)] public float Px { get; set; }
        [Key(3)] public float Py { get; set; }
        [Key(4)] public float Pz { get; set; }
        [Key(5)] public float Qx { get; set; }
        [Key(6)] public float Qy { get; set; }
        [Key(7)] public float Qz { get; set; }
        [Key(8)] public float Qw { get; set; }
    }

    [MessagePackObject]
    public class ChatMessage
    {
        [Key(0)] public string Message { get; set; }
    }

    public static class NetSerializer
    {
        private static readonly MessagePackSerializerOptions Options =
            MessagePackSerializerOptions.Standard
                .WithResolver(CompositeResolver.Create(
                    ContractlessStandardResolver.Instance
                ))
                .WithCompression(MessagePackCompression.Lz4BlockArray);

        public static byte[] Serialize<T>(T obj) => MessagePackSerializer.Serialize(obj, Options);
        public static T Deserialize<T>(byte[] data) => MessagePackSerializer.Deserialize<T>(data, Options);

        public static async Task WriteLengthPrefixed(Stream stream, byte[] data)
        {
            var len = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length));
            await stream.WriteAsync(len, 0, len.Length);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
        }

        public static async Task<byte[]> ReadLengthPrefixed(Stream stream)
        {
            var lenBuf = new byte[4];
            await ReadExact(stream, lenBuf, 0, 4);
            int len = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lenBuf, 0));
            if (len < 0 || len > 16_777_216) // sanity check
                throw new InvalidDataException($"Length-prefixed value too large: {len}");
            var buf = new byte[len];
            await ReadExact(stream, buf, 0, len);
            return buf;
        }

        private static async Task ReadExact(Stream stream, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int r = await stream.ReadAsync(buffer, offset, count);
                if (r == 0) throw new EndOfStreamException();
                offset += r;
                count -= r;
            }
        }
    }
}
