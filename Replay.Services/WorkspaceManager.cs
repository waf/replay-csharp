using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Replay.Services.AssemblyLoading;
using Replay.Services.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Replay.Services
{
    /// <summary>
    /// Some Roslyn APIs like code completion and syntax highligher operate on the
    /// Document/Project/Workspace object model. This class keeps track of the state
    /// of the object model for a single REPL window, so we can interact with those APIs.
    /// </summary>
    class WorkspaceManager
    {
        private readonly ConcurrentDictionary<int, ReplSubmission> EditorToSubmission = new ConcurrentDictionary<int, ReplSubmission>();
        private readonly AdhocWorkspace workspace;
        private readonly CSharpCompilationOptions compilationOptions;
        private readonly DefaultAssemblies defaultAssemblies;

        public WorkspaceManager(DefaultAssemblies defaultAssemblies)
        {
            var host = MefHostServices.Create(MefHostServices.DefaultAssemblies);
            this.workspace = new AdhocWorkspace(host);
            this.defaultAssemblies = defaultAssemblies;
            this.compilationOptions = new CSharpCompilationOptions(
               OutputKind.DynamicallyLinkedLibrary,
               usings: defaultAssemblies.DefaultUsings
            );
        }

        /// <summary>
        /// Track the code in our workspace.
        /// </summary>
        /// <param name="lineId">
        /// The line id. Each successive line should increment by one.
        /// Each line will have a reference back to earlier lines.
        /// </param>
        /// <param name="code">
        /// The C# code to track
        /// </param>
        /// <param name="persistent">
        /// Whether or not the supplied code should be persisted to our workspace.
        /// This is usually true, but there are some "throw-away" cases like syntax
        /// highlighting of potential input we don't want to persist.
        /// </param>
        /// <param name="assemblyReferences">
        /// Assembly references to include with this code. Assembly references provided
        /// for earlier code submissions will be automatically referenced.
        /// </param>
        public ReplSubmission CreateOrUpdateSubmission(
            int lineId,
            string code = "",
            bool persistent = true,
            params MetadataReference[] assemblyReferences)
        {
            var replSubmission = EditorToSubmission.TryGetValue(lineId, out var previousSubmission)
                ? UpdateSubmission(previousSubmission, code, assemblyReferences)
                : CreateSubmission(lineId, code, assemblyReferences);

            if(persistent)
            {
                EditorToSubmission[lineId] = replSubmission;
            }
            return replSubmission;
        }

        private ReplSubmission CreateSubmission(int lineId, string code, MetadataReference[] assemblyReferences)
        {
            var name = "Script" + lineId;
            // we add the previous REPL submission as a project reference, so
            // APIs like Code Completion know about them.
            var projectReferences = GetPreviousSubmission(lineId);
            Project project = CreateProject(name, projectReferences, assemblyReferences);
            Document document = CreateDocument(project, name, code);

            return new ReplSubmission(code, document);
        }

        private Project CreateProject(string name, ProjectReference[] previousSubmission, MetadataReference[] assemblyReferences)
        {
            var projectInfo = ProjectInfo
                .Create(
                    id: ProjectId.CreateNewId(),
                    version: VersionStamp.Create(),
                    name: name,
                    assemblyName: name,
                    language: LanguageNames.CSharp,
                    isSubmission: true
                )
                .WithProjectReferences(previousSubmission)
                .WithMetadataReferences(defaultAssemblies.Assemblies.Value.Concat(assemblyReferences))
                .WithCompilationOptions(compilationOptions);
            var project = workspace.AddProject(projectInfo);
            return project;
        }

        private Document CreateDocument(Project project, string name, string code)
        {
            var documentInfo = DocumentInfo.Create(
                id: DocumentId.CreateNewId(project.Id),
                name: name,
                sourceCodeKind: SourceCodeKind.Script,
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From(code), VersionStamp.Create())));
            var document = workspace.AddDocument(documentInfo);
            return document;
        }

        private ProjectReference[] GetPreviousSubmission(int lineId)
        {
            return lineId > 1
                ? new[] { new ProjectReference(EditorToSubmission[lineId - 1].Document.Project.Id) }
                : Array.Empty<ProjectReference>();
        }

        private ReplSubmission UpdateSubmission(
            ReplSubmission replSubmission,
            string code,
            IReadOnlyCollection<MetadataReference> references)
        {
            var edit = workspace.CurrentSolution
                .WithDocumentText(replSubmission.Document.Id, SourceText.From(code))
                .AddMetadataReferences(replSubmission.Document.Project.Id, references);

            var success = workspace.TryApplyChanges(edit);

            // document has changed, requery to get the new one
            var document = workspace.CurrentSolution.GetDocument(replSubmission.Document.Id);

            return new ReplSubmission(code, document);
        }

        public SessionSnapshot GetHistoricalSnapshot()
        {
            static UsingDirectiveSyntax ParseUsing(string type) =>
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(type)).NormalizeWhitespace();

            var usings = this.compilationOptions
                .Usings
                .Select(ParseUsing)
                .ToList();

            return new SessionSnapshot(usings, this.EditorToSubmission);
        }
    }

    public class SessionSnapshot
    {
        public SessionSnapshot(IReadOnlyCollection<UsingDirectiveSyntax> initialUsingDirectives, IReadOnlyDictionary<int, ReplSubmission> submissions)
        {
            InitialUsingDirectives = initialUsingDirectives;
            Submissions = submissions;
        }

        public IReadOnlyCollection<UsingDirectiveSyntax> InitialUsingDirectives { get; }
        public IReadOnlyDictionary<int, ReplSubmission> Submissions { get; }
    }
}
