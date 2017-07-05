using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace MobiusResourceMonitor_sub
{
    /// <summary>
    /// AboutWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
                e.Handled = true;
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(@"http://www.iotocean.org"));
            e.Handled = true;
        }

        private void Hyperlink_Click_1(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(@"http://www.apache.org/licenses/LICENSE-2.0"));
            e.Handled = true;
        }
    }
}
