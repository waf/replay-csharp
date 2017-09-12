using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Replay.UI
{
    /// <summary>
    /// Interaction logic for TitleBar.xaml
    /// </summary>
    public partial class TitleBar : UserControl
    {
        public TitleBar()
        {
            InitializeComponent();
        }

        public WindowState WindowState
        {
            get => (WindowState)GetValue(WindowStateProperty);
            set => SetValue(WindowStateProperty, value);
        }

        public static readonly DependencyProperty WindowStateProperty =
            DependencyProperty.Register(nameof(WindowState), typeof(WindowState), typeof(TitleBar));

        private void CloseButton_Click(object sender, RoutedEventArgs e) =>
            Application.Current.Shutdown();

        private void MinButton_Click(object sender, RoutedEventArgs e) =>
            this.WindowState = WindowState.Minimized;

        private void MaxButton_Click(object sender, RoutedEventArgs e) =>
            this.WindowState = this.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
    }
}
