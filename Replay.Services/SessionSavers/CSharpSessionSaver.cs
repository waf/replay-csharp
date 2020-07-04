using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Replay.Services.SessionSavers
{
    /// <summary>
    /// Saves the user's session as a C# file, with
    /// the REPL lines embedded in a main method.
    /// </summary>
    class CSharpSessionSaver : ISessionSaver
    {
        private readonly IFileIO io;
        private readonly WorkspaceManager workspaceManager;

        public CSharpSessionSaver(IFileIO io, WorkspaceManager workspaceManager)
        {
            this.io = io;
            this.workspaceManager = workspaceManager;
        }

        public string SaveFormat { get; } = "C# File (*.cs)|*.cs";

        public async Task<string> SaveAsync(string fileName, IReadOnlyCollection<LineToSave> linesToSave)
        {
            var snapshot = workspaceManager.GetHistoricalSnapshot();
            var allNodes = await GetNodes(snapshot);

            var usings = GetUsingDirectives(snapshot, allNodes);

            // our placeholder program - we use a placeholder because it's unlikely all the
            // expressions a user can put into the REPL will be embeddable into a main method.
            // So we use a string placeholder replacement, for a "best effort" approach.
            const string PlaceholderText = "var PLACEHOLDER = 42;";
            var compilation = BuildPlaceholderFile(usings, PlaceholderText);
            var document = CreateDocument(compilation);

            // the contents to swap into the placeholder
            var statements = CreateStringFromStatements(allNodes.Except(usings));

            // do the swap
            var newText = compilation.ToFullString().Replace(PlaceholderText, statements);
            document = document.WithText(SourceText.From(newText));

            // make it pretty
            var formatted = await Formatter.FormatAsync(document);
            var text = await formatted.GetTextAsync();

            // write to file
            await io.WriteAllLinesAsync(fileName, new[] { text.ToString() }, Encoding.UTF8);

            return "Session has been saved as a C# file. Please note that not everything in a REPL "
                + "translates nicely to a C# file, so you may need to fix up the file.";
        }

        private static string CreateStringFromStatements(IEnumerable<SyntaxNode> statementNodes)
        {
            return string.Join(Environment.NewLine,
                statementNodes
                .Select(node => node switch
                {
                    // plain identifier statements are not valid in our main method file. Convert to comment.
                    GlobalStatementSyntax { Statement: ExpressionStatementSyntax { Expression: IdentifierNameSyntax _ } }
                        => Comment($"/* {node.ToFullString()} */").ToFullString(),

                    // otherwise, pass through unchanged
                    _ => node.ToFullString()
                })
            );
        }

        private static Document CreateDocument(CompilationUnitSyntax compilation)
        {
            var workspace = new AdhocWorkspace();
            var project = workspace.AddProject("ReplayProject", LanguageNames.CSharp);
            var document = project.AddDocument("Program.cs", compilation);
            return document;
        }

        private static async Task<List<SyntaxNode>> GetNodes(SessionSnapshot snapshot)
        {
            var replHistory = snapshot
                .Submissions
                .Skip(1) //skip the first because it's our warmup
                .Select(s => s.Document.GetSyntaxRootAsync());

            return (await Task.WhenAll(replHistory))
                .SelectMany(d => d.ChildNodes())
                .ToList();
        }

        private static CompilationUnitSyntax BuildPlaceholderFile(UsingDirectiveSyntax[] usings, string placeholder)
        {
            var methodDeclaration = MethodDeclaration(ParseTypeName("void"), "Main")
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .AddBodyStatements(ParseStatement(placeholder));

            var classDeclaration = ClassDeclaration("Program")
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddMembers(methodDeclaration);

            var namespaceDeclaration = NamespaceDeclaration(ParseName("Replay"))
                .AddMembers(classDeclaration);

            return CompilationUnit()
                .AddUsings(usings)
                .AddMembers(namespaceDeclaration)
                .NormalizeWhitespace();
        }

        private static UsingDirectiveSyntax[] GetUsingDirectives(SessionSnapshot snapshot, List<SyntaxNode> allNodes)
        {
            return allNodes
                .OfType<UsingDirectiveSyntax>()
                .Concat(snapshot.InitialUsingDirectives)
                .Distinct()
                .ToArray();
        }
    }
}
