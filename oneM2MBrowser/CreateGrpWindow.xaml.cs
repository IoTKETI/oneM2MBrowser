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
    /// CreateGrpWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CreateGrpWindow : Window
    {
        public GroupObject GRP { get; set; }

        public CreateGrpWindow()
        {
            InitializeComponent();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (this.txtRN.Text.Trim().Length > 0 
                && this.txtMNM.Text.Length > 0
                && this.lstUrls.Items.Count > 0)
            {
                this.GRP = new GroupObject();
                this.GRP.RN = this.txtRN.Text;
                this.GRP.MNM = this.txtMNM.Text;

                List<string> lstNus = new List<string>();

                for (int i = 0; i < this.lstUrls.Items.Count; i++)
                {
                    lstNus.Add((this.lstUrls.Items[i] as ListBoxItem).Content.ToString());
                }

                this.GRP.MID = lstNus.ToArray();

                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please input the necessary information for Group", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (this.txtNewUrl.Text.Trim().Length > 0)
            {
                for (int i = 0; i < this.lstUrls.Items.Count; i++)
                {
                    if ((this.lstUrls.Items[i] as ListBoxItem).Content.ToString() == this.txtNewUrl.Text)
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
            if (this.lstUrls.SelectedItem != null)
            {
                this.lstUrls.Items.Remove(this.lstUrls.SelectedItem);
            }
        }

        private void txtMNM_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (this.txtMNM.Text.Trim().Length > 0)
                {
                    int value = Convert.ToInt32(this.txtMNM.Text);
                }
            }
            catch
            {
                MessageBox.Show("Must be a number!");
                this.txtMNM.Text = "";
            }
        }
    }
}
