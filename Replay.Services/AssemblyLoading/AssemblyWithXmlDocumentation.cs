
namespace Replay.Services.AssemblyLoading
{
    public class AssemblyWithXmlDocumentation
    {
        public AssemblyWithXmlDocumentation(string assemblyName, string fullAssemblyPath, string fullXmlDocumentationPath)
        {
            AssemblyName = assemblyName;
            FullAssemblyPath = fullAssemblyPath;
            FullXmlDocumentationPath = fullXmlDocumentationPath;
        }

        public string AssemblyName { get; }
        public string FullAssemblyPath { get; }
        public string FullXmlDocumentationPath { get; }
    }
}
