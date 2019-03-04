using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Replay.Services
{
    class DefaultAssemblies
    {
        public static Lazy<IReadOnlyCollection<MetadataReference>> Assemblies =
            new Lazy<IReadOnlyCollection<MetadataReference>>(() =>
            {
                if (!(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string assemblies))
                {
                    throw new PlatformNotSupportedException("Could not find trusted platform assemblies");
                }

                return assemblies
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(path => MetadataReference.CreateFromFile(path))
                    .ToArray();
            });

        public static IReadOnlyCollection<string> DefaultUsings =
            new[] { "System", "System.Collections.Generic", "System.Linq", "System.Text" };
    }
}
