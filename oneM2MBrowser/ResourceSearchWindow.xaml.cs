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

namespace oneM2MBrowser
{
    /// <summary>
    /// Interaction logic for ResourceSearchWindow.xaml
    /// </summary>
    public partial class ResourceSearchWindow : Window
    {
        public delegate void SearchResourceEventHandler(object sender, SearchResourceEventArgs e);

        public SearchResourceEventHandler OnSearching { get; set; }

        private void RaiseSearchResourceEvent(string key)
        {
            if (OnSearching != null)
            {
                SearchResourceEventArgs e = new SearchResourceEventArgs(key);
                OnSearching(this, e);
            }
        }

        public ResourceSearchWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            this.Hide();
        }

        private void btnFind_Click(object sender, RoutedEventArgs e)
        {
            if(this.txtKeyWord.Text.Trim().Length > 0)
            {
                RaiseSearchResourceEvent(this.txtKeyWord.Text);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }

    public class SearchResourceEventArgs : EventArgs
    {
        public string SearchKey { get; set; } 

        public SearchResourceEventArgs(string key)
        {
            this.SearchKey = key;
        }
    }
}
