using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Lime.Protocol.Security;

namespace Lime.Protocol.Network
{
    public static class X509CertificateExtensions
    {
        public static string[] GetDomains(this X509Certificate2 certificate)
        {
            string[] domains;
            
            // Check if the cert contains the 'Subject Alternative Name' extension 
            var sanExtension = certificate.Extensions[X509SubjectAlternativeNameConstants.Oid];
            if (sanExtension != null)
            {
                // If there's a SAN in the certificate, it must be used in favor of 'subject' value.
                // https://stackoverflow.com/a/5937270/704742
                // https://tools.ietf.org/html/rfc6125#section-6.4.4
                var asnEncodedData = new AsnEncodedData(sanExtension.Oid, sanExtension.RawData);
                domains = asnEncodedData
                    .Format(false)
                    .Split(new [] {X509SubjectAlternativeNameConstants.Separator}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Split(new[] {X509SubjectAlternativeNameConstants.Delimiter}, StringSplitOptions.RemoveEmptyEntries))
                    .Where(c => c[0].Trim(' ').Equals(X509SubjectAlternativeNameConstants.Identifier))
                    .Select(c => c[1].Trim(' '))
                    .ToArray();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(certificate.Subject))
                {
                    return new string[0];
                }
                
                domains = certificate
                    .Subject
                    .Split(',')
                    .Select(c => c.Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries))
                    .Where(c => c[0].Trim(' ').Equals("CN"))
                    .Select(c => c[1].Trim(' '))
                    .ToArray();
            }

            return domains;
        }
        
        /// <summary>
        /// Gets the identity domain role for the given certificate.
        /// </summary>
        public static DomainRole GetDomainRole(this X509Certificate2 certificate, Identity identity)
        {
            var domains = certificate.GetDomains();
            if (domains.Length == 0) return DomainRole.Unknown;

            // Given the identity "Domain" value is "limeprotocol.org":
            // 1. Check if there's a '*.limeprotocol.org' CN in the certificate for giving the 'RootAuthority' domain role to the identity. 
            if (
                domains.Any(
                    c => c.Equals($"*.{identity.Domain}", StringComparison.OrdinalIgnoreCase)))
            {
                return DomainRole.RootAuthority;
            }
            
            // 2. Check if there's a 'limeprotocol.org' or '*.org' CNs in the certificate for giving the 'Authority' domain role to the identity.
            if (
                domains.Any(
                    c =>
                        c.Equals(identity.Domain, StringComparison.OrdinalIgnoreCase) ||
                        c.Equals($"*.{identity.Domain.TrimFirstDomainLabel()}", StringComparison.OrdinalIgnoreCase)))
            {
                return DomainRole.Authority;
            }

            return DomainRole.Unknown;
        }
        
        // We don't have a strongly typed extension to parse Subject Alt Names, so we have to do a workaround 
        // to figure out what the identifier, delimiter, and separator is by using a well-known extension
        // Source: https://github.com/dotnet/wcf/blob/a9984490334fdc7d7382cae3c7bc0c8783eacd16/src/System.Private.ServiceModel/src/System/IdentityModel/Claims/X509CertificateClaimSet.cs
        private static class X509SubjectAlternativeNameConstants
        {
            public const string Oid = "2.5.29.17";

            private static readonly string s_identifier;
            private static readonly char s_delimiter;
            private static readonly string s_separator;

            private static bool s_successfullyInitialized = false;
            private static Exception s_initializationException;

            public static string Identifier
            {
                get
                {
                    EnsureInitialized();
                    return s_identifier;
                }
            }

            public static char Delimiter
            {
                get
                {
                    EnsureInitialized();
                    return s_delimiter;
                }
            }
            public static string Separator
            {
                get
                {
                    EnsureInitialized();
                    return s_separator;
                }
            }

            private static void EnsureInitialized()
            {
                if (!s_successfullyInitialized)
                {
                    throw new FormatException(string.Format(
                        "There was an error detecting the identifier, delimiter, and separator for X509CertificateClaims on this platform.{0}" +
                        "Detected values were: Identifier: '{1}'; Delimiter:'{2}'; Separator:'{3}'",
                        Environment.NewLine,
                        s_identifier,
                        s_delimiter,
                        s_separator
                    ), s_initializationException);
                }
            }

            // static initializer runs only when one of the properties is accessed
            static X509SubjectAlternativeNameConstants()
            {
                // Extracted a well-known X509Extension
                byte[] x509ExtensionBytes = new byte[] {
                    48, 36, 130, 21, 110, 111, 116, 45, 114, 101, 97, 108, 45, 115, 117, 98, 106, 101, 99,
                    116, 45, 110, 97, 109, 101, 130, 11, 101, 120, 97, 109, 112, 108, 101, 46, 99, 111, 109
                };
                const string subjectName1 = "not-real-subject-name";

                try
                {
                    X509Extension x509Extension = new X509Extension(Oid, x509ExtensionBytes, true);
                    string x509ExtensionFormattedString = x509Extension.Format(false);

                    // Each OS has a different dNSName identifier and delimiter
                    // On Windows, dNSName == "DNS Name" (localizable), on Linux, dNSName == "DNS"
                    // e.g.,
                    // Windows: x509ExtensionFormattedString is: "DNS Name=not-real-subject-name, DNS Name=example.com"
                    // Linux:   x509ExtensionFormattedString is: "DNS:not-real-subject-name, DNS:example.com"
                    // Parse: <identifier><delimter><value><separator(s)>

                    int delimiterIndex = x509ExtensionFormattedString.IndexOf(subjectName1) - 1;
                    s_delimiter = x509ExtensionFormattedString[delimiterIndex];

                    // Make an assumption that all characters from the the start of string to the delimiter 
                    // are part of the identifier
                    s_identifier = x509ExtensionFormattedString.Substring(0, delimiterIndex);

                    int separatorFirstChar = delimiterIndex + subjectName1.Length + 1;
                    int separatorLength = 1;
                    for (int i = separatorFirstChar + 1; i < x509ExtensionFormattedString.Length; i++)
                    {
                        // We advance until the first character of the identifier to determine what the
                        // separator is. This assumes that the identifier assumption above is correct
                        if (x509ExtensionFormattedString[i] == s_identifier[0])
                        {
                            break;
                        }

                        separatorLength++;
                    }

                    s_separator = x509ExtensionFormattedString.Substring(separatorFirstChar, separatorLength);

                    s_successfullyInitialized = true;
                }
                catch (Exception ex)
                {
                    s_successfullyInitialized = false;
                    s_initializationException = ex;
                }
            }
        }

    }
}