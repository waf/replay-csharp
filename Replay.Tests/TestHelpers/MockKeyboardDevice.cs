using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Replay.Tests.TestHelpers
{
    /// <summary>
    /// Adapted from https://github.com/VsVim/VsVim/blob/master/Src/VimTestUtils/Mock/MockKeyboardDevice.cs
    /// allows us to mock keyboard events in tests (most importantly: modifiers)
    /// </summary>
    /// <example>
    /// var device = new MockKeyboardDevice();
    /// device.ModifierKeysImpl = ModifierKeys.Control;
    /// var keyDown = device.CreateKeyEventArgs(Key.C, Keyboard.KeyDownEvent);
    /// /* send keyDown to your code */
    /// var keyUp = device.CreateKeyEventArgs(key, Keyboard.KeyUpEvent);
    /// /* send keyUp to your code */
    /// device.ModifierKeysImpl = ModifierKeys.None;
    /// </example>
    public sealed class MockKeyboardDevice : KeyboardDevice
    {
        public ModifierKeys ModifierKeysImpl { get; set; }

        public MockKeyboardDevice()
            : this(InputManager.Current)
        {
        }

        public MockKeyboardDevice(InputManager manager)
            : base(manager)
        {
        }

        protected override KeyStates GetKeyStatesFromSystem(Key key)
        {
            var hasMod = false;
            switch (key)
            {
                case Key.LeftAlt:
                case Key.RightAlt:
                    hasMod = HasModifierKey(ModifierKeys.Alt);
                    break;
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    hasMod = HasModifierKey(ModifierKeys.Control);
                    break;
                case Key.LeftShift:
                case Key.RightShift:
                    hasMod = HasModifierKey(ModifierKeys.Shift);
                    break;
            }

            return hasMod ? KeyStates.Down : KeyStates.None;
        }

        public KeyEventArgs CreateKeyEventArgs(Key key, RoutedEvent routedEvent) =>
            new KeyEventArgs(this, new MockPresentationSource(), 0, key)
            {
                RoutedEvent = routedEvent
            };

        private bool HasModifierKey(ModifierKeys modKey) =>
            0 != (ModifierKeysImpl & modKey);

        private sealed class MockPresentationSource : PresentationSource
        {
            private Visual _rootVisual;

            protected override CompositionTarget GetCompositionTargetCore()
            {
                throw new NotImplementedException();
            }

            public override bool IsDisposed
            {
                get { return false; }
            }

            public override Visual RootVisual
            {
                get { return _rootVisual; }
                set { _rootVisual = value; }
            }
        }

    }
}
