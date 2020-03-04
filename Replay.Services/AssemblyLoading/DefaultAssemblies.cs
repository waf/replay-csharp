using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Replay.Services.AssemblyLoading
{
    public class DefaultAssemblies
    {
        public DefaultAssemblies(DotNetAssemblyLocator assemblyLocator)
        {
            this.Assemblies = new Lazy<IReadOnlyCollection<MetadataReference>>(
                () => assemblyLocator.GetDefaultAssemblies()
            );
            this.DefaultUsings = new[] {
                "System", "System.Collections.Generic", "System.Linq", "System.Text"
            };
        }

        public Lazy<IReadOnlyCollection<MetadataReference>> Assemblies { get; }
        public IReadOnlyCollection<string> DefaultUsings { get; }
    }
}
