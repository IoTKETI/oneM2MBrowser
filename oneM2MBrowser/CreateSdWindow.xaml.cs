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
    /// CreateSdWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CreateSdWindow : Window
    {
        public SemanticDescriptorObject SD { get; set; }

        public CreateSdWindow()
        {
            InitializeComponent();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            TextRange allTextRange = new TextRange(txtSD.Document.ContentStart, txtSD.Document.ContentEnd);

            string allText = allTextRange.Text.Remove(allTextRange.Text.Length - 2, 2);
            if (allText.Trim().Length > 0)
            {
                SD = new SemanticDescriptorObject();
                SD.RN = this.txtRN.Text;
                SD.DSPT = allText;
                //SD.DSPT = allText;

                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please input the necessary information for SemanticDesriptor", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
