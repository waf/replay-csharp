using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;

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
                    .Where(dll => Path.GetFileName(dll).StartsWith("System"))
                    .Select(path => MetadataReference.CreateFromFile(path))
                    .ToArray();
            });

        public static IReadOnlyCollection<string> DefaultUsings =
            new[] { "System", "System.Collections.Generic", "System.Linq", "System.Text" };
    }
}
