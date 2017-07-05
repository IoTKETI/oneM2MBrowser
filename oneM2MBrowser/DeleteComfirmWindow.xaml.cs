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
    /// DeleteComfirmWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DeleteComfirmWindow : Window
    {
        public DeleteComfirmWindow()
        {
            InitializeComponent();
        }

        public void SetResourcePath(string path)
        {
            this.Title = "[Resource " + path +"]";
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if(this.txtConfirm.Text == "DELETE")
            {
                this.DialogResult = true;

                this.Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;

            this.Close();
        }
    }
}
