using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Lime.Cli.Certificate
{
    public static class CertificateResolver
    {
        public static X509Certificate2 GetCertificateFromThumbprint(string certificateThumbprint)
        {
            X509Certificate2 certificate = null;
            X509Store store;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            }
            else
            {
                // .NET Core on linux doesn't load the private keys of certificate when using LocalMachine store.
                // Besides that, it only supports the 'Root' and 'CertificateAuthority' stores on LocalMachine.
                // https://github.com/dotnet/corefx/issues/32367
                // https://serverfault.com/questions/259302/best-location-for-ssl-certificate-and-private-keys-on-ubuntu
                // It is required to install the certificate programatically in the "CurrentUser" store (a concept that doesn't exists on linux).
                // You should load the .pfx file to the linux server and create a C# program to install the certificate there.
                // https://stackoverflow.com/questions/43660786/get-private-key-with-dotnet-core-on-linux
                // https://github.com/dotnet/corefx/issues/16879
                store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            }

            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, certificateThumbprint, false);

            if (certificates.Count > 0)
            {
                certificate = certificates[0];
            }

            store.Close();
            return certificate;
        }
    }
}
