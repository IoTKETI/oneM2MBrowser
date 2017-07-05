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
using System.Windows.Shapes;

namespace MobiusResourceMonitor_sub
{
    /// <summary>
    /// AcpInputWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AcpInputWindow : Window
    {
        public string ACP { get; set; }

        public AcpInputWindow()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (this.txtACP.Text.Trim().Length > 0)
            {
                this.DialogResult = true;

                this.ACP = this.txtACP.Text.Trim();

            } else
            {
                this.DialogResult = false;
            }

            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
