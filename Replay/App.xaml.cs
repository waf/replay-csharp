using Replay.Logging;
using System;
using System.Diagnostics;
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
            ReportFatalExceptionsToGitHub();
            base.OnStartup(e);
        }

        [Conditional("DEBUG")]
        private static void EnableDebugListeners()
        {
            PresentationTraceSources.Refresh();
            PresentationTraceSources.DataBindingSource.Listeners.Add(new ConsoleTraceListener());
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning | SourceLevels.Error;
        }

        [Conditional("RELEASE")]
        private void ReportFatalExceptionsToGitHub()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (!e.IsTerminating) return;

                var userSelection = MessageBox.Show(
                    "An unrecoverable error has occurred. Please press 'Ok' to send this error to GitHub or 'Cancel' to exit the application", "Unrecoverable Error",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Error
                );

                if (userSelection == MessageBoxResult.OK)
                {
                    new GitHubExceptionReporter().ReportException(e);
                }
            };
        }
    }
}
