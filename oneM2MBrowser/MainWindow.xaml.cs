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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MobiusResourceMonitor_sub
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, IProgressChanged, IInitResourceManagerHanler
    {
        #region Parameter

        public static ConfigLoader Loader;

        private Dictionary<string, ucResource> ucResources = new Dictionary<string, ucResource>();
        private Dictionary<string, BlockObject> blocks = new Dictionary<string, BlockObject>();
        private List<LineObject> lines = new List<LineObject>();

        private Button btnDecoding;
        private ucResource ucRSelected;

        private ResourceManager rm;

        private Task updateTask;

        private bool isActived = false;
        private bool isStarted = false;
        
        private static double StartX = 50;
        private static double StartY = 100;
        private static double BlockWidth = 140;
        private static double BlockHeight = 40;
        private static double BlockHorizontalSpace = 80;
        private static double BlockVerticalSpace = 15;
        private static double LineLength = BlockHorizontalSpace / 2;

        private double CavHeight = 1000;
        private double CavWidth = 1200;
        private double MaxCinNum = 1;
        private int NodeDepthLevel = 0;
        private int NodeWidthLevel = 0;

        private double CurrentSourceX = 10;
        private double CurrentSourceY = 10;

        private double BlockMoveAnimDuration = 1000;

        private string command = "idel";
        private string contentType = "XML";

        private string rootUri = "";
        private string cseName = "";
        private string aeID = "";
        private string brokerIP = "";
        private string origin = "";

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            Loader = new ConfigLoader();
            Loader.initConf();
        }

        private void btnAppStart_Click(object sender, RoutedEventArgs e)
        {
            if (!isStarted)
            {
                try
                {
                    var mUri = new Uri(this.txtResourceUri.Text);

                    this.rootUri = this.txtResourceUri.Text.Replace(mUri.PathAndQuery, "");
                    this.cseName = mUri.AbsolutePath.Split('/')[1];

                    this.brokerIP = mUri.Host;

                    if (this.brokerIP == null || this.brokerIP.Length == 0)
                    {
                        return;
                    }

                    AEObject ae = new AEObject();
                    ae.RN = Loader.AppName;
                    ae.AppID = RandomString(15);
                    ae.AEID = "";

                    if (CreateAE(ae))
                    {
                        if (ae.AEID.Trim().Length == 0)
                        {
                            GetAE(ae);
                        }
                    }

                    if (ae.AEID.Trim().Length > 0)
                    {
                        this.aeID = ae.AEID;
                        this.origin = ae.AEID;

                        var result = IsEffectiveResource(this.txtResourceUri.Text);

                        if (result == ResourceEffectiveResult.DoNotExisted)
                        {
                            MessageBox.Show("Current resource is not effective!", "Limitaion", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        else if (result == ResourceEffectiveResult.UNKNOWN)
                        {
                            MessageBox.Show("Get unknown error when reqeust this resource!", "Unknown", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        else if (result == ResourceEffectiveResult.AccessDenied)
                        {
                            AcpInputWindow newForm = new AcpInputWindow();
                            if (newForm.ShowDialog().Value)
                            {
                                this.origin = newForm.ACP;

                                if (IsEffectiveResource(this.txtResourceUri.Text) == ResourceEffectiveResult.AccessDenied)
                                {
                                    MessageBox.Show("It is not a effective ACP...!", "Limitaion", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }
                            } else
                            {
                                return;
                            }
                        }

                        string path = mUri.AbsolutePath;
                        string[] rescAry = path.Split('/');

                        if (rescAry.Length <= 2 || rescAry[rescAry.Length - 1].Trim().Length == 0)
                        {
                            MessageBox.Show("I am sorry that you have not right to minitor current resource!", "Limitaion", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        SaveConfigFile();

                        rm = new ResourceManager(Loader.ResourcePath, Loader.AppName, this.aeID, this.origin);
                        rm.SetProtocolInfo("MQTT", this.brokerIP);
                        rm.SetProgressChangedHandler(this);
                        rm.SetInitHandler(this);

                        this.grdProgress.Visibility = System.Windows.Visibility.Visible;
                        this.IsEnabled = false;

                        this.txtResourceUri.IsEnabled = false;

                        rm.Start();

                        command = "working";
                    }
                }
                catch
                {
                    MessageBox.Show("There is a error when server was hosted!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    try
                    {
                        rm.Stop();
                        isActived = false;
                        updateTask.Dispose();
                    }
                    catch
                    {
                        Environment.Exit(0);
                    }

                    return;
                }
                this.btnAppStart.IsEnabled = false;
                this.btnAppStop.IsEnabled = true;

                isStarted = true;
            }
        }

        private void btnAppStop_Click(object sender, RoutedEventArgs e)
        {
            if (isStarted)
            {
                try
                {
                    rm.Stop();
                    isActived = false;
                    try
                    {
                        updateTask.Dispose();
                    }
                    catch (InvalidOperationException exp)
                    {
                        Debug.WriteLine(exp.Message);
                    }

                    this.txtResourceUri.IsEnabled = true;
                    this.txtMsgShow.Text = "";

                    this.cavBlockView.Height = this.CavHeight;
                    this.cavBlockView.Width = this.CavWidth;
                    this.cavLineView.Height = this.CavHeight;
                    this.cavLineView.Width = this.CavWidth;
                    this.cavSourceView.Height = this.CavHeight;
                    this.cavSourceView.Width = this.CavWidth;

                    this.lines.Clear();
                    this.blocks.Clear();
                    this.ucResources.Clear();
                    this.cavBlockView.Children.Clear();
                    this.cavLineView.Children.Clear();

                    ucRSelected = null;
                }
                catch
                {
                    MessageBox.Show("There is a error when server was stoped!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                this.btnAppStart.IsEnabled = true;
                this.btnAppStop.IsEnabled = false;
                isStarted = false;
            }
        }

        private void rdbtn1Latest_Checked(object sender, RoutedEventArgs e)
        {
            MaxCinNum = 1;
        }

        private void rdbtn3Latest_Checked(object sender, RoutedEventArgs e)
        {
            MaxCinNum = 3;
        }

        private void rdbtn5Latest_Checked(object sender, RoutedEventArgs e)
        {
            MaxCinNum = 5;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string version = null;
            try
            {
                //// get deployment version
                version = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            }
            catch (InvalidDeploymentException)
            {
                //// you cannot read publish version when app isn't installed 
                //// (e.g. during debug)
                version = "not installed";
            }
            this.Title = @"[oneM2M Browser-" + version + @"]";

            this.txtResourceUri.Text = Loader.GetResourcePath();
            //this.txtIP.Text = Loader.GetNotificationIP();
        }

        private void SaveConfigFile()
        {
            if (this.txtResourceUri.Text.Trim().Length > 0)
            {
                Loader.SetResourcePath(this.txtResourceUri.Text);
            }

            if (this.brokerIP.Length > 0)
            {
                Loader.SetNotificationIP(this.brokerIP);
            }

            Loader.initConf();

            this.txtResourceUri.Text = Loader.ResourcePath;
            this.brokerIP = Loader.GetNotificationIP();

            //MessageBox.Show("Configuration successfully!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string RandomString(int length)
        {
            string str = "abcdefghijklmnopqrstuvwxyz0123456789";

            var random = new Random(Guid.NewGuid().GetHashCode());

            var sb = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                int num = random.Next(str.Length);
                sb.Append(str.Substring(num, 1));
            }
            return sb.ToString();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            SaveConfigFile();

            MessageBox.Show("Configuration successfully!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ProgressChanged(double percent, string messgage)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (percent >= 0 && percent <= 100)
                {
                    this.pbTask.Value = percent;
                    this.tbkProgress.Text = messgage;
                }
            }));
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;

            if (rm != null && rm.Status != "stopped")
            {
                rm.Stop();
                command = "closing";
            }
            else if (rm == null || rm.Status == "stopped")
            {
                Environment.Exit(0);
            }
        }

        private void AddFrontLine(double block_x, double block_y)
        {
            double lineLength = BlockHorizontalSpace / 2;

            double x1 = block_x - lineLength;
            double y1 = block_y + BlockHeight / 2;
            double x2 = block_x;
            double y2 = block_y + BlockHeight / 2;

            lines.Add(new LineObject() { StartX = x1, StartY = y1, EndX = x2, EndY = y2 });
        }

        private void AddBackLine(double block_x, double block_y)
        {
            double lineLength = BlockHorizontalSpace / 2;

            double x1 = block_x + BlockWidth;
            double y1 = block_y + BlockHeight / 2;
            double x2 = block_x + BlockWidth + lineLength;
            double y2 = block_y + BlockHeight / 2;

            lines.Add(new LineObject() { StartX = x1, StartY = y1, EndX = x2, EndY = y2 });
        }

        private void CaculatorBlockAndLine(BlockObject rootBlock) {

            lines.Clear();
            blocks.Clear();

            NodeDepthLevel = 0;
            NodeWidthLevel = 0;

            ChildSeek(rootBlock);

            this.CavHeight = StartY + NodeDepthLevel * (BlockHeight + BlockVerticalSpace) + 50 ;
            this.CavWidth = StartX + NodeWidthLevel * (BlockWidth + BlockHorizontalSpace);
        }

        private BlockObject ChildSeek(BlockObject node)
        {
            BlockObject block = null;
            if (node != null && node.Resource!= null)
            {
                if (node.Parent == null)
                {
                    //Debug.WriteLine("Find a node[" + node.Resource.ResourcePath + "] in X:[" + node.Resource.Level + "] Y[" + NodeDepthLevel + "]");

                    node.PositionX = StartX;
                    node.PositionY = StartY;

                    blocks.Add(node.Resource.ResourcePath, node);

                    AddBackLine(node.PositionX, node.PositionY);
                }

                if (node != null && node.Childrens.Count > 0)
                {
                    for (int i = 0; i < node.Childrens.Count; i++)
                    {
                        //Debug.WriteLine("Find a node[" + node.Childrens[i].Resource.ResourcePath + "] in X:[" + node.Childrens[i].Resource.Level + "] Y[" + NodeDepthLevel + "]");

                        node.Childrens[i].PositionX = StartX + (node.Childrens[i].Resource.Level - 1) * (BlockWidth + BlockHorizontalSpace);

                        if(node.Childrens[i].Resource.Level > NodeWidthLevel)
                        {
                            NodeWidthLevel = node.Childrens[i].Resource.Level;
                        }

                        node.Childrens[i].PositionY = StartY + NodeDepthLevel * (BlockHeight + BlockVerticalSpace);

                        if (!blocks.ContainsKey(node.Childrens[i].Resource.ResourcePath))
                        {
                            blocks.Add(node.Childrens[i].Resource.ResourcePath, node.Childrens[i]);
                        }

                        AddFrontLine(node.Childrens[i].PositionX, node.Childrens[i].PositionY);

                        var s_x = node.PositionX + BlockWidth + LineLength;
                        var s_y = node.PositionY + BlockHeight / 2;
                        var e_x = node.Childrens[i].PositionX - LineLength;
                        var e_y = node.Childrens[i].PositionY + BlockHeight / 2;

                        lines.Add(new LineObject() { StartX = s_x, StartY = s_y, EndX = e_x, EndY = e_y });

                        if (node.Childrens[i].Childrens != null && node.Childrens[i].Childrens.Count > 0)
                        {
                            AddBackLine(node.Childrens[i].PositionX, node.Childrens[i].PositionY);
                        }

                        block = ChildSeek(node.Childrens[i]);

                        NodeDepthLevel++;

                        if (i == node.Childrens.Count - 1)
                        {
                            NodeDepthLevel--;
                        }
                    }
                }
            }

            return block;
        }

        private ResourceEffectiveResult IsEffectiveResource(string strResourcePath)
        {
            var result = ResourceEffectiveResult.UNKNOWN;

            try
            {
                var req = (HttpWebRequest)HttpWebRequest.Create(strResourcePath);
                req.Proxy = null;
                req.Method = "GET";
                req.Accept = "application/xml";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                req.Headers.Add("X-M2M-Origin", this.origin);

                var resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    result = ResourceEffectiveResult.OK;
                }
                else
                {
                    result = ResourceEffectiveResult.UNKNOWN;
                }
            }
            catch (WebException exp)
            {

                if (exp.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = exp.Response as HttpWebResponse;
                    if (response != null)
                    {
                        if (response.StatusCode == HttpStatusCode.Forbidden)
                        {
                            result = ResourceEffectiveResult.AccessDenied;
                        }
                        else if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            result = ResourceEffectiveResult.DoNotExisted;
                        }
                        else
                        {
                            result = ResourceEffectiveResult.UNKNOWN;
                        }
                    }
                }
                else
                {
                    result = ResourceEffectiveResult.UNKNOWN;
                }
            }

            return result;
        }

        private void UpdateChart()
        {
            isActived = true;
            while (isActived)
            {
                var resources = rm.GetAllResource();

                var lstResoureces = resources.ToList<ResourceObject>();

                var root = MakeTree(lstResoureces);

                CaculatorBlockAndLine(root);

                DrawBlock();

                DrawLine();

                Thread.Sleep(1000);
            }
        }

        private void ResizeCanvas()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.cavBlockView.Height = CavHeight;
                this.cavBlockView.Width = CavWidth;
                this.cavLineView.Height = CavHeight;
                this.cavLineView.Width = CavWidth;
                this.cavSourceView.Height = CavHeight;
                this.cavSourceView.Width = CavWidth;
            }));
        }

        private void DrawLine()
        {
            ResizeCanvas();

            this.Dispatcher.Invoke(new Action(() =>
            {

                this.cavLineView.Children.Clear();

                foreach (var lineInfo in lines)
                {
                    var line = new Line();

                    line.Stroke = Brushes.Green;
                    line.StrokeThickness = 3;
                    line.X1 = lineInfo.StartX;
                    line.Y1 = lineInfo.StartY;
                    line.X2 = lineInfo.EndX;
                    line.Y2 = lineInfo.EndY;

                    this.cavLineView.Children.Add(line);
                }
            }));
        }

        private void DrawBlock()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.cavBlockView.Height = CavHeight;

                foreach (var path in blocks.Keys)
                {
                    if (ucResources.ContainsKey(path))
                    {
                        ucResource resBlock = ucResources[path];

                        var block = blocks[path];

                        double prePositionX = Canvas.GetLeft(resBlock);
                        double prePositionY = Canvas.GetTop(resBlock);

                        Canvas.SetLeft(resBlock, block.PositionX);
                        Canvas.SetTop(resBlock, block.PositionY);

                        if (!Double.IsNaN(prePositionX) && !Double.IsNaN(prePositionY))
                        {
                            if (prePositionX != block.PositionX || prePositionY != block.PositionY)
                            {
                                //Debug.WriteLine("+++++++++++++++++++++++++++++++++++" + path);
                                MoveBlockAsAnimation(resBlock, prePositionX, prePositionY, block.PositionX, block.PositionY);
                            }
                        }
                        else
                        {
                            MoveBlockAsAnimation(resBlock, this.CurrentSourceX, this.CurrentSourceY, block.PositionX, block.PositionY);
                        }
                    }
                    else
                    {
                        var block = blocks[path];

                        ucResource resBlock = new ucResource(rm);
                        resBlock.Width = BlockWidth;
                        resBlock.Height = BlockHeight;

                        resBlock.ResourceName = block.Resource.ResourceName;
                        resBlock.ResourcePath = block.Resource.ResourcePath;
                        resBlock.ResourceType = block.Resource.ResourceType;

                        if (block.Resource.ResourceStatus == ResourceStatusOption.New)
                        {
                            resBlock.ShowNewTag();
                        }
                        else if (block.Resource.ResourceStatus == ResourceStatusOption.Old)
                        {
                            resBlock.ShowOldTag();
                        }

                        resBlock.BodyType = this.contentType;

                        resBlock.RetriveResoureceCompleted += (s, e) =>
                        {
                            var selectResource = s as ucResource;
                            this.ucRSelected = selectResource;

                            UnCheckOtherResources(selectResource);

                            this.txtMsgShow.Text = e.MessageBody;

                            if (e.ResourceType == "SemanticDescriptor")
                            {
                                btnDecoding = new Button();
                                btnDecoding.Content = "Decode Sementic";
                                btnDecoding.Margin = new Thickness(5);

                                btnDecoding.Click += (send, evn) =>
                                {
                                    DecodeSementicDescription(e.MessageBody);
                                };

                                Grid.SetRow(btnDecoding, 2);
                                Grid.SetColumn(btnDecoding, 0);

                                this.grdResourceInfo.Children.Add(btnDecoding);
                            }
                            else
                            {
                                if (btnDecoding != null)
                                {
                                    this.grdResourceInfo.Children.Remove(btnDecoding);
                                    btnDecoding = null;
                                }
                            }
                        };

                        resBlock.DeleteResourceCompleted += (s, e) =>
                        {
                            var selectResource = s as ucResource;
                            var r_name = selectResource.ResourceName;
                            var r_path = selectResource.ResourcePath;
                            var r_type = rm.EncodeResourceType(selectResource.ResourceType);
                            var p_path = r_path.Remove(r_path.LastIndexOf("/"), r_name.Length + 1);

                            rm.RemoveResource(r_name, p_path, r_type);

                        };

                        resBlock.CreateResourceCompleted += (s, e) =>
                        {
                            var selectResource = s as ucResource;
                            var r_name = selectResource.ResourceName;
                            var r_path = selectResource.ResourcePath;
                            var r_type = rm.EncodeResourceType(selectResource.ResourceType);
                            var p_path = r_path.Remove(r_path.LastIndexOf("/"), r_name.Length + 1);

                            if (e.NewResource.ResourceType == OneM2MResourceType.AE)
                            {
                                AEObject ae = e.NewResource as AEObject;

                                //Debug.WriteLine(ae.ToString(OneM2MResourceMessageType.XML));
                                if (rm.CreaetAE(r_path, ae))
                                {
                                    rm.AddResource(ae.RN, r_path, "2");
                                }
                            }
                            else if (e.NewResource.ResourceType == OneM2MResourceType.Container)
                            {
                                ContainerObject cnt = e.NewResource as ContainerObject;

                                if (rm.CreateContainer(r_path, cnt))
                                {
                                    rm.AddResource(cnt.RN, r_path, "3");
                                }
                            }
                            else if (e.NewResource.ResourceType == OneM2MResourceType.ContentInstance)
                            {
                                ContentInstanceObject cin = e.NewResource as ContentInstanceObject;

                                if (rm.CreateContentInstance(r_path, cin))
                                {
                                    rm.AddResource(cin.RN, r_path, "4");
                                }
                            }
                            else if (e.NewResource.ResourceType == OneM2MResourceType.Subscription)
                            {
                                SubscriptionObject sub = e.NewResource as SubscriptionObject;

                                if(rm.CreateSubscription(r_path, sub))
                                {
                                    rm.AddResource(sub.RN, r_path, "23");
                                }
                            }
                            else if (e.NewResource.ResourceType == OneM2MResourceType.Group)
                            {
                                GroupObject grp = e.NewResource as GroupObject;

                                if (rm.CreateGroup(r_path, grp))
                                {
                                    rm.AddResource(grp.RN, r_path, "9");
                                }
                            }
                            else if(e.NewResource.ResourceType == OneM2MResourceType.TimeSeries)
                            {
                                TimeSeriesObject ts = e.NewResource as TimeSeriesObject;

                                if(rm.CreateTimeSeries(r_path, ts))
                                {
                                    rm.AddResource(ts.RN, r_path, "29");
                                }
                            }
                            else if (e.NewResource.ResourceType == OneM2MResourceType.TimeSeriesContentInstance)
                            {
                                TimeSeriesContentInstanceObject tsi = e.NewResource as TimeSeriesContentInstanceObject;

                                if(rm.CreateTimeSeriesContentInstance(r_path, tsi))
                                {
                                    rm.AddResource(tsi.RN, r_path, "30");
                                }
                            }
                            else if(e.NewResource.ResourceType == OneM2MResourceType.SemanticDescriptor)
                            {
                                SemanticDescriptorObject sd = e.NewResource as SemanticDescriptorObject;

                                if (rm.CreateSemanticDescriptor(r_path, sd))
                                {
                                    rm.AddResource(sd.RN, r_path, "24");
                                }
                            }
                        };

                        Canvas.SetLeft(resBlock, block.PositionX);
                        Canvas.SetTop(resBlock, block.PositionY);

                        this.cavBlockView.Children.Add(resBlock);
                        ucResources.Add(resBlock.ResourcePath, resBlock);

                        //Debug.WriteLine("Resource Path is " + resBlock.ResourcePath);

                        if (block.Resource.ResourceStatus == ResourceStatusOption.New)
                        {
                            MoveBlockAsAnimation(resBlock, this.CurrentSourceX, this.CurrentSourceY, block.PositionX, block.PositionY);
                        }
                    }
                }

                var paths = ucResources.Keys.ToArray();

                for (int i = 0; i < paths.Length; i++)
                {
                    if (!blocks.ContainsKey(paths[i]))
                    {
                        var uc = ucResources[paths[i]];
                        ucResources.Remove(paths[i]);
                        this.cavBlockView.Children.Remove(uc);
                    }
                }
            }));
        }

        private void MoveBlockAsAnimation(ucResource resc,double sx, double sy, double ex, double ey)
        {
            var sb = new Storyboard();
            sb.FillBehavior = FillBehavior.Stop;

            var daX = new DoubleAnimation();
            var daY = new DoubleAnimation();

            daX.From = sx;
            daY.From = sy;

            daX.To = ex;
            daY.To = ey;

            daX.EasingFunction = new CircleEase { EasingMode = EasingMode.EaseInOut };
            daY.EasingFunction = new CircleEase { EasingMode = EasingMode.EaseInOut };

            daX.Duration = TimeSpan.FromMilliseconds(BlockMoveAnimDuration);
            daY.Duration = TimeSpan.FromMilliseconds(BlockMoveAnimDuration);

            Storyboard.SetTarget(daX, resc);
            Storyboard.SetTarget(daY, resc);

            Storyboard.SetTargetProperty(daX, new PropertyPath("(Canvas.Left)"));
            Storyboard.SetTargetProperty(daY, new PropertyPath("(Canvas.Top)"));


            sb.Children.Add(daX);
            sb.Children.Add(daY);

            if (!Resources.Contains("rectAnimation"))
            {
                Resources.Add("rectAnimation", sb);
            } else
            {
                Resources.Remove("rectAnimation");
            }

            sb.Begin();
        }

        private void UnCheckOtherResources(ucResource current)
        {
            foreach(var key in ucResources.Keys)
            {
                if (current != ucResources[key])
                {
                    ucResources[key].Uncheck();
                }
            }
        }

        private bool IsAeExisted(AEObject ae)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUri).Append(@"/").Append(this.cseName).Append(@"/").Append(ae.RN);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [GET] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "GET";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=2";
                req.Accept = "application/xml";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                req.Headers.Add("X-M2M-Origin", "S");
                req.Headers.Add("nmtype", "short");

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    {
                        string content = sr.ReadToEnd();
                        sr.Close();

                        ae.Parse(content, OneM2MResourceMessageType.XML);

                        bResult = true;
                    }
                }
                resp.Close();
            }
            catch(Exception exp)
            {
                Debug.WriteLine(exp.Message);
                bResult = false;
            }

            return bResult;
        }

        private bool GetAE(AEObject ae)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUri).Append(@"/").Append(this.cseName).Append(@"/").Append(ae.RN);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [GET] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "GET";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=2";
                req.Accept = "application/xml";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                req.Headers.Add("X-M2M-Origin", "S");

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    {
                        string content = sr.ReadToEnd();
                        sr.Close();

                        ae.Parse(content, OneM2MResourceMessageType.XML);

                        bResult = true;
                    }
                }
                resp.Close();
            }
            catch(WebException exp)
            {
                bResult = false;
            }

            return bResult;
        }

        private bool CreateAE(AEObject ae)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUri).Append(@"/").Append(this.cseName);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [POST] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "POST";
                req.Accept = "application/xml";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=2";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                req.Headers.Add("X-M2M-Origin", "C" + Loader.AppName);

                string req_content = ae.ToString(OneM2MResourceMessageType.XML);

                using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(req_content);
                    sw.Flush();
                    sw.Close();
                }

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.Created || resp.StatusCode == HttpStatusCode.Conflict)
                {
                    bResult = true;

                    if (resp.StatusCode == HttpStatusCode.Created)
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            string resp_content = sr.ReadToEnd();
                            sr.Close();

                            ae.Parse(resp_content, OneM2MResourceMessageType.XML);

                            bResult = true;
                        }
                    }
                }
                resp.Close();
            }
            catch (WebException exp)
            {
                if (exp.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = exp.Response as HttpWebResponse;
                    if (response != null)
                    {
                        if (response.StatusCode == HttpStatusCode.Conflict)
                        {
                            bResult = true;
                        }
                    }
                }
                Debug.WriteLine(exp.Message);
            }

            return bResult;
        }

        private bool DeleteAppAE(string aeName)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUri).Append(@"/").Append(this.cseName).Append(@"/").Append(aeName);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [DELETE] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "DELETE";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=2";
                req.Accept = "application/xml";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                req.Headers.Add("X-M2M-Origin", "C" + Loader.AppName);
                req.Headers.Add("nmtype", "short");

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    bResult = true;
                }
                resp.Close();
            }
            catch(Exception exp)
            {
                Debug.WriteLine(exp.Message);
                bResult = false;
            }

            return bResult;
        }

        private void DecodeSementicDescription(string msg)
        {
            SemanticDescriptorObject sd = new SemanticDescriptorObject();
            if (this.contentType == "XML")
            {
                sd.Parse(msg, OneM2MResourceMessageType.XML);
                var window = new SemanticDisplayWindow(sd.DSPT);
                window.Show();
            }
            else if (this.contentType == "Json")
            {
                sd.Parse(msg, OneM2MResourceMessageType.JSON);
                var window = new SemanticDisplayWindow(sd.DSPT);
                window.Show();
            }
        }

        public void InitResourceManagerCompeleted()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                //form.Close();
                this.grdProgress.Visibility = System.Windows.Visibility.Hidden;
                this.IsEnabled = true;
            }));

            updateTask = new Task(new Action(UpdateChart));
            updateTask.Start();

        }

        private bool IsContains(string content, List<string> filters)
        {
            var isResult = false;

            for (int i = 0; i < filters.Count; i++)
            {
                if (content == filters[i])
                {
                    isResult = true;
                    break;
                }
            }

            return isResult;
        }
        
        private BlockObject MakeTree(List<ResourceObject> totalResources)
        {
            var rootBlock = new BlockObject();

            int maxLevel = 0;

            if (totalResources.Count >= 1)
            {
                for (int i = 0; i < totalResources.Count; i++)
                {
                    if (totalResources[i].Level > maxLevel)
                    {
                        maxLevel = totalResources[i].Level;
                    }
                }

                var rescMetrix = new List<ResourceObject>[maxLevel];

                for (int i = 1; i <= maxLevel; i++)
                {
                    var rescList = new List<ResourceObject>();
                    for (int j = 0; j < totalResources.Count; j++)
                    {                       
                        if (totalResources[j].Level == i)
                        {
                            rescList.Add(totalResources[j]);
                        }
                    }

                    rescMetrix[i-1] = rescList;
                }

                List<BlockObject> blockList = null;

                for (int i = 1; i < rescMetrix.Length; i++)
                {
                    if (i == 1)
                    {
                        blockList = new List<BlockObject>();
                        rootBlock.Resource = rescMetrix[0][0];
                        rootBlock.Parent = null;
                        blockList.Add(rootBlock);
                    }

                    var temp = new List<BlockObject>();

                    for (int j = 0; j < rescMetrix[i].Count; j++)
                    {
                        for (int x = 0; x < blockList.Count; x++)
                        {
                            if (rescMetrix[i][j].ParentPath == blockList[x].Resource.ResourcePath)
                            {
                                var block = new BlockObject();
                                block.Resource = rescMetrix[i][j];
                                block.Parent = blockList[x];

                                if (blockList[x].Resource.ResourceType == "Container"
                                    && rescMetrix[i][j].ResourceType == "ContentInstance")
                                {
                                    if (!isOverMaxCinNum(blockList[x].Childrens))
                                    {
                                        blockList[x].Childrens.Add(block);
                                        temp.Add(block);
                                    }
                                }
                                else
                                {
                                    blockList[x].Childrens.Add(block);
                                    temp.Add(block);
                                }
                            }
                        }                                   
                    }

                    blockList = temp;
                }
            }
            return rootBlock;
        }
        
        private bool isOverMaxCinNum(List<BlockObject> childrens)
        {
            int count = 0;
            for (int i = 0; i < childrens.Count; i++)
            {
                if (childrens[i].Resource.ResourceType == "ContentInstance")
                {
                    count++;
                }
            }
            if (count >= MaxCinNum)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void StopResourceManagerBegined()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.grdProgress.Visibility = System.Windows.Visibility.Visible;
                this.IsEnabled = false;
            }));
        }

        public void StopResourceManagerCompeleted()
        {

            DeleteAppAE(Loader.AppName);

            this.Dispatcher.Invoke(new Action(() =>
            {
                this.grdProgress.Visibility = System.Windows.Visibility.Hidden;
                this.IsEnabled = true;

                if (command == "closing")
                {
                    Environment.Exit(0);
                }
            }));
        }

        private void mnAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow mForm = new AboutWindow();
            mForm.ShowDialog();
        }

        private void scrvDisplay_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            this.CurrentSourceX += e.HorizontalChange;
            this.CurrentSourceY += e.VerticalChange;

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                Canvas.SetLeft(this.imgNetwork, this.CurrentSourceX);
                Canvas.SetTop(this.imgNetwork, this.CurrentSourceY);
            }));
        }

        private void rdbtnJson_Checked(object sender, RoutedEventArgs e)
        {
            this.contentType = "Json";

            foreach(string key in ucResources.Keys)
            {
                ucResources[key].BodyType = this.contentType;
            }

            if(this.ucRSelected != null)
            {
                ucRSelected.RequestResourceInfo();
            }
        }

        private void rdbtnXML_Checked(object sender, RoutedEventArgs e)
        {
            this.contentType = "XML";

            foreach (string key in ucResources.Keys)
            {
                ucResources[key].BodyType = this.contentType;
            }

            if (this.ucRSelected != null)
            {
                ucRSelected.RequestResourceInfo();
            }
        }

        private void slZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.tbkZoom.Text = Math.Round(this.slZoom.Value) + @"%";

                this.stLineView.CenterX = CavWidth / 2;

                this.stBlockView.CenterX = CavWidth / 2;

                this.stSourceView.CenterX = CavWidth / 2;

                this.stLineView.ScaleX = this.slZoom.Value / 100;
                this.stLineView.ScaleY = this.slZoom.Value / 100;

                this.stBlockView.ScaleX = this.slZoom.Value / 100;
                this.stBlockView.ScaleY = this.slZoom.Value / 100;

                this.stSourceView.ScaleX = this.slZoom.Value / 100;
                this.stSourceView.ScaleY = this.slZoom.Value / 100;
            }));
        }
    }

    public enum ResourceEffectiveResult
    {
        DoNotExisted, AccessDenied, OK, UNKNOWN
    }

    public class BlockObject
    {
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public ResourceObject Resource { get; set; }
        public BlockObject Parent { get; set; }
        public List<BlockObject> Childrens = new List<BlockObject>();
    }

    public class LineObject
    {
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
    }
}
