using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Medidata.Pikapika.Miner.Models;
using NuGet;
using NuGet.Packaging;

namespace Medidata.Pikapika.Miner.Extensions
{
    public static class XmlExtenstions
    {
        public static XDocument TryConvertToXDocument(this string xmlString, Logger logger, out bool isXmlStringValid, bool lastTry = false)
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
                logger.LogWarning($"First TryConvertToXDocument Exception encountered: {ex.Message}");
                if (lastTry)
                {
                    logger.LogError($"Last TryConvertToXDocument Exception encountered: {ex.Message}");
                    isXmlStringValid = false;
                    return null;
                }

                logger.LogWarning($"Trying again TryConvertToXDocument without extra byte as prefix in XML doc.");
                var _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
                if (xmlString.StartsWith(_byteOrderMarkUtf8))
                {
                    xmlString = xmlString.Remove(0, _byteOrderMarkUtf8.Length);
                }
                return TryConvertToXDocument(xmlString, logger, out isXmlStringValid, true);
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
            var result = new List<string>();
            var propertyGroups = document.Root.Elements("PropertyGroup");

            var targetFramework = propertyGroups
                .Where(element => element.Elements("TargetFramework").Any())
                .FirstOrDefault()?
                .Element("TargetFramework")
                .Value;
            if (!string.IsNullOrWhiteSpace(targetFramework))
            {
                result.Add(targetFramework);
            }
            else
            {
                var targetFrameworks = propertyGroups
                    .Where(element => element.Elements("TargetFrameworks").Any())
                    .FirstOrDefault()?
                    .Element("TargetFrameworks")
                    .Value;
                if (!string.IsNullOrWhiteSpace(targetFrameworks))
                {
                    result.AddRange(targetFrameworks.Split(';'));
                }
            }
            return result;
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
