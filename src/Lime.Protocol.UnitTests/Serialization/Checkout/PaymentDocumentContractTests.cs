using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Serialization.Checkout
{
    [TestFixture]
    public class PaymentDocumentContractTests
    {
        [TestCaseSource(nameof(AllTypeCases))]
        public void PaymentType_Members_FollowDocumentContract(Type type)
        {
            var violations = FindViolations(type);

            type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ShouldNotBeEmpty();
            violations.ShouldBeEmpty();
        }

        [TestCaseSource(nameof(DocumentCases))]
        public void PaymentDocument_ContractVersion_IsNullableInt(Type documentType)
        {
            var property = documentType.GetProperty("ContractVersion", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            property.ShouldNotBeNull();
            property.PropertyType.ShouldBe(typeof(int?));
            property.GetCustomAttribute<DataMemberAttribute>().Name.ShouldBe("contractVersion");
        }

        [Test]
        public void FindViolations_ClassBreakingTheRules_ReportsEveryRule()
        {
            var violations = FindViolations(typeof(ContractProbeDocument));

            violations.ShouldContain(v => v.Contains("Amount") && v.Contains("non-nullable"));
            violations.ShouldContain(v => v.Contains("Price") && v.Contains("forbidden type"));
            violations.ShouldContain(v => v.Contains("Names") && v.Contains("forbidden type"));
            violations.ShouldContain(v => v.Contains("Extra") && v.Contains("forbidden type"));
            violations.ShouldContain(v => v.Contains("Kind") && v.Contains("forbidden type"));
            violations.ShouldContain(v => v.Contains("Hidden") && v.Contains("IgnoreDataMember"));
            violations.ShouldContain(v => v.Contains("Undeclared") && v.Contains("without DataMember"));
            violations.ShouldContain(v => v.Contains("Renamed") && v.Contains("name"));
            violations.ShouldContain(v => v.Contains("Computed") && v.Contains("read-only"));
            violations.ShouldContain(v => v.Contains("ContractVersion"));
        }

        private static IReadOnlyList<string> FindViolations(Type type)
        {
            var violations = new List<string>();
            var contract = type.GetCustomAttribute<DataContractAttribute>();
            if (contract == null || contract.Namespace != null)
            {
                violations.Add(type.Name + ": missing DataContract or DataContract with namespace");
            }

            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                violations.Add(type.Name + ": missing public parameterless constructor");
            }

            if (type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Any())
            {
                violations.Add(type.Name + ": public instance field");
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                violations.AddRange(FindPropertyViolations(type.Name + "." + property.Name, property));
            }

            if (typeof(Document).IsAssignableFrom(type))
            {
                var version = type.GetProperty("ContractVersion", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (version == null || version.PropertyType != typeof(int?))
                {
                    violations.Add(type.Name + ".ContractVersion: missing or not int?");
                }
            }

            return violations;
        }

        private static IEnumerable<string> FindPropertyViolations(string name, PropertyInfo property)
        {
            var member = property.GetCustomAttribute<DataMemberAttribute>();
            var expectedName = char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
            if (member == null)
            {
                yield return name + ": public property without DataMember";
            }
            else if (member.Name != expectedName)
            {
                yield return name + ": DataMember name is not " + expectedName;
            }

            if (property.GetCustomAttribute<IgnoreDataMemberAttribute>() != null)
            {
                yield return name + ": IgnoreDataMember";
            }

            if (!property.CanRead || property.SetMethod == null || !property.SetMethod.IsPublic)
            {
                yield return name + ": read-only or computed property";
            }

            var propertyType = property.PropertyType;
            if (propertyType.IsValueType && Nullable.GetUnderlyingType(propertyType) == null)
            {
                yield return name + ": non-nullable value type " + propertyType.Name;
            }

            if (!IsAllowedType(propertyType))
            {
                yield return name + ": forbidden type " + propertyType.Name;
            }
        }

        private static bool IsAllowedType(Type type)
        {
            if (type.IsArray)
            {
                return IsAllowedType(type.GetElementType());
            }

            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            if (underlying.IsEnum || underlying == typeof(decimal) || underlying == typeof(object))
            {
                return false;
            }

            if (underlying == typeof(string) || underlying.IsPrimitive)
            {
                return true;
            }

            if (underlying.IsGenericType || underlying.IsAbstract || underlying.IsInterface
                || typeof(IEnumerable).IsAssignableFrom(underlying) || typeof(JToken).IsAssignableFrom(underlying))
            {
                return false;
            }

            return underlying.IsClass && underlying.GetCustomAttribute<DataContractAttribute>() != null;
        }

        private static object[] AllTypeCases() => PaymentDocumentCatalog.AllTypes.Cast<object>().ToArray();

        private static object[] DocumentCases() => PaymentDocumentCatalog.DocumentTypes.Cast<object>().ToArray();

        public enum ProbeKind
        {
            First,
        }

        [DataContract]
        public class ContractProbeDocument : Document
        {
            public ContractProbeDocument()
                : base(MediaType.Parse("application/vnd.example.contract-probe+json")) { }

            [DataMember(Name = "amount")]
            public long Amount { get; set; }

            [DataMember(Name = "price")]
            public decimal? Price { get; set; }

            [DataMember(Name = "names")]
            public List<string> Names { get; set; }

            [DataMember(Name = "extra")]
            public Dictionary<string, object> Extra { get; set; }

            [DataMember(Name = "kind")]
            public ProbeKind? Kind { get; set; }

            [DataMember(Name = "hidden")]
            [IgnoreDataMember]
            public string Hidden { get; set; }

            public string Undeclared { get; set; }

            [DataMember(Name = "renamed_value")]
            public string Renamed { get; set; }

            [DataMember(Name = "computed")]
            public string Computed => "value";
        }
    }
}
