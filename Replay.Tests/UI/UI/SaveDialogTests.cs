using ICSharpCode.AvalonEdit.Document;
using NSubstitute;
using Replay.ViewModel;
using Replay.Services;
using Replay.Services.SessionSavers;
using Replay.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Replay.Tests.UI
{
    public class SaveDialogTests
    {
        private readonly IReplServices replServices;
        private readonly SaveDialog saveDialog;

        private SaveDialog.ShowSaveDialog saveDialogStub;

        public SaveDialogTests()
        {
            this.replServices = Substitute.For<IReplServices>();
            this.saveDialog = new SaveDialog(
                replServices,
                filter => saveDialogStub(filter),
                message => { }
            );
        }

        [Fact]
        public async Task SaveAsync_SessionProvided_SavesSession()
        {
            replServices.GetSupportedSaveFormats().Returns(new[] {
                "Markdown File (*.md)|*.md",
                "Best Format File (*.bff)|*.bff",
            });

            saveDialogStub = saveFormats => new SaveDialog.SaveDialogResult(true, "TheBest.bff", 2);

            var inputData = new[]
            {
                CreateLine("\"Hello\" + \" World\"", "\"Hello World\"", null, null),
                CreateLine("", null, null, null),
                CreateLine("asdf", null, null, "asdf is undefined"),
                CreateLine("Console.WriteLine(4)", null, "4", null),
            };

            // system under test
            await this.saveDialog.SaveAsync(inputData);

            await replServices
                .Received()
                .SaveSessionAsync(
                    "TheBest.bff",
                    "Best Format File (*.bff)|*.bff",
                    Arg.Is<IReadOnlyCollection<LineToSave>>(lines => AssertSavedData(inputData, lines))
                );
        }

        bool AssertSavedData(IReadOnlyCollection<LineViewModel> inputData, IReadOnlyCollection<LineToSave> lines)
        {
            Assert.Equal(inputData.Count, lines.Count);

            foreach(var line in inputData.Zip(lines, (input, output) => (input, output)))
            {
                Assert.Equal(line.input.Document.Text, line.output.Input);
                Assert.Equal(line.input.Result, line.output.Result);
                Assert.Equal(line.input.StandardOutput, line.output.Output);
                Assert.Equal(line.input.Error, line.output.Error);
            }

            return true;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(null)]
        public async Task SaveAsync_SaveCanceled_DoesNotSave(bool? closeResult)
        {
            replServices.GetSupportedSaveFormats().Returns(new[] {
                "Markdown File (*.md)|*.md",
                "Best Format File (*.bff)|*.bff",
            });
            
            // user cancels dialog
            saveDialogStub = saveFormats => new SaveDialog.SaveDialogResult(closeResult, null, 0);

            // system under test
            await this.saveDialog.SaveAsync(new[] { CreateLine("asdf", null, null, "asdf is not defined") });

            await replServices
                .DidNotReceiveWithAnyArgs()
                .SaveSessionAsync(default, default, default);
        }

        private static LineViewModel CreateLine(string text, string result, string output, string error) =>
            new LineViewModel
            {
                Document = new TextDocument(text),
                Result = result,
                StandardOutput = output,
                Error = error
            };
    }
}
