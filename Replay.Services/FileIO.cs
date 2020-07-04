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
    public interface IFileIO
    {
        PortableExecutableReference CreateMetadataReferenceFromFile(string filepath, MetadataReferenceProperties properties = default, DocumentationProvider documentation = null);
        PortableExecutableReference CreateMetadataReferenceWithDocumentation(AssemblyWithXmlDocumentation assembly);
        string[] GetFilesInDirectory(string path, string searchPattern, SearchOption searchOption);
        string[] GetDirectories(string path);
        string[] GetDirectories(string path, string searchPattern);
        XmlDocumentationProvider CreateDocumentationFromXmlFile(string xmlDocCommentFilePath);
        string GetFullFileSystemPath(string path);
        bool DoesFileExist(string path);
        Task WriteAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default);
    }

    public class RealFileIO : IFileIO
    {
        public virtual XmlDocumentationProvider CreateDocumentationFromXmlFile(string xmlDocCommentFilePath) =>
            XmlDocumentationProvider.CreateFromFile(xmlDocCommentFilePath);

        public virtual PortableExecutableReference CreateMetadataReferenceFromFile(string filepath, MetadataReferenceProperties properties = default, DocumentationProvider documentation = null) =>
            MetadataReference.CreateFromFile(filepath, properties, documentation);

        public virtual PortableExecutableReference CreateMetadataReferenceWithDocumentation(AssemblyWithXmlDocumentation assembly) =>
            assembly.FullXmlDocumentationPath is null
                ? CreateMetadataReferenceFromFile(assembly.FullAssemblyPath)
                : CreateMetadataReferenceFromFile(assembly.FullAssemblyPath,
                    documentation: CreateDocumentationFromXmlFile(assembly.FullXmlDocumentationPath)
                  );

        public virtual bool DoesFileExist(string path) =>
            File.Exists(path);

        public virtual string[] GetDirectories(string path) =>
            Directory.GetDirectories(path);

        public virtual string[] GetDirectories(string path, string searchPattern) =>
            Directory.GetDirectories(path, searchPattern);

        public virtual string[] GetFilesInDirectory(string path, string searchPattern, SearchOption searchOption) =>
            Directory.GetFiles(path, searchPattern, searchOption);

        public virtual string GetFullFileSystemPath(string path) =>
            Path.GetFullPath(path);

        public virtual Task WriteAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default) =>
            File.WriteAllLinesAsync(path, contents, encoding, cancellationToken);
    }
}
