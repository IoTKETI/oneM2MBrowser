using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace MobiusResourceMonitor_sub
{
    /// <summary>
    /// ucResource.xaml 的交互逻辑
    /// </summary>
    public partial class ucResource : UserControl
    {
        public string ResourceName
        {
            get { return (string)GetValue(ResourceNameProperty); }
            set { SetValue(ResourceNameProperty, value); }
        }

        public string ResourcePath
        {
            get { return (string)GetValue(ResourcePathProperty); }
            set { SetValue(ResourcePathProperty, value); }
        }

        public string ResourceType
        {
            get { return (string)GetValue(ResourceTypeProperty); }
            set { SetValue(ResourceTypeProperty, value); }
        }

        public string RootUrl
        {
            get;
            set;
        }

        private bool isChecked = false;

        private Storyboard sbCheckAnimi;

        public ucResource(string strUrl)
        {
            InitializeComponent();

            sbCheckAnimi = this.Resources["sbCheckAnim"] as Storyboard;
            //test
            RootUrl = strUrl;
        }

        public readonly static DependencyProperty ResourceNameProperty = DependencyProperty.Register("ResourceName", typeof(string), typeof(ucResource), new PropertyMetadata(ResourceNameChangedCallback));
        public readonly static DependencyProperty ResourcePathProperty = DependencyProperty.Register("ResourcePath", typeof(string), typeof(ucResource), new PropertyMetadata(ResourcePathChangedCallback));
        public readonly static DependencyProperty ResourceTypeProperty = DependencyProperty.Register("ResourceType", typeof(string), typeof(ucResource), new PropertyMetadata(ResourceTypeChangedCallback));

        public delegate void RetriveResourceEventHandler(object sender, RetriveResourceEvnetArgs e);

        public RetriveResourceEventHandler RetriveResoureceCompleted { get; set; }

        public void RaiseRetriveResourceEvent(string msg)
        {
            if (RetriveResoureceCompleted != null)
            {
                RetriveResourceEvnetArgs e = new RetriveResourceEvnetArgs(msg, this.ResourceType);
                RetriveResoureceCompleted(this, e);
            }
        }

        public static void ResourceNameChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ucResource control = sender as ucResource;
            control.SetResourceName();
        }

        public static void ResourcePathChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ucResource control = sender as ucResource;
            control.SetResourcePath();
        }

        public static void ResourceTypeChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ucResource control = sender as ucResource;
            control.SetResourceType();
        }

        public void SetResourceName()
        {
            this.txtTag.Text = ResourceName;
            this.tbkName.Text = ResourceName;
        }

        public void SetResourcePath()
        {
            this.tbkPath.Text = ResourcePath;
        }

        public void SetResourceType()
        {
            this.tbkType.Text = ResourceType;

            if (ResourceType == "CSEBase")
            {
                this.rootLayout.Background = Brushes.Yellow;
                this.rootLayout.BorderBrush = Brushes.GreenYellow;
                this.tbkShortTypeName.Text = "cse";
            }
            else if (ResourceType == "AE")
            {
                this.rootLayout.Background = Brushes.LightBlue;
                this.rootLayout.BorderBrush = Brushes.Blue;
                this.tbkShortTypeName.Text = "ae";
            }
            else if (ResourceType == "Container")
            {
                this.rootLayout.Background = Brushes.Pink;
                this.rootLayout.BorderBrush = Brushes.Red;
                this.tbkShortTypeName.Text = "cnt";
            }
            else if (ResourceType == "ContentInstance")
            {
                this.rootLayout.Background = Brushes.LightGreen;
                this.rootLayout.BorderBrush = Brushes.DarkGreen;
                this.tbkShortTypeName.Text = "cin";
            }
            else if (ResourceType == "Subscription")
            {
                this.rootLayout.Background = Brushes.LightGray;
                this.rootLayout.BorderBrush = Brushes.Black;
                this.tbkShortTypeName.Text = "sub";
            }
            else if (ResourceType == "SemanticDescription")
            {
                this.rootLayout.Background = new SolidColorBrush(Color.FromRgb(200, 138, 255));
                this.rootLayout.BorderBrush = Brushes.Purple;
                this.tbkShortTypeName.Text = "sd";
            }
        }

        private string getResourceInfo()
        {
            string strResult = "";
            try
            {
                string strUrl = RootUrl + ResourcePath;

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);
                req.Method = "GET";
                req.Accept = "application/xml";
                req.Headers.Add("X-M2M-RI", "12345");
                req.Headers.Add("X-M2M-Origin", "Origin");
                req.Timeout = 2000;

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader sr = new StreamReader(resp.GetResponseStream());
                    string content = sr.ReadToEnd();
                    
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(content);

                    content = Beautify(doc);

                    strResult = content;
                }
            }
            catch (WebException)
            {
                MessageBox.Show("Can not get resource information from mobius. checke the network status and try it again!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return strResult;
        }

        private string Beautify(XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }
            return sb.ToString();
        }

        public void ShowNewTag()
        {
            this.tbkNew.Visibility = System.Windows.Visibility.Visible;
        }

        public void Uncheck()
        {
            if (isChecked)
            {
                isChecked = false;
                sbCheckAnimi.Stop();
            }
        }

        private void rootLayout_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            string msg = getResourceInfo();

            RaiseRetriveResourceEvent(msg);

            this.tbkNew.Visibility = System.Windows.Visibility.Hidden;
            this.isChecked = true;
            this.sbCheckAnimi.Begin();
        }

        private void rootLayout_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Hand;
        }

        private void rootLayout_MouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }
    }

    public class RetriveResourceEvnetArgs : EventArgs
    {
        public string MessageBody { get; set; }
        public string ResourceType { get; set; }

        public RetriveResourceEvnetArgs(string msg, string rt)
        {
            this.MessageBody = msg;
            this.ResourceType = rt;
        }
    }
}
