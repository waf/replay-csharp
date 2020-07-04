using Replay.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Replay.Tests
{
    /// <summary>
    /// Building a ReplServices is expensive, this will allow re-use of a single
    /// across all tests.
    /// </summary>
    public class ReplServicesFixture : IAsyncLifetime
    {
        public ReplServices ReplServices { get; }

        public ReplServicesFixture()
        {
            this.ReplServices = new ReplServices(new RealFileIO());
        }

        public Task InitializeAsync() => this.ReplServices.HighlightAsync(Guid.Empty, "");

        public Task DisposeAsync() => Task.CompletedTask;
    }
}
