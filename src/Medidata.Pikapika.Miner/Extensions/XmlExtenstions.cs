using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Medidata.Pikapika.Miner.Models;
using NuGet;

namespace Medidata.Pikapika.Miner.Extensions
{
    public static class XmlExtenstions
    {
        public static XDocument TryConvertToXDocument(this string xmlString, out bool isXmlStringValid)
        {
            isXmlStringValid = false;

            try
            {
                var document = XDocument.Parse(xmlString);

                isXmlStringValid = true;
                return document;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TryConvertToXDocument Exception encountered: {ex.Message}");
                isXmlStringValid = false;
                return null;
            }
        }

        public static bool IsNewCsProjFormat(this XDocument document)
        {
            return (document.Root.Attribute("Sdk") != null &&
                    (document.Root.Attribute("Sdk").Value.StartsWith("Microsoft.NET.Sdk")));
        }

        public static DotnetAppProject ConvertToDotnetAppProject(this XDocument document)
        {
            return new DotnetAppProject
            {
                Frameworks = document.GetFrameworksFromNewCsProj(),
                ProjectNugets = document.GetNewCsProjReferences()
            };
        }

        private static IEnumerable<string> GetFrameworksFromNewCsProj(this XDocument document)
        {
            return document.Root.Elements("PropertyGroup")
                .Where(element => element.Elements("TargetFramework").Any())
                .FirstOrDefault()?
                .Element("TargetFramework")
                .Value
                .Split(',');
        }

        public static string GetFrameworkFromOldCsProj(this XDocument document)
        {
            var nav = document.Root.CreateNavigator();
            var xNamespaces = nav.GetNamespacesInScope(XmlNamespaceScope.ExcludeXml);

            if (!xNamespaces.Any())
            {
                return document.Root.Elements("PropertyGroup")
                    .Where(element => element.Elements("TargetFrameworkVersion").Any())
                    .FirstOrDefault()?
                    .Element("TargetFrameworkVersion")
                    .Value;
            }

            XNamespace xNamespace = xNamespaces.First().Value;
            var propertyGroups = document.Root.Elements(xNamespace + "PropertyGroup");
            foreach (var propertyGroup in propertyGroups)
            {
                var targetElement = propertyGroup.Elements(xNamespace + "TargetFrameworkVersion").FirstOrDefault();
                if (targetElement != null)
                {
                    return targetElement.Value;
                }
            }

            return null;
        }

        private static IEnumerable<DotnetAppProjectNuget> GetNewCsProjReferences(this XDocument document)
        {
            var references = new List<DotnetAppProjectNuget>();

            var itemGroups = document.Root.Elements("ItemGroup");
            foreach (var itemGroup in itemGroups)
            {
                references.AddRange(itemGroup.Elements("PackageReference")
                .Select(x => new DotnetAppProjectNuget()
                {
                    Name = x.GetOptionalAttributeValue("Include"),
                    Version = x.GetOptionalAttributeValue("Version")
                }));
            }

            return references;
        }

        public static IEnumerable<DotnetAppProjectNuget> GetPackagesConfigReferences(this XDocument document)
        {
            var references = new List<DotnetAppProjectNuget>();

            references.AddRange(document.Root.Elements("package")
                .Select(x => new DotnetAppProjectNuget()
                {
                    Name = x.GetOptionalAttributeValue("id"),
                    Version = x.GetOptionalAttributeValue("version")
                }));

            return references;
        }
    }
}
