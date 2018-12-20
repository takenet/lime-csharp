using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Lime.Transport.Tcp.UnitTests
{
    public static class CertificateUtil
    {
        private static readonly ConcurrentDictionary<string, X509Certificate2> CertificateCache = new ConcurrentDictionary<string, X509Certificate2>();

        public static X509Certificate2 ReadFromFile(string filepath)
        {
            var bytes = File.ReadAllBytes(filepath);
            var certificate = new X509Certificate2();
            certificate.Import(bytes);
            return certificate;
        }
    }
}