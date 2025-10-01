using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

public class Utils
{
    public static X509Certificate2 CreateSelfSignedCertificate(string subjectName)
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(
            new X500DistinguishedName(subjectName),
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );

        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));

        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

        // Re-import with all flags Unity / non-admin compatible
        return new X509Certificate2(cert.Export(X509ContentType.Pfx),
            (string?)null,
            X509KeyStorageFlags.Exportable |
            X509KeyStorageFlags.PersistKeySet |
            X509KeyStorageFlags.UserKeySet |
            X509KeyStorageFlags.MachineKeySet
        );
    }
}
