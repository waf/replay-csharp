using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using Replay.Model;
using Replay.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IReadOnlyDictionary<string, Color> theme;
        public Color ForegroundColor { get; }
        public Color BackgroundColor { get; }

        public SyntaxHighlighter(string themeFilename)
        {
            this.theme = ThemeReader.GetTheme(themeFilename);
            this.ForegroundColor = theme[ThemeReader.Foreground];
            this.BackgroundColor = theme[ThemeReader.Background];
        }

        public async Task<IReadOnlyCollection<ColorSpan>> Highlight(ReplSubmission submission)
        {
            IEnumerable<ClassifiedSpan> classified = await Classifier.GetClassifiedSpansAsync(
                submission.Document,
                TextSpan.FromBounds(0, submission.Code.Length)
            );

            return classified

                .Select(span => new ColorSpan
                (
                    theme.GetValueOrDefault(span.ClassificationType, ForegroundColor),
                    span.TextSpan.Start,
                    span.TextSpan.End
                ))
                .ToList();
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
