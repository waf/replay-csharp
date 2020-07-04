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
        // our datastructure is a dictionary of "line id" => "LinkedListNodes of submissions"
        // This is a convenient structure because:
        //    - We can retrieve repl submissions directly by id, useful when the user updates code on a line.
        //    - There's a concept of order (via linked list nodes next/prev), which is required for correct code evaluation.
        // As a side-benefit, both of the above cases are O(1) as well as line insertion/deletion.
        private readonly ConcurrentDictionary<Guid, LinkedListNode<ReplSubmission>> EditorToSubmission;
        private readonly LinkedList<ReplSubmission> OrderedSubmissions;

        // dependencies
        private readonly AdhocWorkspace workspace;
        private readonly CSharpCompilationOptions compilationOptions;
        private readonly DefaultAssemblies defaultAssemblies;

        public WorkspaceManager(DefaultAssemblies defaultAssemblies)
        {
            EditorToSubmission = new ConcurrentDictionary<Guid, LinkedListNode<ReplSubmission>>();
            OrderedSubmissions = new LinkedList<ReplSubmission>();
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
            Guid lineId,
            string code = "",
            bool speculative = false,
            params MetadataReference[] assemblyReferences)
        {
            bool alreadyTracked = EditorToSubmission.TryGetValue(lineId, out var previousSubmission);

            if(alreadyTracked)
            {
                if(previousSubmission.Value.Code == code && !assemblyReferences.Any())
                {
                    return previousSubmission.Value;
                }
                var updatedSubmission = UpdateSubmission(lineId, previousSubmission.Value, code, assemblyReferences, speculative);
                if (speculative)
                {
                    return updatedSubmission;
                }

                previousSubmission.Value = updatedSubmission;
                return updatedSubmission;
            }

            var replSubmission = CreateSubmission(lineId, code, assemblyReferences);

            if(speculative)
            {
                return replSubmission;
            }

            var node = new LinkedListNode<ReplSubmission>(replSubmission);
            OrderedSubmissions.AddLast(node);
            EditorToSubmission[lineId] = node;

            return replSubmission;
        }

        private ReplSubmission CreateSubmission(Guid lineId, string code, MetadataReference[] assemblyReferences)
        {
            var name = "Script" + lineId;
            // we add the previous REPL submission as a project reference, so
            // APIs like Code Completion know about them.
            var projectReferences = GetPreviousSubmission(null);
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

        internal void EnsureRecordForLine(Guid lineId)
        {
            if(!EditorToSubmission.TryGetValue(lineId, out _))
            {
                var replSubmission = CreateSubmission(lineId, string.Empty, Array.Empty<MetadataReference>());
                var node = new LinkedListNode<ReplSubmission>(replSubmission);
                OrderedSubmissions.AddLast(node);
                EditorToSubmission[lineId] = node;
            }
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

        private ProjectReference[] GetPreviousSubmission(Guid? lineId)
        {
            if(!OrderedSubmissions.Any())
            {
                return Array.Empty<ProjectReference>();
            }

            var projectId = lineId is null
                ? OrderedSubmissions.Last.Value.Document.Project.Id // append
                : EditorToSubmission[lineId.Value].Previous.Value.Document.Project.Id; // get the previous project

            return new[] { new ProjectReference(projectId) };
        }

        private ReplSubmission UpdateSubmission(
            Guid lineId,
            ReplSubmission replSubmission,
            string code,
            IReadOnlyCollection<MetadataReference> references,
            bool speculative)
        {
            var projectReferences = GetPreviousSubmission(lineId);
            var edit = workspace.CurrentSolution
                .WithDocumentText(replSubmission.Document.Id, SourceText.From(code))
                .WithProjectReferences(replSubmission.Document.Project.Id, projectReferences)
                .AddMetadataReferences(replSubmission.Document.Project.Id, references);

            if(speculative)
            {
                var speculativeDocument = edit.GetDocument(replSubmission.Document.Id);
                return new ReplSubmission(code, speculativeDocument);
            }

            _ = workspace.TryApplyChanges(edit);

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

            return new SessionSnapshot(usings, this.OrderedSubmissions.ToList());
        }

        public string Debug() =>
            string.Join("\n------------\n", OrderedSubmissions.Select(sub => sub.Code));
    }

    public class SessionSnapshot
    {
        public SessionSnapshot(IReadOnlyCollection<UsingDirectiveSyntax> initialUsingDirectives, IReadOnlyList<ReplSubmission> submissions)
        {
            InitialUsingDirectives = initialUsingDirectives;
            Submissions = submissions;
        }

        public IReadOnlyCollection<UsingDirectiveSyntax> InitialUsingDirectives { get; }
        public IReadOnlyList<ReplSubmission> Submissions { get; }
    }
}
