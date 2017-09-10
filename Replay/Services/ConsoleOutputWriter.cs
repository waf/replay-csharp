using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replay.Services
{
    /// <summary>
    /// A StringWriter than can detect the difference between
    /// empty string output vs never being written to.
    /// </summary>
    class ConsoleOutputWriter : StringWriter
    {
        public bool HasOutput { get; private set; }

        public override void Write(char value)
        {
            HasOutput = true;
            base.Write(value);
        }
        public override void Write(char[] buffer, int index, int count)
        {
            HasOutput = true;
            base.Write(buffer, index, count);
        }
        public override void Write(string value)
        {
            HasOutput = true;
            base.Write(value);
        }
        public override Task WriteAsync(char value)
        {
            HasOutput = true;
            return base.WriteAsync(value);
        }
        public override Task WriteAsync(string value)
        {
            HasOutput = true;
            return base.WriteAsync(value);
        }
        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            HasOutput = true;
            return base.WriteAsync(buffer, index, count);
        }
        public override Task WriteLineAsync(char value)
        {
            HasOutput = true;
            return base.WriteAsync(value);
        }
        public override Task WriteLineAsync(string value)
        {
            HasOutput = true;
            return base.WriteAsync(value);
        }
        public override Task WriteLineAsync(char[] buffer, int index, int count)
        {
            HasOutput = true;
            return base.WriteAsync(buffer, index, count);
        }
    }
}
