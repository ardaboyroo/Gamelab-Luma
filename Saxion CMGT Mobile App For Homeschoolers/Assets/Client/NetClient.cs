using System;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using SharedLibrary.Net;
using System.Net;
using System.IO;

public class NetClient
{
    private TcpClient _tcp;
    private SslStream _ssl;
    private UdpClient _udp;
    private Guid _sessionId;
    private byte[] _udpKey;
    private uint _udpNoncePrefix;
    private ulong _sendSeq = 0;
    private IPEndPoint _serverUdpEndpoint;

    public async Task<bool> ConnectAsync(string host, int tcpPort)
    {
        _tcp = new TcpClient();
        await _tcp.ConnectAsync(host, tcpPort);
        _ssl = new SslStream(_tcp.GetStream(), false, (s, cert, chain, errors) => true, null);
        await _ssl.AuthenticateAsClientAsync(host, null, System.Security.Authentication.SslProtocols.Tls12, false);

        // Send login
        var login = new LoginRequest { Username = "player1", PasswordOrToken = "token" };
        await NetSerializer.WriteLengthPrefixed(_ssl, NetSerializer.Serialize(login));

        // Receive SessionInit
        var bytes = await NetSerializer.ReadLengthPrefixed(_ssl);
        var init = NetSerializer.Deserialize<SessionInit>(bytes);
        _sessionId = init.SessionId;
        _udpKey = init.UdpKey;
        _udpNoncePrefix = init.UdpNoncePrefix;
        _serverUdpEndpoint = new IPEndPoint(IPAddress.Parse(host), init.ServerUdpPort);

        _udp = new UdpClient();
        _udp.Connect(_serverUdpEndpoint);

        return true;
    }

    private bool ValidateServerCert(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors errors)
    {
        // Implement certificate pinning in production. For testing, allow:
        return true;
    }

    public void SendPlayerState(PlayerStateUdp state)
    {
        var payload = NetSerializer.Serialize(state);
        var cipherAndTag = EncryptUdp(_udpKey, _udpNoncePrefix, _sendSeq, payload);
        var packet = new byte[16 + 8 + cipherAndTag.Length];
        Buffer.BlockCopy(_sessionId.ToByteArray(), 0, packet, 0, 16);
        Buffer.BlockCopy(BitConverter.GetBytes(_sendSeq), 0, packet, 16, 8);
        Buffer.BlockCopy(cipherAndTag, 0, packet, 24, cipherAndTag.Length);
        _udp.Send(packet, packet.Length);
        _sendSeq++;
    }

    private static byte[] EncryptUdp(byte[] key, uint noncePrefix, ulong seq, byte[] plaintext)
    {
        var nonce = new byte[12];
        Buffer.BlockCopy(BitConverter.GetBytes(seq), 0, nonce, 0, 8);
        Buffer.BlockCopy(BitConverter.GetBytes(noncePrefix), 0, nonce, 8, 4);

        using var aesgcm = new System.Security.Cryptography.AesGcm(key);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        aesgcm.Encrypt(nonce, plaintext, ciphertext, tag);
        var result = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, result, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, ciphertext.Length, tag.Length);
        return result;
    }

    public async Task SendTcpTestMessage(string msg)
    {
        var chat = new ChatMessage { Message = msg };
        await NetSerializer.WriteLengthPrefixed(_ssl, NetSerializer.Serialize(chat));
        Console.WriteLine($"TCP sent: {msg}");
    }
}