using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Text.RegularExpressions;

namespace Analyzers
{
    public static class ProtobufHelpers
    {
        public static IEnumerable<(string filePath, string content)> DiscoverProtoFiles(Compilation compilation, AnalyzerConfigOptionsProvider optionsProvider)
        {
            var projectOptions = GetProjectOptions(compilation, optionsProvider);
            if (projectOptions == null)
            {
                yield break;
            }

            if (!projectOptions.TryGetValue("build_property.projectdir", out var projectDir) ||
                string.IsNullOrEmpty(projectDir))
            {
                yield break;
            }

            var protoRootDir = projectDir;
            if (projectOptions.TryGetValue("build_property.protorootdir", out var customProtoRoot))
            {
                protoRootDir = Path.Combine(projectDir, customProtoRoot);
            }

            var fullProtoRoot = Path.GetFullPath(protoRootDir);
            var files = Directory.GetFiles(fullProtoRoot, "*.proto", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                yield return (file, File.ReadAllText(file));
            }
        }

        public static AnalyzerConfigOptions GetProjectOptions(Compilation compilation, AnalyzerConfigOptionsProvider optionsProvider)
        {
            var firstTree = compilation.SyntaxTrees.FirstOrDefault();
            return firstTree != null ? optionsProvider.GetOptions(firstTree) : null;
        }

        public static string? GetNamespace(string fileContents)
        {
            var nsMatch = Regex.Match(fileContents, @"option\s+csharp_namespace\s*=\s*""([^""]+)"";");
            if (nsMatch.Success)
            {
                return nsMatch.Groups[1].Value;
            }

            return null;
        }
    }
}
