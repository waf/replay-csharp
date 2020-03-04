using ICSharpCode.AvalonEdit;
using Replay.ViewModel;
using System;

namespace Replay.UI
{
    static class TextEditorExtensions
    {
        public static LineViewModel ViewModel(this TextEditor editor) =>
            (LineViewModel)editor.DataContext;

        public static bool IsCaretOnFirstLine(this LineViewModel editor) =>
            !editor.Document.Text.Substring(0, editor.SelectionStart).Contains(Environment.NewLine);

        public static bool IsCaretOnFinalLine(this LineViewModel editor) =>
            !editor.Document.Text.Substring(editor.SelectionStart).Contains(Environment.NewLine);

        public static bool IsTextSelected(this LineViewModel editor) =>
            editor.SelectionLength != 0;
    }
}
