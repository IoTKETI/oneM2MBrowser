/**
 * Copyright (c) 2015, OCEAN
 * All rights reserved.
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
 * 1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products derived from this software without specific prior written permission.
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

/**
 * Created by Chen Nan in KETI on 2016-07-28.
 */
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml;

namespace MobiusResourceMonitor_sub
{
    /// <summary>
    /// ucResource.xaml UI logic
    /// </summary>
    public partial class ucResource : UserControl, IGetResourceInfoCallback
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

        public string AccessControlPolicy
        {
            get;
            set;
        }

        public string RootUrl
        {
            get;
            set;
        }

        public string BodyType
        {
            get;
            set;
        }

        private bool isChecked = false;

        private Storyboard sbCheckAnimi;
        private Storyboard sbBlinkAnimi;

        private ResourceManager rm;

        public ucResource(ResourceManager rm)
        {
            InitializeComponent();

            sbCheckAnimi = this.Resources["sbCheckAnim"] as Storyboard;

            this.rm = rm;
        }

        public readonly static DependencyProperty ResourceNameProperty = DependencyProperty.Register("ResourceName", typeof(string), typeof(ucResource), new PropertyMetadata(ResourceNameChangedCallback));
        public readonly static DependencyProperty ResourcePathProperty = DependencyProperty.Register("ResourcePath", typeof(string), typeof(ucResource), new PropertyMetadata(ResourcePathChangedCallback));
        public readonly static DependencyProperty ResourceTypeProperty = DependencyProperty.Register("ResourceType", typeof(string), typeof(ucResource), new PropertyMetadata(ResourceTypeChangedCallback));

        public delegate void RetriveResourceEventHandler(object sender, RetriveResourceEventArgs e);
        public delegate void DeleteResourceEventHandler(object sender, DeleteResourceEventArgs e);
        public delegate void CreateResourceEventHandler(object sender, CreateResourceEventArgs e);

        public RetriveResourceEventHandler RetriveResoureceCompleted { get; set; }
        public DeleteResourceEventHandler DeleteResourceCompleted { get; set; }
        public CreateResourceEventHandler CreateResourceCompleted { get; set; }

        private void RaiseRetriveResourceEvent(string msg)
        {
            if (RetriveResoureceCompleted != null)
            {
                RetriveResourceEventArgs e = new RetriveResourceEventArgs(msg, this.ResourceType);
                RetriveResoureceCompleted(this, e);
            }
        }

        private void RaiseDeleteResourceEvent()
        {
            if (DeleteResourceCompleted != null)
            {
                DeleteResourceEventArgs e = new DeleteResourceEventArgs();
                DeleteResourceCompleted(this, e);
            }
        }

        public void RaiseCreateResourceEvent(OneM2MResource resc)
        {
            if (CreateResourceCompleted != null)
            {
                CreateResourceEventArgs e = new CreateResourceEventArgs(resc);
                CreateResourceCompleted(this, e);
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
            if (ResourceName.Length > 23)
            {
                string newName = ResourceName.Substring(0, 8) + "..." + ResourceName.Substring(ResourceName.Length - 8, 8);
                this.txtTag.Text = newName;
            }
            else
            {
                this.txtTag.Text = ResourceName;
            }
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
                this.rootLayout.BorderBrush = Brushes.DarkGreen;
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
            else if (ResourceType == "SemanticDescriptor")
            {
                this.rootLayout.Background = new SolidColorBrush(Color.FromRgb(200, 138, 255));
                this.rootLayout.BorderBrush = Brushes.Purple;
                this.tbkShortTypeName.Text = "smd";
            }
            else if (ResourceType == "TimeSeries")
            {
                this.rootLayout.Background = new SolidColorBrush(Color.FromRgb(255, 197, 0));
                this.rootLayout.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 104, 0));
                this.tbkShortTypeName.Text = "ts";
            }
            else if (ResourceType == "TimeSeriesContentInstance")
            {
                this.rootLayout.Background = new SolidColorBrush(Color.FromRgb(255, 124, 236));
                this.rootLayout.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 79, 255));
                this.tbkShortTypeName.Text = "tsi";
            }
            else if (ResourceType == "Group")
            {
                this.rootLayout.Background = new SolidColorBrush(Color.FromRgb(0, 255, 185));
                this.rootLayout.BorderBrush = new SolidColorBrush(Color.FromRgb(176, 0, 255));
                this.tbkShortTypeName.Text = "grp";
            }
        }

        private void getResourceInfo()
        {
            //string strResult = "";
            try
            {
                string strUrl = RootUrl + ResourcePath;

                rm.GetResourceInfoAsync(strUrl, BodyType, this);

            }
            catch (WebException exp)
            {
                Debug.WriteLine(exp.Message);
                MessageBox.Show("Can not get resource information from mobius. checke the network status and try it again!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //return strResult;
        }

        private string FormatJson(string json)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);
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
            this.tbkNew.Visibility = Visibility.Visible;
            this.tbkOld.Visibility = Visibility.Hidden;
            BinkBlock();
        }

        public void HideNewTag()
        {
            this.tbkNew.Visibility = Visibility.Hidden;
        }

        public void ShowOldTag()
        {
            this.tbkOld.Visibility = Visibility.Visible;
            this.tbkNew.Visibility = Visibility.Hidden;
        }

        public void HideOldTag()
        {
            this.tbkOld.Visibility = Visibility.Hidden;
        }

        public void ShowSearchArrow()
        {
            this.imgArrow.Visibility = Visibility.Visible;
        }

        public void HideSearchArrow()
        {
            this.imgArrow.Visibility = Visibility.Hidden;
        }

        public void BinkBlock()
        {
            sbBlinkAnimi = this.Resources["BlockBlink"] as Storyboard;
            sbBlinkAnimi.Begin();
        }

        public void Uncheck()
        {
            if (isChecked)
            {
                isChecked = false;
                sbCheckAnimi.Stop();
            }
        }

        public void RequestResourceInfo()
        {
            //string msg = getResourceInfo();

            getResourceInfo();

            //RaiseRetriveResourceEvent(msg);

            //this.tbkNew.Visibility = System.Windows.Visibility.Hidden;
            //this.isChecked = true;
            //this.sbCheckAnimi.Begin();
        }

        private void rootLayout_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            RequestResourceInfo();
        }

        private void rootLayout_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Hand;
        }

        private void rootLayout_MouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void UserControl_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ContextMenu root = new ContextMenu();

            MenuItem meitCreate = new MenuItem();
            MenuItem meitDelete = new MenuItem();

            MenuItem meitCreateAE = new MenuItem();
            MenuItem meitCreateCnt = new MenuItem();
            MenuItem meitCreateSub = new MenuItem();
            MenuItem meitCreateCin = new MenuItem();
            MenuItem meitCreateSd = new MenuItem();
            MenuItem meitCreateTs = new MenuItem();
            MenuItem meitCreateTscin = new MenuItem();
            MenuItem meitCreateGrp = new MenuItem();

            meitDelete.Click += MeitDelete_Click;

            meitCreateAE.Click += MeitCreate_Click;
            meitCreateCnt.Click += MeitCreate_Click;
            meitCreateSub.Click += MeitCreate_Click;
            meitCreateCin.Click += MeitCreate_Click;
            meitCreateSd.Click += MeitCreate_Click;
            meitCreateTs.Click += MeitCreate_Click;
            meitCreateTscin.Click += MeitCreate_Click;
            meitCreateGrp.Click += MeitCreate_Click;

            meitCreate.Header = "Create";
            meitDelete.Header = "Delete";

            meitCreateAE.Header = "AE";
            meitCreateCnt.Header = "Container";
            meitCreateSub.Header = "Subscription";
            meitCreateCin.Header = "ContentInstance";
            meitCreateSd.Header = "SemanticDescriptor";
            meitCreateTs.Header = "TimeSeries";
            meitCreateTscin.Header = "TimeSeriesContentInstance";
            meitCreateGrp.Header = "Group";

            if (ResourceType == "CSEBase")
            {
                meitCreate.Items.Add(meitCreateAE);
                meitCreate.Items.Add(meitCreateCnt);
                meitCreate.Items.Add(meitCreateTs);
                meitCreate.Items.Add(meitCreateGrp);
            }
            else if (ResourceType == "AE")
            {
                meitCreate.Items.Add(meitCreateCnt);
                meitCreate.Items.Add(meitCreateSub);
                meitCreate.Items.Add(meitCreateSd);
                meitCreate.Items.Add(meitCreateTs);
                meitCreate.Items.Add(meitCreateGrp);
            }
            else if (ResourceType == "Container")
            {
                meitCreate.Items.Add(meitCreateCnt);
                meitCreate.Items.Add(meitCreateCin);
                meitCreate.Items.Add(meitCreateSub);
                meitCreate.Items.Add(meitCreateSd);
            }
            else if (ResourceType == "ContentInstance")
            {
                meitCreate.Items.Add(meitCreateSd);
            }
            else if (ResourceType == "Subscription")
            {
                meitCreate.IsEnabled = false;

                if (this.ResourceName == (this.rm.aeName + @"_sub"))
                {
                    meitDelete.IsEnabled = false;
                }
            }
            else if (ResourceType == "SemanticDescriptor")
            {
                meitCreate.Items.Add(meitCreateSub);
            }
            else if (ResourceType == "TimeSeries")
            {
                meitCreate.Items.Add(meitCreateTs);
                meitCreate.Items.Add(meitCreateTscin);
                meitCreate.Items.Add(meitCreateSub);
                meitCreate.Items.Add(meitCreateSd);
            }
            else if (ResourceType == "TimeSeriesContentInstance")
            {
                meitCreate.Items.Add(meitCreateSd);
            }
            else if (ResourceType == "Group")
            {
                meitCreate.Items.Add(meitCreateSub);
                meitCreate.Items.Add(meitCreateSd);
            }

            root.Items.Add(meitCreate);
            root.Items.Add(meitDelete);

            this.ContextMenu = root;
        }

        private void MeitDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteComfirmWindow form = new DeleteComfirmWindow();
            form.SetResourcePath(ResourcePath);
            if (form.ShowDialog().Value)
            {
                try
                {
                    if (!rm.DeleteResource(ResourcePath, AccessControlPolicy))
                    {
                        MessageBox.Show("Can not delete resource from mobius. checke the network status and try it again!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        RaiseDeleteResourceEvent();
                    }
                }
                catch (OneM2MException exp)
                {
                    if (exp.ExceptionCode == 403)
                    {
                        AcpInputWindow acpInputForm = new AcpInputWindow();
                        if (acpInputForm.ShowDialog().Value)
                        {
                            if (!rm.DeleteResource(ResourcePath, acpInputForm.ACP))
                            {
                                MessageBox.Show("Can not delete resource from mobius. checke the network status and try it again!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            else
                            {
                                RaiseDeleteResourceEvent();
                            }
                        }
                    }
                }
            }
        }

        private void MeitCreate_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;

            if (item.Header.ToString() == "AE")
            {
                CreateAeWindow form = new CreateAeWindow();
                if (form.ShowDialog().Value)
                {
                    RaiseCreateResourceEvent(form.AE);
                }
            }
            else if (item.Header.ToString() == "Container")
            {
                CreateCntWindow form = new CreateCntWindow();
                if (form.ShowDialog().Value)
                {
                    RaiseCreateResourceEvent(form.CNT);
                }
            }
            else if (item.Header.ToString() == "ContentInstance")
            {
                CreateCinWindow form = new CreateCinWindow();
                if (form.ShowDialog().Value)
                {
                    RaiseCreateResourceEvent(form.CIN);
                }
            }
            else if (item.Header.ToString() == "Subscription")
            {
                CreateSubWindow form = new CreateSubWindow();
                if (form.ShowDialog().Value)
                {
                    RaiseCreateResourceEvent(form.SUB);
                }
            }
            else if (item.Header.ToString() == "Group")
            {
                CreateGrpWindow form = new CreateGrpWindow();
                if (form.ShowDialog().Value)
                {
                    RaiseCreateResourceEvent(form.GRP);
                }
            }
            else if (item.Header.ToString() == "TimeSeries")
            {
                CreateTsWindow form = new CreateTsWindow();
                if (form.ShowDialog().Value)
                {
                    RaiseCreateResourceEvent(form.TS);
                }
            }
            else if (item.Header.ToString() == "TimeSeriesContentInstance")
            {
                CreateTsiWindow form = new CreateTsiWindow();
                if (form.ShowDialog().Value)
                {
                    RaiseCreateResourceEvent(form.TSI);
                }
            }
            else if (item.Header.ToString() == "SemanticDescriptor")
            {
                CreateSdWindow form = new CreateSdWindow();
                if (form.ShowDialog().Value)
                {
                    RaiseCreateResourceEvent(form.SD);
                }
            }
        }

        public void GetResourceInfoFinish(string msg)
        {
            string strResult = "";
            try
            {
                if (BodyType == "XML")
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(msg);

                    msg = Beautify(doc);
                }
                else
                {
                    msg = FormatJson(msg);
                }

                strResult = msg;

                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    RaiseRetriveResourceEvent(strResult);

                    this.tbkNew.Visibility = System.Windows.Visibility.Hidden;
                    this.isChecked = true;
                    this.sbCheckAnimi.Begin();
                }));
            }
            catch { }
        }
    }

    public class RetriveResourceEventArgs : EventArgs
    {
        public string MessageBody { get; set; }
        public string ResourceType { get; set; }

        public RetriveResourceEventArgs(string msg, string rt)
        {
            this.MessageBody = msg;
            this.ResourceType = rt;
        }
    }

    public class DeleteResourceEventArgs : EventArgs
    {
    }

    public class CreateResourceEventArgs : EventArgs
    {
        public OneM2MResource NewResource { get; set; }

        public CreateResourceEventArgs(OneM2MResource resc)
        {
            this.NewResource = resc;
        }
    }
}
