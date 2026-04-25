using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

/// <summary>
/// Genera y almacena en caché un certificado TLS autofirmado con SANs para la IP y el hostname
/// locales. El archivo .crt (DER) puede ser descargado e instalado en dispositivos clientes para
/// que Safari (iOS/macOS) confíe en la conexión HTTPS.
/// </summary>
public static class LocalCertificateProvider
{
    private const string PfxFileName = "localca.pfx";
    private const string CrtFileName = "localca.crt";

    /// <summary>Días de validez del certificado. iOS/Safari exige ≤ 825 días.</summary>
    private const int ValidityDays = 400;

    /// <summary>
    /// Devuelve un certificado X.509 listo para usar en Kestrel y los bytes DER para descarga.
    /// Si ya existe un certificado válido en <paramref name="storeDir"/> lo reutiliza; en caso
    /// contrario genera uno nuevo.
    /// </summary>
    public static (X509Certificate2 Certificate, byte[] DerBytes) GetOrCreate(
        string storeDir, string localIp, string hostName)
    {
        Directory.CreateDirectory(storeDir);

        var pfxPath = Path.Combine(storeDir, PfxFileName);
        var crtPath = Path.Combine(storeDir, CrtFileName);

        if (File.Exists(pfxPath))
        {
            try
            {
                var existing = new X509Certificate2(
                    pfxPath, (string?)null, X509KeyStorageFlags.EphemeralKeySet);

                if (existing.NotAfter > DateTime.UtcNow.AddDays(30))
                {
                    var existingDer = File.Exists(crtPath)
                        ? File.ReadAllBytes(crtPath)
                        : existing.Export(X509ContentType.Cert);
                    return (existing, existingDer);
                }

                existing.Dispose();
            }
            catch { /* cert corrupto → regenerar */ }
        }

        return Generate(storeDir, pfxPath, crtPath, localIp, hostName);
    }

    /// <summary>Nombre de archivo .crt para exponer en el endpoint de descarga.</summary>
    public static string CrtDownloadFileName => CrtFileName;

    private static (X509Certificate2, byte[]) Generate(
        string storeDir, string pfxPath, string crtPath, string localIp, string hostName)
    {
        using var rsa = RSA.Create(2048);

        var req = new CertificateRequest(
            "CN=VirtualWebDisplay Local, O=VirtualWebDisplay",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Básico: certificado de CA para que iOS permita marcarlo como "de confianza total"
        req.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(
                certificateAuthority: true,
                hasPathLengthConstraint: false,
                pathLengthConstraint: 0,
                critical: true));

        req.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment |
                X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                critical: true));

        // Extended Key Usage: TLS Server Authentication
        req.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                [new Oid("1.3.6.1.5.5.7.3.1")],
                critical: false));

        req.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(req.PublicKey, critical: false));

        // SANs — imprescindibles para Chrome, Edge y Safari modernos
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost");
        san.AddDnsName(hostName);
        san.AddIpAddress(IPAddress.Loopback);
        san.AddIpAddress(IPAddress.IPv6Loopback);
        if (IPAddress.TryParse(localIp, out var parsedIp))
            san.AddIpAddress(parsedIp);
        req.CertificateExtensions.Add(san.Build());

        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter  = notBefore.AddDays(ValidityDays);

        var cert = req.CreateSelfSigned(notBefore, notAfter);

        var pfxBytes = cert.Export(X509ContentType.Pfx);
        File.WriteAllBytes(pfxPath, pfxBytes);

        var derBytes = cert.Export(X509ContentType.Cert);
        File.WriteAllBytes(crtPath, derBytes);

        var finalCert = new X509Certificate2(
            pfxBytes, (string?)null, X509KeyStorageFlags.EphemeralKeySet);
        return (finalCert, derBytes);
    }
}
