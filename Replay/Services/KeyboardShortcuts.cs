using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static Replay.Services.ReplCommand;
using static System.Windows.Input.Key;
using static System.Windows.Input.ModifierKeys;

namespace Replay.Services
{
    enum ReplCommand
    {
        EvaluateCurrentLine,
        ReevaluateCurrentLine,
        DuplicatePreviousLine,
        OpenIntellisense,
        GoToFirstLine,
        GoToLastLine,
        LineDown,
        LineUp
    }

    /// <summary>
    /// Maps keyboard keys to commands in the REPL
    /// </summary>
    static class KeyboardShortcuts
    {
        /// <summary>
        /// Given a key event, map it to a command in the REPL, or null if
        /// the key event does not correspond to a command in the REPL.
        /// </summary>
        public static ReplCommand? MapToCommand(KeyEventArgs keyEvent) =>
            GetPressedKey(keyEvent) switch
            {
                Enter => EvaluateCurrentLine,
                (Control, Enter) => ReevaluateCurrentLine,
                (Alt, Up) => DuplicatePreviousLine,
                (Control, Space) => OpenIntellisense,
                Tab => OpenIntellisense,
                PageUp => GoToFirstLine,
                (Control, Up) => GoToFirstLine,
                PageDown => GoToLastLine,
                (Control, Down) => GoToLastLine,
                Up => LineUp,
                Down => LineDown,
                _ => null
            };

        public static IReadOnlyCollection<string> KeyboardShortcutHelp => new[]
        {
            @"Enter – Evaluate the current line.",
            @"Ctrl-Enter – Reevaluate the current line.",
            @"Alt-Up – Duplicate the previous line.",
            @"Shift-Enter – Insert a ""soft newline.""",
            @"Ctrl-Space or Tab – Open intellisense.",
            @"PageUp or Ctrl-Up – Go to the first line of the session.",
            @"PageDown or Ctrl-Down – Go to the last line of the session.",
        };

        private static object GetPressedKey(KeyEventArgs keyEvent) =>
            Keyboard.Modifiers == ModifierKeys.None
            ? keyEvent.Key as object
            : (Keyboard.Modifiers, keyEvent.Key == Key.System ? keyEvent.SystemKey : keyEvent.Key);
    }
}
