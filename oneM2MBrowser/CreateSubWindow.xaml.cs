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
    /// CreateSubWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CreateSubWindow : Window
    {
        public SubscriptionObject SUB { get; set; }

        public CreateSubWindow()
        {
            InitializeComponent();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (this.txtRN.Text.Trim().Length > 0
                && (this.chbNet1.IsChecked.Value
                || this.chbNet2.IsChecked.Value
                || this.chbNet3.IsChecked.Value
                || this.chbNet4.IsChecked.Value)
                && this.lstUrls.Items.Count > 0)
            {
                SUB = new SubscriptionObject();

                this.SUB.RN = this.txtRN.Text;

                List<string> lstNets = new List<string>();

                if (this.chbNet1.IsChecked.Value) lstNets.Add("1");
                if (this.chbNet2.IsChecked.Value) lstNets.Add("2");
                if (this.chbNet3.IsChecked.Value) lstNets.Add("3");
                if (this.chbNet4.IsChecked.Value) lstNets.Add("4");

                this.SUB.NET = lstNets.ToArray();

                List<string> lstNus = new List<string>();

                for(int i = 0; i < this.lstUrls.Items.Count; i++)
                {
                    lstNus.Add((this.lstUrls.Items[i] as ListBoxItem).Content.ToString());
                }

                this.SUB.NU = lstNus.ToArray();

                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please input the necessary information for Subscription", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if(this.txtNewUrl.Text.Trim().Length > 0)
            {
                for(int i =0; i < this.lstUrls.Items.Count; i++)
                {
                    if((this.lstUrls.Items[i] as ListBoxItem).Content.ToString() == this.txtNewUrl.Text)
                    {
                        MessageBox.Show("This notification url is existed..!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                        return;
                    }
                }

                ListBoxItem newItem = new ListBoxItem();
                newItem.Content = this.txtNewUrl.Text;

                this.lstUrls.Items.Add(newItem);

                this.txtNewUrl.Text = "";
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if(this.lstUrls.SelectedItem != null)
            {
                this.lstUrls.Items.Remove(this.lstUrls.SelectedItem);
            }
        }
    }
}
