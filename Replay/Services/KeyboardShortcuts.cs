using System.Windows.Input;
using static System.Windows.Input.ModifierKeys;
using static System.Windows.Input.Key;
using static Replay.Services.ReplCommand;

namespace Replay.Services
{
    enum ReplCommand
    {
        EvaluateCurrentLine,
        ReevaluateCurrentLine,
        OpenIntellisense,
        GoToFirstLine,
        GoToLastLine,
        LineDown,
        LineUp
    }

    /// <summary>
    /// Maps keyboard keys to REPL commands
    /// </summary>
    static class KeyboardShortcuts
    {
        public static ReplCommand? MapToCommand(KeyEventArgs key)
        {
            if (key.Is(Enter))
            {
                key.Handled = true;
                return EvaluateCurrentLine;
            }
            else if (key.Is(Control, Enter))
            {
                key.Handled = true;
                return ReevaluateCurrentLine;
            }
            else if (key.Is(Control, Space) || key.Is(Tab))
            {
                key.Handled = true;
                return OpenIntellisense;
            }
            else if (key.Is(PageUp) || key.Is(Control, Up))
            {
                key.Handled = true;
                return GoToFirstLine;
            }
            else if (key.Is(PageDown) || key.Is(Control, Down))
            {
                key.Handled = true;
                return GoToLastLine;
            }
            else if (key.Is(Up))
            {
                return LineUp;
            }
            else if (key.Is(Down))
            {
                return LineDown;
            }

            return null;
        }

        private static bool Is(this KeyEventArgs args, Key test) =>
            args.Key == test && Keyboard.Modifiers == ModifierKeys.None;

        private static bool Is(this KeyEventArgs args, ModifierKeys modifier, Key test) =>
            args.Key == test && Keyboard.Modifiers.HasFlag(modifier);
    }

}
