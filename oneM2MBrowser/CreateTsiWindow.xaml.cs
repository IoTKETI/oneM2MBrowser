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
    /// CreateTsiWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CreateTsiWindow : Window
    {
        public TimeSeriesContentInstanceObject TSI { get; set; }

        public CreateTsiWindow()
        {
            InitializeComponent();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            TextRange allTextRange = new TextRange(txtCon.Document.ContentStart, txtCon.Document.ContentEnd);

            string allText = allTextRange.Text.Remove(allTextRange.Text.Length - 2, 2);
            if (allText.Trim().Length > 0)
            {
                TSI = new TimeSeriesContentInstanceObject();
                TSI.RN = this.txtRN.Text;
                TSI.CON = allText;

                var str_dpt = String.Format("{0:4d}{1:2d}{2:2d}{3:2d}{4:2d}{5:2d}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                TSI.DGT = str_dpt;

                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please input the necessary information for TimeSeriesContentInstance", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
