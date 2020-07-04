using Replay.Services.AssemblyLoading;
using Replay.Services.CommandHandlers;
using Replay.Services.Logging;
using Replay.Services.Model;
using Replay.Services.Nuget;
using Replay.Services.SessionSavers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Replay.Services
{
    public interface IReplServices
    {
        Task<LineEvaluationResult> AppendEvaluationAsync(Guid lineId, string code, IReplLogger logger);
        Task<IReadOnlyList<ReplCompletion>> CompleteCodeAsync(Guid lineId, string code, int caretIndex);
        Task<IReadOnlyList<string>> GetSupportedSaveFormats();
        Task<IReadOnlyCollection<string>> GetUnboundVariables(Guid lineId, string code);
        Task<IReadOnlyCollection<ColorSpan>> HighlightAsync(Guid lineId, string code);
        Task<string> SaveSessionAsync(string filename, string fileFormat, IReadOnlyCollection<LineToSave> linesToSave);

        event EventHandler<UserConfiguration> UserConfigurationLoaded;
    }

    /// <summary>
    /// Main access point for editor services.
    /// Manages background initialization for all services in a way that doesn't increase startup time.
    /// This is a stateful service (due to the contained WorkspaceManager) and is one-per-window.
    /// </summary>
    public class ReplServices : IReplServices
    {
        private readonly Task requiredInitialization;
        private readonly Task commandInitialization;
        private SyntaxHighlighter syntaxHighlighter;
        private ScriptEvaluator scriptEvaluator;
        private CodeCompleter codeCompleter;
        private WorkspaceManager workspaceManager;
        private DataFlowAnalyzer dataFlowAnalyzer;
        private IReadOnlyCollection<ICommandHandler> commandHandlers;
        private IReadOnlyCollection<ISessionSaver> savers;

        public event EventHandler<UserConfiguration> UserConfigurationLoaded;

        public ReplServices(IFileIO io)
        {
            // some of the initialization can be heavy, and causes slow startup time for the UI.
            // run it in a background thread so the UI can render immediately.
            requiredInitialization = Task.WhenAll(
                Task.Run(() =>
                {
                    this.syntaxHighlighter = new SyntaxHighlighter("Themes/theme.vssettings");
                    UserConfigurationLoaded?.Invoke(this, new UserConfiguration
                    (
                        syntaxHighlighter.BackgroundColor,
                        syntaxHighlighter.ForegroundColor
                    ));
                    this.codeCompleter = new CodeCompleter();
                }),
                Task.Run(() =>
                {
                    var assemblies = new DefaultAssemblies(new DotNetAssemblyLocator(io));
                    this.scriptEvaluator = new ScriptEvaluator(assemblies);
                    this.workspaceManager = new WorkspaceManager(assemblies);
                })
            );
            commandInitialization = Task.Run(async () =>
            {
                await requiredInitialization;

                this.commandHandlers = new ICommandHandler[]
                {
                    new ExitCommandHandler(),
                    new HelpCommandHandler(),
                    new AssemblyReferenceCommandHandler(scriptEvaluator, workspaceManager, io),
                    new NugetReferenceCommandHandler(scriptEvaluator, workspaceManager, new NugetPackageInstaller(io)),
                    new EvaluationCommandHandler(scriptEvaluator, workspaceManager, new PrettyPrinter())
                };

                this.savers = new ISessionSaver[]
                {
                    new CSharpSessionSaver(io, workspaceManager),
                    new MarkdownSessionSaver(io),
                };

                this.dataFlowAnalyzer = new DataFlowAnalyzer();
            });
        }

        public async Task<IReadOnlyList<ReplCompletion>> CompleteCodeAsync(Guid lineId, string code, int caretIndex)
        {
            await requiredInitialization.ConfigureAwait(false);
            var submission = workspaceManager.CreateOrUpdateSubmission(lineId, code, speculative: true);
            return await codeCompleter.Complete(submission, caretIndex).ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ColorSpan>> HighlightAsync(Guid lineId, string code)
        {
            await requiredInitialization.ConfigureAwait(false);
            var submission = workspaceManager.CreateOrUpdateSubmission(lineId, code, speculative: true);
            return await syntaxHighlighter.HighlightAsync(submission).ConfigureAwait(false);
        }

        public async Task<LineEvaluationResult> AppendEvaluationAsync(Guid lineId, string code, IReplLogger logger)
        {
            await commandInitialization.ConfigureAwait(false);
            try
            {
                var result = await commandHandlers
                    .First(handler => handler.CanHandle(code))
                    .HandleAsync(lineId, code, logger)
                    .ConfigureAwait(false);

                // Depending on which command was run (e.g. 'help'), we might not have a
                // corresponding entry in our workspace. The following line will create an
                // empty record if that's the case. Everything's easier to reason about if
                // the viewmodel and the workspace have the same number of lines.
                workspaceManager.EnsureRecordForLine(lineId);

                return result;
            }
            catch (Exception ex)
            {
                return new LineEvaluationResult(code, null, "Error: " + ex.Message, null);
            }
        }

        public async Task<string> SaveSessionAsync(string filename, string fileFormat, IReadOnlyCollection<LineToSave> linesToSave)
        {
            await commandInitialization.ConfigureAwait(false);
            try
            {
                return await savers
                    .First(saver => saver.SaveFormat == fileFormat)
                    .SaveAsync(filename, linesToSave)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return "Saving the session has failed. " + ex.Message;
            }
        }

        public async Task<IReadOnlyList<string>> GetSupportedSaveFormats()
        {
            await commandInitialization.ConfigureAwait(false);
            return this.savers.Select(s => s.SaveFormat).ToList();
        }

        public async Task<IReadOnlyCollection<string>> GetUnboundVariables(Guid lineId, string code)
        {
            await commandInitialization.ConfigureAwait(false);

            var submission = workspaceManager.CreateOrUpdateSubmission(lineId, code, speculative: true);
            return await this.dataFlowAnalyzer.GetUnboundVariables(submission);
        }

    }
}
