using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using SharedLibrary.Net;

public class Session
{
    public Guid SessionId;
    public byte[] UdpKey; // 32 bytes
    public uint UdpNoncePrefix; // 4 bytes
    public SslStream TcpStream;
    public IPEndPoint UdpEndpoint; // set when first UDP arrives
    public ulong LastRecvUdpSeq = 0;
    public object Lock = new();
}

public class GameServer
{
    private readonly int _tcpPort;
    private readonly int _udpPort;
    private readonly X509Certificate2 _serverCert;
    private readonly ConcurrentDictionary<Guid, Session> _sessions = new();

    public GameServer(int tcpPort, int udpPort, X509Certificate2 serverCert)
    {
        _tcpPort = tcpPort;
        _udpPort = udpPort;
        _serverCert = serverCert;
    }

    public async Task StartAsync()
    {
        var tcpListener = new TcpListener(IPAddress.Any, _tcpPort);
        tcpListener.Start();
        Console.WriteLine($"TCP listening {_tcpPort}");
        _ = Task.Run(() => RunUdpLoop()); // start UDP listener

        while (true)
        {
            var tcpClient = await tcpListener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleTcpClientAsync(tcpClient));
        }
    }

    private async Task HandleTcpClientAsync(TcpClient client)
    {
        using (client)
        {
            var ns = client.GetStream();
            var ssl = new SslStream(ns, false);

            try
            {
                var sslOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = _serverCert,
                    ClientCertificateRequired = false,
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                };

                await ssl.AuthenticateAsServerAsync(sslOptions);

                // receive login request (length-prefixed)
                while (true)
                {
                    var msg = await ReadLengthPrefixed(ssl);
                    var login = NetSerializer.Deserialize<LoginRequest>(msg);
                    // TODO: validate credentials here (DB, token system, etc.)
                    var ok = ValidateLogin(login);
                    if (!ok)
                    {
                        var resp = new LoginResponse { Success = false, Message = "Invalid credentials" };
                        await WriteLengthPrefixed(ssl, NetSerializer.Serialize(resp));
                        break;
                    }

                    // create session
                    var sessionId = Guid.NewGuid();
                    var udpKey = RandomNumberGenerator.GetBytes(32);
                    var noncePrefix = BitConverter.ToUInt32(RandomNumberGenerator.GetBytes(4));
                    var session = new Session
                    {
                        SessionId = sessionId,
                        UdpKey = udpKey,
                        UdpNoncePrefix = noncePrefix,
                        TcpStream = ssl
                    };
                    _sessions[sessionId] = session;

                    var init = new SessionInit
                    {
                        SessionId = sessionId,
                        UdpKey = udpKey,
                        UdpNoncePrefix = noncePrefix,
                        ServerUdpPort = _udpPort
                    };

                    await WriteLengthPrefixed(ssl, NetSerializer.Serialize(init));
                    Console.WriteLine($"Session {sessionId} created for {login.Username}");

                    while (true)
                    {
                        var raw = await ReadLengthPrefixed(ssl);
                        try
                        {
                            var chat = NetSerializer.Deserialize<ChatMessage>(raw);
                            Console.WriteLine($"TCP Chat from {sessionId}: {chat.Message}");
                        }
                        catch
                        {
                            Console.WriteLine("Unknown TCP packet");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TCP client error: {ex}");
            }
        }
    }

    private bool ValidateLogin(LoginRequest login)
    {
        // TODO: real auth. For now accept any username.
        return !string.IsNullOrEmpty(login.Username);
    }

    private static async Task WriteLengthPrefixed(Stream stream, byte[] bytes)
    {
        var len = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytes.Length));
        await stream.WriteAsync(len, 0, 4);
        await stream.WriteAsync(bytes, 0, bytes.Length);
    }

    private static async Task<byte[]> ReadLengthPrefixed(Stream stream)
    {
        var lenBuf = new byte[4];
        await ReadExact(stream, lenBuf, 0, 4);
        var length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lenBuf, 0));
        var buf = new byte[length];
        await ReadExact(stream, buf, 0, length);
        return buf;
    }

    private static async Task ReadExact(Stream stream, byte[] buf, int off, int count)
    {
        while (count > 0)
        {
            var r = await stream.ReadAsync(buf, off, count);
            if (r == 0) throw new EndOfStreamException();
            off += r; count -= r;
        }
    }
   
    private async Task RunUdpLoop()
    {
        using var udp = new UdpClient(_udpPort);
        Console.WriteLine($"UDP listening {_udpPort}");
        while (true)
        {
            var result = await udp.ReceiveAsync();
            var data = result.Buffer;
            if (data.Length < 24) continue; // sessionId(16) + seq(8) minimal

            var sessionId = new Guid(new ReadOnlySpan<byte>(data, 0, 16));
            var seq = BitConverter.ToUInt64(data, 16);
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                Console.WriteLine($"Unknown session UDP {sessionId}");
                continue;
            }

            var cipherAndTag = new ReadOnlySpan<byte>(data, 24, data.Length - 24);
            try
            {
                var plaintext = DecryptUdpPayload(session, seq, cipherAndTag);
                // plaintext is MessagePack bytes, deserialize the message (PlayerStateUdp or Ack)
                // For example:
                var ps = NetSerializer.Deserialize<PlayerStateUdp>(plaintext);
                // handle movement; update authoritative server state, broadcast to nearby players...
                session.UdpEndpoint ??= result.RemoteEndPoint; // bind client UDP endpoint
                session.LastRecvUdpSeq = seq;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UDP decrypt/parse error: {ex.Message}");
            }
        }
    }

    private static byte[] DecryptUdpPayload(Session session, ulong seq, ReadOnlySpan<byte> cipherAndTag)
    {
        // AES-GCM tag is 16 bytes at the end
        if (cipherAndTag.Length < 16) throw new InvalidOperationException("Invalid cipher");
        var tag = cipherAndTag.Slice(cipherAndTag.Length - 16).ToArray();
        var ciphertext = cipherAndTag.Slice(0, cipherAndTag.Length - 16).ToArray();

        var nonce = new byte[12];
        Buffer.BlockCopy(BitConverter.GetBytes(seq), 0, nonce, 0, 8);
        Buffer.BlockCopy(BitConverter.GetBytes(session.UdpNoncePrefix), 0, nonce, 8, 4);

        var plaintext = new byte[ciphertext.Length];
        using var aesgcm = new System.Security.Cryptography.AesGcm(session.UdpKey);
        aesgcm.Decrypt(nonce, ciphertext, tag, plaintext, null);
        return plaintext;
    }
}