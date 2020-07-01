namespace Replay.Services.AssemblyLoading
{
    public class DotNetCoreInstallation
    {
        public DotNetCoreInstallation(string implementationPath, string documentationPath)
        {
            ImplementationPath = implementationPath;
            DocumentationPath = documentationPath;
        }

        public string ImplementationPath { get; }
        public string DocumentationPath { get; }
    }
}
