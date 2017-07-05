using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MobiusResourceMonitor_sub
{
    /// <summary>
    /// ucProccessView.xaml 的交互逻辑
    /// </summary>
    public partial class ucProccessView : Window
    {
        private delegate void UpdateProgressBarDelegate(System.Windows.DependencyProperty dp, Object value);

        public ucProccessView()
        {
            InitializeComponent();
        }

        public void setProgressValue(double value)
        {
            if (value >= 0 && value <= 100)
            {
                UpdateProgressBarDelegate updatePbDelegate = new UpdateProgressBarDelegate(this.pbTask.SetValue);

                Dispatcher.Invoke(updatePbDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { System.Windows.Controls.ProgressBar.ValueProperty, value });
            }
        }

        public void setProgressStatus(string msg)
        {
            UpdateProgressBarDelegate updatePbDelegate = new UpdateProgressBarDelegate(this.tbkProgress.SetValue);

            Dispatcher.Invoke(updatePbDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { System.Windows.Controls.TextBlock.TextProperty, msg });
        }
    }
}
