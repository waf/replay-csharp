using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Replay.Services.AssemblyLoading
{
    public class DotNetAssemblyLocator
    {
        private readonly DotNetCoreInstallationLocator dotNetInstallationLocator;
        private readonly IFileIO io;

        public DotNetAssemblyLocator(IFileIO io)
        {
            this.dotNetInstallationLocator = new DotNetCoreInstallationLocator(io);
            this.io = io;
        }

        public MetadataReference[] GetDefaultAssemblies()
        {
            var installation = dotNetInstallationLocator.GetReferenceAssemblyPath() ?? throw new InvalidOperationException("Could not find dotnet installation");

            return GroupDirectoryContentsIntoAssemblies(
                    io.GetFilesInDirectory(installation.ImplementationPath, "*.dll", SearchOption.AllDirectories)
                    .Union(io.GetFilesInDirectory(installation.DocumentationPath, "*.xml", SearchOption.AllDirectories))
                )
                // this method needs to be revisited. See https://github.com/dotnet/runtime/issues/47029
                .Where(assembly => assembly.AssemblyName.StartsWith("System") && !assembly.AssemblyName.Contains("Native"))
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
