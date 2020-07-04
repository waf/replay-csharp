using Microsoft.CodeAnalysis;
using Replay.Services;
using Replay.Services.AssemblyLoading;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Replay.Tests.Services
{
    public class ScriptEvaluatorTest
    {
        private readonly ScriptEvaluator scriptEvaluator;

        public ScriptEvaluatorTest()
        {
            this.scriptEvaluator = new ScriptEvaluator(
                new DefaultAssemblies(
                    new DotNetAssemblyLocator(
                        new RealFileIO()
                    )
                )
            );
        }

        [Fact]
        public async Task EvaluateAsync_ReturnValue_IsReturned()
        {
            var result = await scriptEvaluator.EvaluateAsync(@" ""Hello World"" ");
            Assert.Equal("Hello World", result.ScriptResult.ReturnValue);
        }

        [Fact]
        public async Task EvaluateAsync_NoReturnValue_IsNull()
        {
            var result = await scriptEvaluator.EvaluateAsync(@" Console.WriteLine(""Hello World"") ");
            Assert.Null(result.ScriptResult.ReturnValue);
        }

        [Fact]
        public async Task EvaluateAsync_Exception_ReturnsException()
        {
            var result = await scriptEvaluator.EvaluateAsync(@"throw new Exception(""Thingy is borked"");");
            Assert.Equal("Thingy is borked", result.Exception.Message);
        }

        [Fact]
        public async Task EvaluateAsync_SyntaxError_ReturnsException()
        {
            var result = await scriptEvaluator.EvaluateAsync(@"C#");
            Assert.Equal(
                "(1,2): error CS1040: Preprocessor directives must appear as the first non-whitespace character on a line",
                result.Exception.Message
            );
        }

        [Fact]
        public async Task EvaluateAsync_StandardOutput_IsReturned()
        {
            var result = await scriptEvaluator.EvaluateAsync(@"Console.WriteLine(""🦘"")");
            Assert.Equal(
                "🦘" + Environment.NewLine,
                result.StandardOutput
            );
        }

        [Fact]
        public async Task EvaluateAsync_MultipleEvaluation_PreserveState()
        {
            _ = await scriptEvaluator.EvaluateAsync(@"var favorite = ""durian"";");
            var subsequent = await scriptEvaluator.EvaluateAsync(@"""My favorite fruit is "" + favorite");
            Assert.Equal(
                "My favorite fruit is durian",
                subsequent.ScriptResult.ReturnValue
            );
        }

        [Fact]
        public async Task AddReferences_WithAssembly_CanAddReference()
        {
            const string instantiationRequiringReference = @"using NodaTime;";

            // ensure the reference is not loaded
            var failure = await scriptEvaluator.EvaluateAsync(instantiationRequiringReference);
            Assert.Equal(
                "(1,7): error CS0246: The type or namespace name 'NodaTime' could not be found (are you missing a using directive or an assembly reference?)",
                failure.Exception?.Message
            );

            // system under test
            await scriptEvaluator.AddReferences(
                MetadataReference.CreateFromFile("./TestHelpers/NodaTime.dll")
            );

            var success = await scriptEvaluator.EvaluateAsync(instantiationRequiringReference);
            Assert.Null(success.Exception);
        }

        [Fact]
        public async Task TryCompleteStatementAsync_WithMissingSemicolon_CanComplete()
        {
            var (success, newTree) = await scriptEvaluator.TryCompleteStatementAsync("var x = 5");

            Assert.True(success);
            Assert.Equal("var x = 5;", newTree.ToString());
        }

        [Fact]
        public async Task TryCompleteStatementAsync_WithSemicolon_DoesNotComplete()
        {
            var (success, newTree) = await scriptEvaluator.TryCompleteStatementAsync("var x = 5;");

            Assert.True(success);
            Assert.Equal("var x = 5;", newTree.ToString());
        }
    }
}
