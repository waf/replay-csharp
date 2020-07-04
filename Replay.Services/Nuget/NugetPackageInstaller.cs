using Microsoft.CodeAnalysis;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using Replay.Services.AssemblyLoading;
using Replay.Services.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Replay.Services.Nuget
{
    /// <summary>
    /// Downloads nuget packages
    /// </summary>
    /// <remarks>
    /// https://martinbjorkstrom.com/posts/2018-09-19-revisiting-nuget-client-libraries
    /// https://daveaglick.com/posts/exploring-the-nuget-v3-libraries-part-2
    /// </remarks>
    class NugetPackageInstaller
    {
        private readonly NuGetFramework nugetFramework;
        private readonly IReadOnlyCollection<SourceRepository> repositories;
        private readonly SourceCacheContext nugetCache;
        private readonly PackagePathResolver packagePathResolver;
        private readonly string globalPackageFolder;
        private readonly FrameworkReducer frameworkReducer;
        private readonly ClientPolicyContext clientPolicy;
        private readonly IFileIO io;

        public NugetPackageInstaller(IFileIO io)
        {
            var nugetSettings = Settings.LoadDefaultSettings(root: null);
            var sourceRepositoryProvider = new SourceRepositoryProvider(new PackageSourceProvider(nugetSettings), Repository.Provider.GetCoreV3());

            this.io = io;
            this.nugetFramework = NuGetFramework.ParseFolder("netstandard2.0");
            this.repositories = sourceRepositoryProvider.GetRepositories().ToList();
            this.nugetCache = new SourceCacheContext();
            this.packagePathResolver = new PackagePathResolver(io.GetFullFileSystemPath("packages"));
            this.globalPackageFolder = SettingsUtility.GetGlobalPackagesFolder(nugetSettings);
            this.frameworkReducer = new FrameworkReducer();
            this.clientPolicy = ClientPolicyContext.GetClientPolicy(nugetSettings, NuGet.Common.NullLogger.Instance);
        }

        public async Task<IReadOnlyCollection<MetadataReference>> Install(string packageSearchTerm, IReplLogger logger)
        {
            var nugetLogger = new NugetErrorLogger(logger);
            logger.LogOutput($"Searching for {packageSearchTerm}");
            var package = await FindPackageAsync(packageSearchTerm, repositories, nugetLogger);
            if (package == null)
            {
                logger.LogError($"Could not find package '{packageSearchTerm}'");
                return Array.Empty<MetadataReference>();
            }
            logger.LogOutput("Found " + Display(package));
            logger.LogOutput("Determining dependences...");

            var dependencies = await GetPackageDependencies(package, nugetFramework, nugetCache, nugetLogger, repositories);
            var packagesToInstall = ResolvePackages(package, dependencies, nugetLogger);
            var packageExtractionContext = new PackageExtractionContext(
                PackageSaveMode.Defaultv3,
                XmlDocFileSaveMode.None,
                clientPolicy,
                nugetLogger);

            var assemblyPaths = await Task.WhenAll(packagesToInstall
                .Select(async packageToInstall =>
                {
                    var installedPath = packagePathResolver.GetInstalledPath(packageToInstall);
                    PackageReaderBase packageReader = installedPath == null
                        ? await DownloadPackage(packageToInstall, packageExtractionContext, nugetLogger)
                        : LocalPackageReader(packageToInstall, installedPath, nugetLogger);

                    var allResources = await Task.WhenAll(
                        packageReader.GetLibItemsAsync(CancellationToken.None),
                        packageReader.GetFrameworkItemsAsync(CancellationToken.None)
                    );

                    installedPath ??= packagePathResolver.GetInstalledPath(packageToInstall);
                    var references = DotNetAssemblyLocator.GroupDirectoryContentsIntoAssemblies(
                            FilterByFramework(frameworkReducer, allResources.SelectMany(x => x).ToList())
                            .Select(item => Path.Combine(installedPath, item))
                        )
                        .Select(assembly => io.CreateMetadataReferenceWithDocumentation(assembly))
                        .ToList();

                    logger.LogOutput("Installation complete for " + Display(packageToInstall));

                    return references;
                })
            );

            return assemblyPaths
                .SelectMany(assemblyGroup => assemblyGroup)
                .ToList();
        }

        private static PackageFolderReader LocalPackageReader(SourcePackageDependencyInfo packageToInstall, string installedPath, NugetErrorLogger nugetLogger)
        {
            nugetLogger.LogInformationSummary($"Using cached package {Display(packageToInstall)}");
            return new PackageFolderReader(installedPath);
        }

        private static string Display(PackageIdentity package)
        {
            string version = package.HasVersion
                ? " v" + package.Version.ToFullString()
                : "";
            return package.Id + version;
        }

        private static async Task<PackageIdentity> FindPackageAsync(
            string packageSearch,
            IReadOnlyCollection<SourceRepository> repositories,
            ILogger nugetLogger)
        {
            var results = await Task.WhenAll(repositories
                .Select(async repository =>
                {
                    PackageSearchResource searchResource = await repository.GetResourceAsync<PackageSearchResource>();
                    var searchResults = await searchResource.SearchAsync(
                        packageSearch,
                        new SearchFilter(includePrerelease: false), 0, 10,
                        nugetLogger, CancellationToken.None
                    );
                    var exactMatch = searchResults.FirstOrDefault(
                        package => package.Identity.Id.Equals(packageSearch, StringComparison.OrdinalIgnoreCase)
                    );
                    return exactMatch?.Identity;
                })
            );

            return results.FirstOrDefault(result => result != null);
        }

        private async Task<ISet<SourcePackageDependencyInfo>> GetPackageDependencies(
            PackageIdentity package, NuGetFramework nugetFramework, SourceCacheContext nugetCache,
            ILogger instance, IReadOnlyCollection<SourceRepository> repositories)
        {
            var dependencies = new ConcurrentDictionary<SourcePackageDependencyInfo, byte>(PackageIdentityComparer.Default);
            await GetPackageDependencies(package, nugetFramework, nugetCache, instance, repositories, dependencies);
            return new HashSet<SourcePackageDependencyInfo>(dependencies.Keys);
        }

        private async Task GetPackageDependencies(PackageIdentity package,
            NuGetFramework framework,
            SourceCacheContext cacheContext,
            ILogger logger,
            IReadOnlyCollection<SourceRepository> repositories,
            ConcurrentDictionary<SourcePackageDependencyInfo, byte> dependencies,
            int recursionLevel = 0)
        {
            if (dependencies.Keys.Contains(package)) return;

            foreach (var repository in repositories)
            {
                var dependencyInfoResource = await repository.GetResourceAsync<DependencyInfoResource>();
                var dependencyInfo = await dependencyInfoResource.ResolvePackage(
                    package, framework, cacheContext, logger, CancellationToken.None);

                if (dependencyInfo == null) continue;

                logger.LogInformationSummary(
                    (recursionLevel == 0 ? "" : new string('│', recursionLevel - 1))
                    + (recursionLevel == 0 ? Display(package) : $"├ {Display(package)}"));

                dependencies[dependencyInfo] = 1;

                await Task.WhenAll(dependencyInfo.Dependencies.Select(dependency =>
                    GetPackageDependencies(
                        new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion),
                        framework, cacheContext, logger, new[] { dependencyInfo.Source }, dependencies, recursionLevel + 1)
                ));

                return;
            }
        }

        private IEnumerable<SourcePackageDependencyInfo> ResolvePackages(
            PackageIdentity package,
            ISet<SourcePackageDependencyInfo> dependencies,
            ILogger nugetLogger)
        {
            var resolverContext = new PackageResolverContext(
                DependencyBehavior.Lowest,
                new[] { package.Id },
                Enumerable.Empty<string>(),
                Enumerable.Empty<PackageReference>(),
                Enumerable.Empty<PackageIdentity>(),
                dependencies,
                repositories.Select(s => s.PackageSource),
                nugetLogger);

            return new PackageResolver()
                .Resolve(resolverContext, CancellationToken.None)
                .Select(packageIdentity => dependencies
                    .Single(dependency => PackageIdentityComparer.Default.Equals(dependency, packageIdentity))
                );
        }

        private async Task<PackageReaderBase> DownloadPackage(
            SourcePackageDependencyInfo packageToInstall,
            PackageExtractionContext packageExtractionContext,
            ILogger nugetLogger)
        {
            nugetLogger.LogInformationSummary($"Downloading {Display(packageToInstall)}");

            var downloadResource = await packageToInstall.Source.GetResourceAsync<DownloadResource>(CancellationToken.None);
            using var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                packageToInstall,
                new PackageDownloadContext(nugetCache),
                globalPackageFolder,
                nugetLogger, CancellationToken.None);

            nugetLogger.LogInformationSummary($"Extracting {Display(packageToInstall)}");

            await PackageExtractor.ExtractPackageAsync(
                downloadResult.PackageSource,
                downloadResult.PackageStream,
                packagePathResolver,
                packageExtractionContext,
                CancellationToken.None);

            return downloadResult.PackageReader;
        }

        private List<string> FilterByFramework(FrameworkReducer frameworkReducer, IReadOnlyCollection<FrameworkSpecificGroup> libItems)
        {
            var nearest = frameworkReducer.GetNearest(nugetFramework, libItems.Select(x => x.TargetFramework));
            var libItemCollection = libItems
                .Where(x => x.TargetFramework.Equals(nearest))
                .SelectMany(x => x.Items)
                .ToList();
            return libItemCollection;
        }
    }
}
