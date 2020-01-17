using System;
using System.Collections.Generic;
using System.Windows.Input;
using static Replay.Services.ReplCommand;
using static System.Windows.Input.Key;
using static System.Windows.Input.ModifierKeys;

namespace Replay.Services
{
    public enum ReplCommand
    {
        EvaluateCurrentLine,
        ReevaluateCurrentLine,
        CyclePreviousLine,
        CycleNextLine,
        OpenIntellisense,
        GoToFirstLine,
        GoToLastLine,
        LineDown,
        LineUp,
        ClearScreen,
        CancelLine,
        SaveSession,
        SmartPaste,
    }

    /// <summary>
    /// Maps keyboard keys to commands in the REPL
    /// </summary>
    public static class KeyboardShortcuts
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
                (Alt, Up) => CyclePreviousLine,
                (Alt, Down) => CycleNextLine,
                (Control, Space) => OpenIntellisense,
                Tab => OpenIntellisense,
                PageUp => GoToFirstLine,
                (Control, Up) => GoToFirstLine,
                PageDown => GoToLastLine,
                (Control, Down) => GoToLastLine,
                Up => LineUp,
                Down => LineDown,
                (Control, C) => CancelLine,
                (Control, S) => SaveSession,
                (Control, L) => ClearScreen,
                (Control | Shift, V) => SmartPaste,
                _ => null
            };

        public static IReadOnlyCollection<string> KeyboardShortcutHelp => new[]
        {
            @"Enter – Evaluate the current line.",
            @"Ctrl-Enter – Reevaluate the current line.",
            @"Shift-Enter – Insert a ""soft newline.""",
            @"Alt-Up or Alt-Down – Cycle through previous lines.",
            @"Ctrl-Space or Tab – Open intellisense.",
            @"PageUp or Ctrl-Up – Go to the first line of the session.",
            @"PageDown or Ctrl-Down – Go to the last line of the session.",
            @"Ctrl-L – Clear the screen.",
            @"Ctrl-S – Export your session (as a C# or Markdown file).",
            @"Ctrl-Shift-V – Paste clipboard contents and extract unbound variables.",
        };

        private static object GetPressedKey(KeyEventArgs keyEvent) =>
            keyEvent.KeyboardDevice.Modifiers == ModifierKeys.None
            ? keyEvent.Key as object
            : (keyEvent.KeyboardDevice.Modifiers, keyEvent.Key == Key.System ? keyEvent.SystemKey : keyEvent.Key);
    }
}
