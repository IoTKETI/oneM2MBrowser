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
    /// CreateAeWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CreateAeWindow : Window
    {
        public AEObject AE { get; set; }

        public CreateAeWindow()
        {
            InitializeComponent();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (this.txtRN.Text.Trim().Length > 0
                && this.txtAppID.Text.Trim().Length > 0)
            {
                AE = new AEObject();
                AE.RN = this.txtRN.Text;
                AE.AppID = this.txtAppID.Text;

                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please input the necessary information for AE", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
