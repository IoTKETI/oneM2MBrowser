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
    /// CreateCinWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CreateCinWindow : Window
    {
        public ContentInstanceObject CIN { get; set; }

        public CreateCinWindow()
        {
            InitializeComponent();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            TextRange allTextRange = new TextRange(txtCon.Document.ContentStart, txtCon.Document.ContentEnd);

            string allText = allTextRange.Text.Remove(allTextRange.Text.Length - 2, 2);
            if (allText.Trim().Length > 0)
            {
                CIN = new ContentInstanceObject();
                CIN.RN = this.txtRN.Text;
                CIN.CON = allText;

                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please input the necessary information for ContentInstance", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
