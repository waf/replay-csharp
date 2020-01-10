using Replay.Services.Logging;
using Replay.Services.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Replay.Services.CommandHandlers
{
    class HelpCommandHandler : ICommandHandler
    {
        public bool CanHandle(string input) =>
            "help".Equals(input, StringComparison.OrdinalIgnoreCase);

        public Task<LineEvaluationResult> HandleAsync(Guid lineId, string text, IReplLogger logger)
        {
            var help = new[]
            {
                new[]
                {
                    "Welcome to Replay! ❤",
                    "",
                    "Keyboard Shortcuts",
                    "==================",
                },
                KeyboardShortcuts.KeyboardShortcutHelp,
                new[]
                {
                    "",
                    "Commands",
                    "========",
                },
                CommandHelp
            };

            string helpText = string.Join(Environment.NewLine, help.SelectMany(linegroups => linegroups));

            return Task.FromResult(
                new LineEvaluationResult(null, helpText, null, null)
            );
        }

        private static IReadOnlyCollection<string> CommandHelp => new[]
        {
            @"#nuget mypackagename – search for and install a nuget package.",
            @"#r ""path/to/lib.dll"" – reference a DLL.",
            @"exit – exit the application.",
        };

        public static ColorSpan[] SyntaxHighlight { get; } =
            new[] { new ColorSpan(Color.FromRgb(80, 250, 123), 0, 4) };
    }
}
