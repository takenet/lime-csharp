using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Lime.Transport.Tcp.UnitTests
{
    public static class CertificateUtil
    {
        private static readonly ConcurrentDictionary<string, X509Certificate2> CertificateCache = new ConcurrentDictionary<string, X509Certificate2>();

        public static X509Certificate2 GetOrCreateSelfSignedCertificate(string commonName)
        {
            var key = commonName.Aggregate(string.Empty, (a, b) => $"{a};{b}");
            return CertificateCache.GetOrAdd(key, k => CreateSelfSignedCertificate(commonName));
        }

        /// <summary>
        /// Creates a self-signed certificate
        /// http://stackoverflow.com/questions/13806299/how-to-create-a-self-signed-certificate-using-c
        /// </summary>
        /// <param name="subjectName"></param>       
        /// <returns></returns>
        public static X509Certificate2 CreateSelfSignedCertificate(string subjectName)
        {
            SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();                
            sanBuilder.AddDnsName(subjectName);
            sanBuilder.AddDnsName(Environment.MachineName);

            var distinguishedName = new X500DistinguishedName($"CN={subjectName}");

            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature , false));


                request.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                        new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

                request.CertificateExtensions.Add(sanBuilder.Build());

                var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));                
                return new X509Certificate2(certificate.Export(X509ContentType.Pfx));
            }
        }        
    }
}
