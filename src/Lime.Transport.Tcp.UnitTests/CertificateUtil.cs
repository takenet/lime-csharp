using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CERTENROLLLib;
using Pluralsight.Crypto;

namespace Lime.Transport.Tcp.UnitTests
{
    public static class CertificateUtil
    {
        private static readonly ConcurrentDictionary<string, X509Certificate2> CertificateCache = new ConcurrentDictionary<string, X509Certificate2>();

        /// <summary>
        /// Creates a self-signed certificate
        /// http://stackoverflow.com/questions/13806299/how-to-create-a-self-signed-certificate-using-c
        /// </summary>
        /// <param name="subjectName"></param>       
        /// <returns></returns>
        public static X509Certificate2 CreateSelfSignedCertificate(params string[] commonNames)
        {
            using (var ctx = new CryptContext())
            {
                ctx.Open();

                var nameBuilder = new StringBuilder();
                foreach (var commonName in commonNames)
                {
                    nameBuilder.AppendLine($"CN={commonName}");
                }               
                
                var certificate = ctx.CreateSelfSignedCertificate(
                    new SelfSignedCertProperties
                    {
                        IsPrivateKeyExportable = true,
                        KeyBitLength = 4096,                        
                        Name = new X500DistinguishedName(nameBuilder.ToString(), X500DistinguishedNameFlags.UseNewLines),
                        ValidFrom = DateTime.Today.AddDays(-1),
                        ValidTo = DateTime.Today.AddYears(1)
                    });
                
                return certificate;
            }

            //// create DN for subject and issuer
            //var dn = new CX500DistinguishedName();
            //dn.Encode("CN=" + subjectName, X500NameFlags.XCN_CERT_NAME_STR_NONE);
            //// create a new private key for the certificate
            //CX509PrivateKey privateKey = new CX509PrivateKey();
            //privateKey.ProviderName = "Microsoft Base Cryptographic Provider v1.0";
            //privateKey.MachineContext = true;
            //privateKey.Length = 2048;
            //privateKey.KeySpec = X509KeySpec.XCN_AT_SIGNATURE; // use is not limited
            //privateKey.ExportPolicy = X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_PLAINTEXT_EXPORT_FLAG;            
            //privateKey.Create();

            //// Use the stronger SHA512 hashing algorithm
            //var hashobj = new CObjectId();
            //hashobj.InitializeFromAlgorithmName(ObjectIdGroupId.XCN_CRYPT_HASH_ALG_OID_GROUP_ID,
            //    ObjectIdPublicKeyFlags.XCN_CRYPT_OID_INFO_PUBKEY_ANY,
            //    AlgorithmFlags.AlgorithmFlagsNone, "SHA512");

            //// add extended key usage if you want - look at MSDN for a list of possible OIDs
            //var serverOid = new CObjectId();
            //serverOid.InitializeFromValue("1.3.6.1.5.5.7.3.1"); // SSL server
            //var clientOid = new CObjectId();
            //clientOid.InitializeFromValue("1.3.6.1.5.5.7.3.2"); // Client authentication
            //var oidlist = new CObjectIds();
            //oidlist.Add(serverOid);
            //oidlist.Add(clientOid);
            //var eku = new CX509ExtensionEnhancedKeyUsage();
            //eku.InitializeEncode(oidlist);

            //// Create the self signing request
            //var cert = new CX509CertificateRequestCertificate();
            //cert.InitializeFromPrivateKey(X509CertificateEnrollmentContext.ContextMachine, privateKey, "");
            //cert.Subject = dn;
            //cert.Issuer = dn; // the issuer and the subject are the same
            //cert.NotBefore = DateTime.Now;
            //// this cert expires immediately. Change to whatever makes sense for you
            //cert.NotAfter = DateTime.Now.AddYears(10);
            //cert.X509Extensions.Add((CX509Extension)eku); // add the EKU
            //cert.HashAlgorithm = hashobj; // Specify the hashing algorithm
            //cert.Encode(); // encode the certificate
            
            //// Do the final enrollment process
            //var enroll = new CX509Enrollment();
            //enroll.InitializeFromRequest(cert); // load the certificate
            //enroll.CertificateFriendlyName = subjectName; // Optional: add a friendly name
            //string csr = enroll.CreateRequest(); // Output the request in base64
            //// and install it back as the response
            //enroll.InstallResponse(InstallResponseRestrictionFlags.AllowUntrustedCertificate,
            //    csr, EncodingType.XCN_CRYPT_STRING_BASE64, ""); // no password

            //// output a base64 encoded PKCS#12 so we can import it back to the .Net security classes
            //var base64encoded = enroll.CreatePFX("", // no password, this is for internal consumption
            //    PFXExportOptions.PFXExportChainWithRoot);

            //// instantiate the target class with the PKCS#12 data (and the empty password)
            //return new System.Security.Cryptography.X509Certificates.X509Certificate2(
            //    System.Convert.FromBase64String(base64encoded), "",
            //    // mark the private key as exportable (this is usually what you want to do)
            //    System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable
            //);
        }
        
        //public static X509Certificate2 DeleteAndCreateSelfSignedCertificate(string subjectName)
        //{
        //    using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
        //    {
        //        store.Open(OpenFlags.MaxAllowed);
        //        var certificates = store.Certificates
        //            .Find(X509FindType.FindBySubjectName, subjectName, false)
        //            .OfType<X509Certificate2>();
        //        foreach (var x509Certificate2 in certificates)
        //        {
        //            store.Remove(x509Certificate2);
        //        }

        //        store.Close();
        //        return CreateSelfSignedCertificate(subjectName);
        //    }
        //}
    }
}
