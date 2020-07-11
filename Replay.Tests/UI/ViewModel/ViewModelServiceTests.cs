using NSubstitute;
using Replay.Services;
using Replay.ViewModel;
using Replay.ViewModel.Services;
using System.Windows.Input;
using Xunit;

namespace Replay.Tests.UI.ViewModel
{
    public class ViewModelServiceTests
    {
        private readonly IReplServices replServices;
        private readonly ViewModelService viewModelServices;

        public ViewModelServiceTests()
        {
            this.replServices = Substitute.For<IReplServices>();
            this.viewModelServices = new ViewModelService(replServices);
        }

        [WpfFact]
        public void HandleWindowScroll_NoCtrlKey_DoesNotZoom()
        {
            var windowvm = new WindowViewModel();
            var mouseScroll = new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, 1);

            // system under test
            viewModelServices.HandleWindowScroll(windowvm, ModifierKeys.None, mouseScroll);

            Assert.Equal(1, windowvm.Zoom);
        }

        [WpfFact]
        public void HandleWindowScroll_CtrlKey_DoesZoom()
        {
            var windowvm = new WindowViewModel();
            var mouseScroll = new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, 1);

            // system under test
            viewModelServices.HandleWindowScroll(windowvm, ModifierKeys.Control, mouseScroll);

            Assert.Equal(1.05, windowvm.Zoom);
        }
    }
}
