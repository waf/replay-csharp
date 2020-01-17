using Microsoft.CodeAnalysis;
using Replay.Services.AssemblyLoading;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Replay.Services
{
    /// <summary>
    /// Quick and dirty way of indirecting File IO for testability
    /// </summary>
    public class FileIO
    {
        public static FileIO RealIO = CreateRealIO();

        public static FileIO CreateRealIO()
        {
            var io = new FileIO
            {
                GetFilesInDirectory = Directory.GetFiles,
                DoesFileExist = File.Exists,
                GetFullFileSystemPath = Path.GetFullPath,
                WriteAllLinesAsync = File.WriteAllLinesAsync,
                CreateMetadataReferenceFromFile = MetadataReference.CreateFromFile,
                CreateDocumentationFromXmlFile = XmlDocumentationProvider.CreateFromFile,
            };
            io.CreateMetadataReferenceWithDocumentation =
                (AssemblyWithXmlDocumentation assembly) => assembly.FullXmlDocumentationPath is null
                    ? io.CreateMetadataReferenceFromFile(assembly.FullAssemblyPath)
                    : io.CreateMetadataReferenceFromFile(assembly.FullAssemblyPath, documentation: io.CreateDocumentationFromXmlFile(assembly.FullXmlDocumentationPath));
            return io;
        }

        public GetFilesInDirectory GetFilesInDirectory { get; set; }
        public DoesFileExist DoesFileExist { get; set; }
        public GetFullFileSystemPath GetFullFileSystemPath { get; set; }
        public WriteAllLinesAsync WriteAllLinesAsync { get; set; }
        public CreateMetadataReferenceFromFile CreateMetadataReferenceFromFile { get; set; }
        public CreateDocumentationFromXmlFile CreateDocumentationFromXmlFile { get; set; }
        public CreateMetadataReferenceWithDocumentation CreateMetadataReferenceWithDocumentation { get; set; }
    }

    public delegate PortableExecutableReference CreateMetadataReferenceFromFile(string filepath, MetadataReferenceProperties properties = default, DocumentationProvider documentation = null);
    public delegate PortableExecutableReference CreateMetadataReferenceWithDocumentation(AssemblyWithXmlDocumentation assembly);
    public delegate string[] GetFilesInDirectory(string path, string searchPattern, SearchOption searchOption);
    public delegate XmlDocumentationProvider CreateDocumentationFromXmlFile(string xmlDocCommentFilePath);
    public delegate string GetFullFileSystemPath(string path);
    public delegate bool DoesFileExist(string path);
    public delegate Task WriteAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default);
}
