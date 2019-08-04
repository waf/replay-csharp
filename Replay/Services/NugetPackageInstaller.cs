using Microsoft.CodeAnalysis;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Replay.Services
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
        private readonly List<SourceRepository> repositories;
        private readonly SourceCacheContext nugetCache;
        private readonly PackagePathResolver packagePathResolver;
        private readonly string globalPackageFolder;
        private readonly FrameworkReducer frameworkReducer;
        private readonly ClientPolicyContext clientPolicy;

        public NugetPackageInstaller()
        {
            var nugetSettings = Settings.LoadDefaultSettings(root: null);
            var sourceRepositoryProvider = new SourceRepositoryProvider(nugetSettings, Repository.Provider.GetCoreV3());

            this.nugetFramework = NuGetFramework.ParseFolder("netstandard2.0");
            this.repositories = sourceRepositoryProvider.GetRepositories().ToList();
            this.nugetCache = new SourceCacheContext();
            this.packagePathResolver = new PackagePathResolver(Path.GetFullPath("packages"));
            this.globalPackageFolder = SettingsUtility.GetGlobalPackagesFolder(nugetSettings);
            this.frameworkReducer = new FrameworkReducer();
            this.clientPolicy = ClientPolicyContext.GetClientPolicy(nugetSettings, NuGet.Common.NullLogger.Instance);
        }

        public async Task<IReadOnlyCollection<MetadataReference>> Install(string packageSearchTerm, IReplLogger logger)
        {
            var nugetLogger = new NugetLogger(logger);
            var package = await FindPackageAsync(packageSearchTerm, repositories, nugetLogger);

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
                        : new PackageFolderReader(installedPath);

                    var allResources = await Task.WhenAll(
                        packageReader.GetLibItemsAsync(CancellationToken.None),
                        packageReader.GetFrameworkItemsAsync(CancellationToken.None)
                    );

                    installedPath = installedPath ?? packagePathResolver.GetInstalledPath(packageToInstall);
                    return FilterByFramework(frameworkReducer, allResources.SelectMany(x => x))
                        .Where(item => item.EndsWith(".dll"))
                        .Select(item => MetadataReference.CreateFromFile(Path.Combine(installedPath, item)))
                        .ToList();
                })
            );

            return assemblyPaths
                .SelectMany(assemblyGroup => assemblyGroup)
                .ToList();
        }

        private static async Task<PackageIdentity> FindPackageAsync(
            string packageSearch,
            IReadOnlyCollection<SourceRepository> repositories,
            NugetLogger nugetLogger)
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
            ILogger instance, List<SourceRepository> repositories)
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
            ConcurrentDictionary<SourcePackageDependencyInfo, byte> dependencies)
        {
            if (dependencies.Keys.Contains(package)) return;

            await Task.WhenAll(repositories.Select(async repository =>
            {
                var dependencyInfoResource = await repository.GetResourceAsync<DependencyInfoResource>();
                var dependencyInfo = await dependencyInfoResource.ResolvePackage(
                    package, framework, cacheContext, logger, CancellationToken.None);

                if (dependencyInfo == null) return;

                dependencies[dependencyInfo] = 1;

                await Task.WhenAll(dependencyInfo.Dependencies.Select(dependency =>
                    GetPackageDependencies(
                        new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion),
                        framework, cacheContext, logger, repositories, dependencies)
                ));
            }));
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
            var downloadResource = await packageToInstall.Source.GetResourceAsync<DownloadResource>(CancellationToken.None);
            using (var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                packageToInstall,
                new PackageDownloadContext(nugetCache),
                globalPackageFolder,
                nugetLogger, CancellationToken.None))
            {
                await PackageExtractor.ExtractPackageAsync(
                    downloadResult.PackageSource,
                    downloadResult.PackageStream,
                    packagePathResolver,
                    packageExtractionContext,
                    CancellationToken.None);

                return downloadResult.PackageReader;
            }
        }

        private List<string> FilterByFramework(FrameworkReducer frameworkReducer, IEnumerable<FrameworkSpecificGroup> libItems)
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
