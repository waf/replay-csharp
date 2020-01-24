using NSubstitute;
using NuGet.Common;
using Replay.Services.Logging;
using Replay.Services.Nuget;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Replay.Tests.Services.Nuget
{
    public class NugetErrorLoggerTest
    {
        private readonly IReplLogger replLogger;
        private readonly NugetErrorLogger logger;

        public NugetErrorLoggerTest()
        {
            this.replLogger = Substitute.For<IReplLogger>();
            this.logger = new NugetErrorLogger(replLogger);
        }

        [Theory]
        [InlineData(new object[] { LogLevel.Verbose, LogTestCase.Log })]
        [InlineData(new object[] { LogLevel.Verbose, LogTestCase.LogMessage })]
        [InlineData(new object[] { LogLevel.Verbose, LogTestCase.LogAsync })]
        [InlineData(new object[] { LogLevel.Information, LogTestCase.Log })]
        [InlineData(new object[] { LogLevel.Information, LogTestCase.LogMessage })]
        [InlineData(new object[] { LogLevel.Information, LogTestCase.LogAsync })]
        [InlineData(new object[] { LogLevel.Debug, LogTestCase.Log })]
        [InlineData(new object[] { LogLevel.Debug, LogTestCase.LogMessage })]
        [InlineData(new object[] { LogLevel.Debug, LogTestCase.LogAsync })]
        [InlineData(new object[] { LogLevel.Minimal, LogTestCase.Log })]
        [InlineData(new object[] { LogLevel.Minimal, LogTestCase.LogMessage })]
        [InlineData(new object[] { LogLevel.Minimal, LogTestCase.LogAsync })]
        public async Task Log_DetailedLogs_AreNotLogged(LogLevel level, LogTestCase test)
        {
            await LogMethod(level, test, "Hello There!");

            replLogger
                .DidNotReceiveWithAnyArgs()
                .LogOutput(default);
            replLogger
                .DidNotReceiveWithAnyArgs()
                .LogError(default);
        }

        [Theory]
        [InlineData(new object[] { LogTestCase.Log })]
        [InlineData(new object[] { LogTestCase.LogMessage })]
        [InlineData(new object[] { LogTestCase.LogAsync })]
        public async Task Log_ErrorLogs_AreLogged(LogTestCase test)
        {
            await LogMethod(LogLevel.Error, test, "Computer on fire");

            replLogger
                .DidNotReceiveWithAnyArgs()
                .LogOutput(default);
            replLogger
                .Received()
                .LogError("Computer on fire");
        }

        [Theory]
        [InlineData(new object[] { LogTestCase.Log })]
        [InlineData(new object[] { LogTestCase.LogMessage })]
        [InlineData(new object[] { LogTestCase.LogAsync })]
        public async Task Log_WarningLogs_AreLogged(LogTestCase test)
        {
            await LogMethod(LogLevel.Warning, test, "Computer is smoldering");

            replLogger
                .Received()
                .LogOutput("Computer is smoldering");
            replLogger
                .DidNotReceiveWithAnyArgs()
                .LogError(default);
        }

        /// <summary>
        /// There are three ways of logging using a Nuget logger. We want to 
        /// ensure consistend behavior across all three methods.
        /// </summary>
        private async Task LogMethod(LogLevel level, LogTestCase test, string message)
        {
            switch (test)
            {
                case LogTestCase.Log:
                    logger.Log(level, message);
                    break;
                case LogTestCase.LogMessage:
                    logger.Log(new LogMessage(level, message));
                    break;
                case LogTestCase.LogAsync:
                    await logger.LogAsync(level, message);
                    break;
                default:
                    throw new ArgumentException("Unknown test case: " + test);
            }
        }

        public enum LogTestCase
        {
            Log,
            LogAsync,
            LogMessage,
        }
    }
}
