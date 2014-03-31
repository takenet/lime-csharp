using Lime.Protocol.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization
{
    public static class TypeUtil
    {
        private static Dictionary<MediaType, Type> _documentMediaTypeDictionary;
        private static Dictionary<AuthenticationScheme, Type> _authenticationSchemeDictionary;

        static TypeUtil()
        {
            _documentMediaTypeDictionary = new Dictionary<MediaType, Type>();

            var documentTypes = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(t => !t.IsAbstract && typeof(Document).IsAssignableFrom(t));

            foreach (var documentType in documentTypes)
            {
                var document = Activator.CreateInstance(documentType) as Document;

                if (document != null)
                {
                    _documentMediaTypeDictionary.Add(document.GetMediaType(), documentType);
                }
            }

            _authenticationSchemeDictionary = new Dictionary<AuthenticationScheme, Type>();

            var authenticationTypes = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(t => !t.IsAbstract && typeof(Authentication).IsAssignableFrom(t));

            foreach (var authenticationType in authenticationTypes)
            {
                var authentication = Activator.CreateInstance(authenticationType) as Authentication;

                if (authentication != null)
                {
                    _authenticationSchemeDictionary.Add(authentication.GetAuthenticationScheme(), authenticationType);
                }
            }
        }

        public static bool TryGetTypeForMediaType(MediaType mediaType, out Type type)
        {
            return _documentMediaTypeDictionary.TryGetValue(mediaType, out type);            
        }

        public static bool TryGetTypeForAuthenticationScheme(AuthenticationScheme scheme, out Type type)
        {
            return _authenticationSchemeDictionary.TryGetValue(scheme, out type);
        }
    }
}
