using Microsoft.CodeAnalysis;
using Replay.Services.AssemblyLoading;
using System.IO;

namespace Replay.Services
{
    /// <summary>
    /// Quick and dirty way of indirecting File IO for testability
    /// </summary>
    public class FileIO
    {
        public FileIO()
        {
            GetFilesInDirectory = Directory.GetFiles;
            GetFullFileSystemPath = Path.GetFullPath;
            CreateMetadataReferenceFromFile = MetadataReference.CreateFromFile;
            CreateDocumentationFromXmlFile = XmlDocumentationProvider.CreateFromFile;
            CreateMetadataReferenceWithDocumentation =
                (AssemblyWithXmlDocumentation assembly) => assembly.FullXmlDocumentationPath is null
                    ? CreateMetadataReferenceFromFile(assembly.FullAssemblyPath)
                    : CreateMetadataReferenceFromFile(assembly.FullAssemblyPath, documentation: CreateDocumentationFromXmlFile(assembly.FullXmlDocumentationPath));
        }

        public CreateMetadataReferenceFromFile CreateMetadataReferenceFromFile { get; set; }
        public CreateMetadataReferenceWithDocumentation CreateMetadataReferenceWithDocumentation { get; set; }
        public CreateDocumentationFromXmlFile CreateDocumentationFromXmlFile { get; set; }
        public GetFilesInDirectory GetFilesInDirectory { get; set; }
        public GetFullFileSystemPath GetFullFileSystemPath { get; set; }
    }


    public delegate PortableExecutableReference CreateMetadataReferenceFromFile(string filepath, MetadataReferenceProperties properties = default, DocumentationProvider documentation = null);
    public delegate PortableExecutableReference CreateMetadataReferenceWithDocumentation(AssemblyWithXmlDocumentation assembly);
    public delegate string[] GetFilesInDirectory(string path, string searchPattern, SearchOption searchOption);
    public delegate XmlDocumentationProvider CreateDocumentationFromXmlFile(string xmlDocCommentFilePath);
    public delegate string GetFullFileSystemPath(string path);
}
