using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Lime.Protocol.Security;

namespace Lime.Protocol.Network
{
    public static class X509CertificateExtensions
    {
        /// <summary>
        /// Gets the identity domain role for the given certificate.
        /// </summary>
        public static DomainRole GetDomainRole(this X509Certificate certificate, Identity identity)
        {
            var subject = certificate.Subject;
            if (certificate is X509Certificate2 x509Certificate2)
            {
                // Check if the cert contains the 'Subject Alternative Name' extension 
                var sanExtension = x509Certificate2.Extensions["2.5.29.17"];
                if (sanExtension != null)
                {
                    // If there's a SAN in the certificate, it must be used in favor of 'subject' value.
                    // https://stackoverflow.com/a/5937270/704742
                    // https://tools.ietf.org/html/rfc6125#section-6.4.4
                    var asnEncodedData = new AsnEncodedData(sanExtension.Oid, sanExtension.RawData);
                    subject = asnEncodedData
                        .Format(false)
                        .Replace("DNS:", "DNS Name="); // The behavior is different on linux: https://github.com/dotnet/core/issues/2243
                }
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                return DomainRole.Unknown;
            }
            
            var commonNames = subject
                .Split(',')
                .Select(c => c.Split('='))
                .Select(c => new
                {
                    DN = c[0].Trim(' '),        // Distinguished names: 'CN' from 'Subject' and 'DNS Name' from 'Subject Alternative Name'
                    Subject = c[1].Trim(' ')
                })
                .Where(s => s.DN.Equals("CN") || s.DN.Equals("DNS Name"))
                .ToArray();

            // Given the identity "Domain" value is "limeprotocol.org":
            // 1. Check if there's a '*.limeprotocol.org' CN in the certificate for giving the 'RootAuthority' domain role to the identity. 
            if (
                commonNames.Any(
                    c => c.Subject.Equals($"*.{identity.Domain}", StringComparison.OrdinalIgnoreCase)))
            {
                return DomainRole.RootAuthority;
            }
            
            // 2. Check if there's a 'limeprotocol.org' or '*.org' CNs in the certificate for giving the 'Authority' domain role to the identity.
            if (
                commonNames.Any(
                    c =>
                        c.Subject.Equals(identity.Domain, StringComparison.OrdinalIgnoreCase) ||
                        c.Subject.Equals($"*.{identity.Domain.TrimFirstDomainLabel()}", StringComparison.OrdinalIgnoreCase)))
            {
                return DomainRole.Authority;
            }

            return DomainRole.Unknown;
        }
    }
}