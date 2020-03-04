using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

[assembly: InternalsVisibleTo("Replay.Tests")]
namespace Replay
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            EnableDebugListeners();
            base.OnStartup(e);
        }

        [Conditional("DEBUG")]
        private static void EnableDebugListeners()
        {
            PresentationTraceSources.Refresh();
            PresentationTraceSources.DataBindingSource.Listeners.Add(new ConsoleTraceListener());
            PresentationTraceSources.DataBindingSource.Listeners.Add(new DebugTraceListener());
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning | SourceLevels.Error;
        }
    }

    public class DebugTraceListener : TraceListener
    {
        public DebugTraceListener()
        {
            this.TraceOutputOptions = TraceOptions.Callstack | TraceOptions.LogicalOperationStack;
        }
        public override void Write(string message)
        {
        }

        public override void WriteLine(string message)
        {
            if(IgnoredDataBindIssues.Any(message.StartsWith))
            {
                return;
            }

            Debugger.Break();
        }

        private IReadOnlyCollection<string> IgnoredDataBindIssues = new[] {
            // something about scaling the code completion tooltip while it's animating in throws this warning
            "Cannot find governing FrameworkElement or FrameworkContentElement for target element. BindingExpression:Path=Zoom; DataItem=null; target element is 'ScaleTransform'",
        };
    }
}
