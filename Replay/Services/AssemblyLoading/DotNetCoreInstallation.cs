namespace Replay.Services.AssemblyLoading
{
    public class DotNetCoreInstallation
    {
        public DotNetCoreInstallation(string basePath, string version)
        {
            BasePath = basePath;
            Version = version;
        }

        public string BasePath { get; }
        public string Version { get; }
    }
}
