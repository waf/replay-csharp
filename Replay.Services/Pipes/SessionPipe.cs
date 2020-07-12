using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Replay.Services.Pipes
{
    /// <summary>
    /// Creates a named pipe for interprocess communication.
    /// Editors (VS, VSCode, etc) can write to this named pipe to send lines to evaluate in the REPL.
    /// </summary>
    public class SessionPipe
    {
        public const string PipeName = "ReplaySession";

        public async IAsyncEnumerable<string> ConnectAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var serverPipe = new NamedPipeServerStream(
                    GetPipeName(),
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    transmissionMode: PipeTransmissionMode.Message
                );

                await Task.Factory.FromAsync(
                    (callback, state) => serverPipe.BeginWaitForConnection(callback, state),
                    asyncResult => serverPipe.EndWaitForConnection(asyncResult),
                    state: null
                );

                var bytes = await ReadBytesAsync(serverPipe);

                yield return Encoding.UTF8.GetString(bytes).Trim();
            }
        }

        private string GetPipeName() =>
            PipeName + @"\" + Process.GetCurrentProcess().Id;

        private static async Task<byte[]> ReadBytesAsync(NamedPipeServerStream serverPipe)
        {
            using var memoryStream = new MemoryStream();

            do
            {
                await serverPipe.CopyToAsync(memoryStream);
            }
            while (!serverPipe.IsMessageComplete);

            return memoryStream.ToArray();
        }
    }
}
