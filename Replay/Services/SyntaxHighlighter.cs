using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace Replay.Services
{
    /// <summary>
    /// Uses roslyn's Classifier API to syntax highlight code.
    /// </summary>
    /// <remarks>
    /// There's not much documentation about this API, so this is based off of the following sample code:
    /// https://github.com/dotnet/roslyn/blob/master/src/Samples/CSharp/ConsoleClassifier/Program.cs
    /// </remarks>
    public class SyntaxHighlighter
    {
        private Document document;

        public SyntaxHighlighter(Document document)
        {
            this.document = document;
        }

        public IReadOnlyCollection<ColorSpan> Highlight(string code)
        {
            var source = SourceText.From(code);
            document = document.WithText(source);
            document.TryGetText(out SourceText text);
            IEnumerable<ClassifiedSpan> classified = 
                Classifier.GetClassifiedSpansAsync(document, TextSpan.FromBounds(0, text.Length))
                .Result;

            return classified
                .Select(span => new ColorSpan(
                    MapClassificationToColor(span.ClassificationType),
                    span.TextSpan.Start,
                    span.TextSpan.End))
                .ToList();
        }

        private Color MapClassificationToColor(string classificationType)
        {
            switch (classificationType)
            {
                case ClassificationTypeNames.Comment: return Color.FromArgb(255, 164, 114, 98);
                case ClassificationTypeNames.XmlDocCommentAttributeValue: return Color.FromArgb(255, 140, 250, 241);
                case ClassificationTypeNames.XmlDocCommentCDataSection: return Color.FromArgb(255, 164, 114, 98);
                case ClassificationTypeNames.XmlDocCommentComment: return Color.FromArgb(255, 164, 114, 98);
                case ClassificationTypeNames.XmlDocCommentDelimiter: return Color.FromArgb(255, 164, 114, 98);
                case ClassificationTypeNames.XmlDocCommentEntityReference: return Color.FromArgb(255, 164, 114, 98);
                case ClassificationTypeNames.XmlDocCommentName: return Color.FromArgb(255, 164, 114, 98);
                case ClassificationTypeNames.XmlDocCommentProcessingInstruction: return Color.FromArgb(255, 164, 114, 98);
                case ClassificationTypeNames.XmlDocCommentText: return Color.FromArgb(255, 164, 114, 98);
                case ClassificationTypeNames.XmlDocCommentAttributeQuotes: return Color.FromArgb(255, 140, 250, 241);
                case ClassificationTypeNames.XmlDocCommentAttributeName: return Color.FromArgb(255, 123, 250, 80);
                case ClassificationTypeNames.StructName: return Color.FromArgb(255, 253, 233, 139);
                case ClassificationTypeNames.ExcludedCode: return Color.FromArgb(255, 128, 128, 128);
                case ClassificationTypeNames.Identifier: return Color.FromArgb(255, 242, 248, 248);
                case ClassificationTypeNames.Keyword: return Color.FromArgb(255, 198, 121, 255);
                case ClassificationTypeNames.NumericLiteral: return Color.FromArgb(255, 199, 154, 255);
                case ClassificationTypeNames.Operator: return Color.FromArgb(255, 198, 121, 255);
                case ClassificationTypeNames.PreprocessorKeyword: return Color.FromArgb(255, 198, 121, 255);
                case ClassificationTypeNames.StringLiteral: return Color.FromArgb(255, 255, 255, 144);
                case ClassificationTypeNames.VerbatimStringLiteral: return Color.FromArgb(255, 255, 255, 144);
                case ClassificationTypeNames.Text: return Color.FromArgb(255, 248, 248, 242);
                case ClassificationTypeNames.ClassName: return Color.FromArgb(255, 253, 233, 139);
                case ClassificationTypeNames.DelegateName: return Color.FromArgb(255, 253, 233, 139);
                case ClassificationTypeNames.EnumName: return Color.FromArgb(255, 123, 250, 80);
                case ClassificationTypeNames.InterfaceName: return Color.FromArgb(255, 253, 233, 139);
                case ClassificationTypeNames.ModuleName: return Color.FromArgb(255, 253, 233, 139);
                case ClassificationTypeNames.TypeParameterName: return Color.FromArgb(255, 108, 184, 255);
                default: return Color.FromArgb(255, 255, 255, 255);
            }
        }
    }

    public class ColorSpan
    {
        public ColorSpan(Color color, int start, int end)
        {
            Color = color;
            Start = start;
            End = end;
        }

        public Color Color { get; }
        public int Start { get; }
        public int End { get; }
    }
}
