using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using ReplayVisualStudioIntegration.UI;
using Task = System.Threading.Tasks.Task;

namespace ReplayVisualStudioIntegration
{
    [ProvideUIContextRule(UIContextGuid,
        "RightFileTypeOpen",
        "CSharpFileOpen",
        new[] { "CSharpFileOpen" },
        new[] { "ActiveEditorContentType:CSharp" })]
    internal sealed class ExecuteInReplayCommand
    {
        public const int CommandId = 0x0100;
        public const string UIContextGuid = "96046072-5dcd-48a7-ae44-6465fb749bae"; // unique id for controlling visibility; it's copied in the VSCT file.

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d799cb62-fc74-4c56-925a-8cad9d86b860");
        public static ExecuteInReplayCommand Instance { get; private set; }

        private readonly IOptions options;
        private readonly DTE dte;
        private readonly MessageBoxes messageBoxes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecuteInReplayCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private ExecuteInReplayCommand(IOptions options, OleMenuCommandService commandService, DTE dte, MessageBoxes messageBoxes)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.dte = dte ?? throw new ArgumentNullException(nameof(dte));
            this.messageBoxes = messageBoxes ?? throw new ArgumentNullException(nameof(messageBoxes));;

            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }


        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(ReplayVisualStudioIntegrationPackage package)
        {
            // Switch to the main thread - the call to AddCommand in ExecuteInReplayCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var services = await Task.WhenAll(
                package.GetServiceAsync(typeof(IMenuCommandService)),
                package.GetServiceAsync(typeof(DTE))
            );

            Instance = new ExecuteInReplayCommand(
                package.Options,
                services[0] as OleMenuCommandService,
                services[1] as DTE,
                new MessageBoxes()
            );
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (dte.ActiveDocument is null)
                return;

            var selection = (TextSelection)dte.ActiveDocument.Selection;
            string code = selection.Text?.Trim();

            if (string.IsNullOrEmpty(code))
                return;

            _ = ExecuteAsync(code);
        }

        public async Task ExecuteAsync(string code)
        {
            var pipe = await FindPipeAsync();

            if(pipe is null)
                return;

            await SendToPipeAsync(pipe, code);
        }

        public async Task<string> FindPipeAsync()
        {
            // Replay is already running so a pipe is available
            var pipe = ListReplayPipes().LastOrDefault();
            if (pipe != null)
                return pipe;

            // Launch Replay and wait for pipe
            if (string.IsNullOrEmpty(options.ReplayLocation) || !File.Exists(options.ReplayLocation))
            {
                messageBoxes.ReplayExeNotFound();
                return null;
            }

            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = options.ReplayLocation,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(options.ReplayLocation)
            });

            for(var i = 0; i < 10; i++)
            {
                await Task.Delay(2_000);
                var launchedPipe = ListReplayPipes().LastOrDefault();
                if (launchedPipe != null)
                    return launchedPipe;
            }

            messageBoxes.CouldNotStartReplay();
            return null;
        }

        private IReadOnlyCollection<string> ListReplayPipes() =>
            Directory
                .GetFiles(@"\\.\pipe\", "ReplaySession*")
                .Select(pipeAddress => pipeAddress.Replace(@"\\.\pipe\", string.Empty))
                .ToList();

        private async Task SendToPipeAsync(string pipe, string code)
        {
            using (var clientPipe = new NamedPipeClientStream(".", pipe, PipeDirection.Out, PipeOptions.Asynchronous))
            {
                await clientPipe.ConnectAsync();
                using (var writer = new StreamWriter(clientPipe) { AutoFlush = true })
                {
                    await writer.WriteLineAsync(code);
                }
            }
        }
    }
}
