using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace Replay.Services.AssemblyLoading
{
    public class DotNetAssemblyLocator
    {
        private readonly DotNetCoreInstallationLocator dotNetInstallationLocator;
        private readonly FileIO io;

        public DotNetAssemblyLocator(Func<Process> processRunner, FileIO io)
        {
            this.dotNetInstallationLocator = new DotNetCoreInstallationLocator(processRunner);
            this.io = io;
        }

        public MetadataReference[] GetDefaultAssemblies()
        {
            var installation = dotNetInstallationLocator.GetReferenceAssemblyPath() ?? throw new InvalidOperationException("Could not find dotnet installation");

            // get SDK DLLs
            var implementationPath = Path.Combine(installation.BasePath, "shared", "Microsoft.NETCore.App", installation.Version);
            // get xml documentation for those DLLs
            var latestRef = io
                .GetDirectories(
                    Path.Combine(installation.BasePath, "packs", "Microsoft.NETCore.App.Ref")
                )
                .Last();
            var referencePath = Path.Combine(latestRef, "ref");

            return GroupDirectoryContentsIntoAssemblies(
                    io.GetFilesInDirectory(implementationPath, "*.dll", SearchOption.AllDirectories)
                    .Union(io.GetFilesInDirectory(referencePath, "*.xml", SearchOption.AllDirectories))
                )
                .Where(assembly => assembly.AssemblyName.StartsWith("System"))
                .Select(assembly => io.CreateMetadataReferenceWithDocumentation(assembly))
                .ToArray();
        }

        public static IEnumerable<AssemblyWithXmlDocumentation> GroupDirectoryContentsIntoAssemblies(IEnumerable<string> directoryContents) => directoryContents
            .GroupBy(Path.GetFileNameWithoutExtension)
            .Select(kvp =>
            {
                var assembly = kvp.First(p => p.EndsWith("dll"));
                var doc = kvp.FirstOrDefault(p => p.EndsWith("xml"));
                return new AssemblyWithXmlDocumentation(
                    assemblyName: kvp.Key,
                    fullAssemblyPath: assembly,
                    fullXmlDocumentationPath: doc
                );
            });
    }
}
