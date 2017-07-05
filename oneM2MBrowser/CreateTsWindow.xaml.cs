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
    /// CreateTsWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CreateTsWindow : Window
    {
        public TimeSeriesObject TS;

        public CreateTsWindow()
        {
            InitializeComponent();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (this.txtRN.Text.Trim().Length > 0
                && this.txtMDDT.Text.Length > 0
                && this.txtPEI.Text.Length > 0)
            {
                this.TS = new TimeSeriesObject();
                this.TS.RN = this.txtRN.Text;
                this.TS.PEI = this.txtPEI.Text;

                if (radioTrue.IsChecked.Value)
                {
                    this.TS.MDD = "true";
                }

                if (radioFalse.IsChecked.Value)
                {
                    this.TS.MDD = "false";
                }
                this.TS.MDDT = this.txtMDDT.Text;

                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please input the necessary information for TimeSeries                                                                                                                                                                                                                         ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void txtPEI_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (this.txtPEI.Text.Trim().Length > 0)
                {
                    int value = Convert.ToInt32(this.txtPEI.Text);
                }
            }
            catch
            {
                MessageBox.Show("Must be a number!");
                this.txtPEI.Text = "";
            }
        }

        private void txtMDDT_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (this.txtMDDT.Text.Trim().Length > 0)
                {
                    int value = Convert.ToInt32(this.txtMDDT.Text);
                }
            }
            catch
            {
                MessageBox.Show("Must be a number!");
                this.txtMDDT.Text = "";
            }
        }
    }
}
