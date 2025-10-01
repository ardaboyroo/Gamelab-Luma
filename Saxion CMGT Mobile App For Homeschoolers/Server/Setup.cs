
using System.Security.Cryptography.X509Certificates;

Console.WriteLine("SERVER START");

var cert = Utils.CreateSelfSignedCertificate("CN=localhost");

var server = new GameServer(tcpPort: 5000, udpPort: 5001, serverCert: cert);
await server.StartAsync();


Console.WriteLine("SERVER END");

