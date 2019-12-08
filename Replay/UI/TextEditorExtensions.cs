using ICSharpCode.AvalonEdit;
using Replay.Model;
using System;

namespace Replay.UI
{
    static class TextEditorExtensions
    {
        public static LineEditorViewModel ViewModel(this TextEditor editor) =>
            (LineEditorViewModel)editor.DataContext;

        public static bool IsCaretOnFirstLine(this LineEditorViewModel editor) =>
            !editor.Document.Text.Substring(0, editor.SelectionStart).Contains(Environment.NewLine);

        public static bool IsCaretOnFinalLine(this LineEditorViewModel editor) =>
            !editor.Document.Text.Substring(editor.SelectionStart).Contains(Environment.NewLine);
    }
}
