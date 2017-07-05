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
    /// CreateCntWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CreateCntWindow : Window
    {
        public ContainerObject CNT { get; set; }

        public CreateCntWindow()
        {
            InitializeComponent();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (this.txtRN.Text.Trim().Length > 0)
            {
                CNT = new ContainerObject();
                CNT.RN = this.txtRN.Text;

                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please input the necessary information for Container", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
