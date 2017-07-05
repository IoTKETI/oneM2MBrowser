using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace MobiusResourceMonitor_sub
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, IProgressChanged, IInitResourceManagerHanler
    {
        public static ConfigLoader Loader;

        private List<LineObject> lines = new List<LineObject>();
        private Dictionary<string, ucResource> ucResources = new Dictionary<string, ucResource>();
        private Dictionary<string, BlockObject> blocks = new Dictionary<string, BlockObject>();
        private Button btnDecoding;
        //private NotificationHttpServer httpServer;

        private ResourceManager rm;
        //private ucProccessView form;
        private bool isActived = false;
        private Task updateTask;

        private bool isStarted = false;
        private bool isFirstTime = true;

        private List<string> lv2Filter = new List<string>();
        private List<string> lv3Filter = new List<string>();
        private List<string> lv4Filter = new List<string>();

        private double StartX = 50;
        private double StartY = 100;
        private double BlockWidth = 140;
        private double BlockHeight = 40;
        private double BlockHorizontalSpace = 80;
        private double BlockVerticalSpace = 15;
        private double CavHeight = 800;
        private double MaxCinNum = 1;

        private string command = "idel";

        public MainWindow()
        {
            InitializeComponent();


            Loader = new ConfigLoader();
            Loader.initConf();

            this.txtServerUri.Text = Loader.GetServerUri();
            this.txtCseName.Text = Loader.GetCseBase();
            this.tbkCseName.Text = Loader.GetCseBase();
            string protocolType = Loader.GetNotificationProtocol();
            this.txtIP.Text = Loader.GetNotificationIP();
        }

        private void btnAppStart_Click(object sender, RoutedEventArgs e)
        {
            if (!isStarted)
            {
                try
                {
                    SaveConfigFile();

                    rm = new ResourceManager(this.txtServerUri.Text, this.tbkCseName.Text, Loader.AppName);
                    rm.SetProtocolInfo("MQTT", this.txtIP.Text);
                    rm.SetProgressChangedHandler(this);
                    rm.SetInitHandler(this);

                    this.grdProgress.Visibility = System.Windows.Visibility.Visible;
                    this.IsEnabled = false;

                    this.txtServerUri.IsEnabled = false;
                    this.txtCseName.IsEnabled = false;
                    this.txtIP.IsEnabled = false;
                    this.btnApply.IsEnabled = false;

                    rm.Start();

                    isFirstTime = true;

                    command = "working";
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
                    this.txtServerUri.IsEnabled = true;
                    this.txtCseName.IsEnabled = true;
                    this.txtIP.IsEnabled = true;
                    this.btnApply.IsEnabled = true;

                    this.lines.Clear();
                    this.blocks.Clear();
                    this.ucResources.Clear();
                    this.cavBlockView.Children.Clear();
                    this.cavLineView.Children.Clear();

                    isFirstTime = true;
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

        private void rdbtnAll_Checked(object sender, RoutedEventArgs e)
        {
            MaxCinNum = 5;
            isFirstTime = true;
        }

        private void rdbtnNone_Checked(object sender, RoutedEventArgs e)
        {
            MaxCinNum = 0;
            isFirstTime = true;
        }

        private void rdbtnLatest_Checked(object sender, RoutedEventArgs e)
        {
            MaxCinNum = 1;
            isFirstTime = true;
        }

        private void rdbtn5Latest_Checked(object sender, RoutedEventArgs e)
        {
            MaxCinNum = 5;
            isFirstTime = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.txtServerUri.Text = Loader.GetServerUri();
            this.txtCseName.Text = Loader.GetCseBase();
            this.tbkCseName.Text = Loader.GetCseBase();
            string protocolType = Loader.GetNotificationProtocol();

            this.txtIP.Text = Loader.GetNotificationIP();
        }

        private void SaveConfigFile()
        {
            if (this.txtServerUri.Text.Trim().Length > 0)
            {
                Loader.SetServerUri(this.txtServerUri.Text);
            }

            if (this.txtCseName.Text.Trim().Length > 0)
            {
                Loader.SetCseBase(this.txtCseName.Text);
            }

            if (this.txtIP.Text.Trim().Length > 0)
            {
                Loader.SetNotificationIP(this.txtIP.Text);
            }

            Loader.initConf();

            this.txtServerUri.Text = Loader.RootUrl;
            this.txtCseName.Text = Loader.CseBaseName;
            this.tbkCseName.Text = Loader.CseBaseName;
            string protocolType = Loader.GetNotificationProtocol();

            this.txtIP.Text = Loader.GetNotificationIP();

            //MessageBox.Show("Configuration successfully!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            SaveConfigFile();

            MessageBox.Show("Configuration successfully!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            lv2Filter.Clear();
            lv3Filter.Clear();
            lv4Filter.Clear();

            var lv2FilterText = this.txtFltLv2.Text.ToLower();
            var lv3FilterText = this.txtFltLv3.Text.ToLower();
            var lv4FilterText = this.txtFltLv4.Text.ToLower();

            if (lv2FilterText.Contains(','))
            {
                string[] strAry = lv2FilterText.Split(',');

                for (int i = 0; i < strAry.Length; i++)
                {
                    lv2Filter.Add(strAry[i]);
                }
            }
            else if (lv2FilterText.Trim().Length > 0 )
            {
                lv2Filter.Add(lv2FilterText);
            }

            if (lv3FilterText.Contains(','))
            {
                string[] strAry = lv3FilterText.Split(',');

                for (int i = 0; i < strAry.Length; i++)
                {
                    lv3Filter.Add(strAry[i]);
                }
            }
            else if (lv3FilterText.Trim().Length > 0 )
            {
                lv3Filter.Add(lv3FilterText);
            }

            if (lv4FilterText.Contains(','))
            {
                string[] strAry = lv4FilterText.Split(',');

                for (int i = 0; i < strAry.Length; i++)
                {
                    lv4Filter.Add(strAry[i]);
                }
            }
            else if (lv4FilterText.Trim().Length > 0 )
            {
                lv4Filter.Add(lv4FilterText);
            }

            isFirstTime = true;
        }

        public void ProgressChanged(double percent, string messgage)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (percent > 0 && percent < 100)
                {
                    if (percent >= 0 && percent <= 100)
                    {
                        this.pbTask.Value = percent;
                        this.tbkProgress.Text = messgage;
                    }
                    //form.setProgressValue(percent);
                    //form.setProgressStatus(messgage);
                }
            }));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
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

        private void CaculatorBlockAndLine(BlockObject rootBlock)
        {
            lines.Clear();
            blocks.Clear();

            double lv1_x = StartX;
            double lv2_x = StartX + BlockWidth + BlockHorizontalSpace;
            double lv3_x = StartX + (BlockWidth + BlockHorizontalSpace) * 2;
            double lv4_x = StartX + (BlockWidth + BlockHorizontalSpace) * 3;
            double lv5_x = StartX + (BlockWidth + BlockHorizontalSpace) * 4;
            double lv6_x = StartX + (BlockWidth + BlockHorizontalSpace) * 5;

            double lineLength = BlockHorizontalSpace / 2;

            double tag_y = StartY;

            rootBlock.PositionX = lv1_x;
            rootBlock.PositionY = tag_y;

            if (!blocks.ContainsKey(rootBlock.Resource.ResourcePath))
            {
                blocks.Add(rootBlock.Resource.ResourcePath, rootBlock);
            }

            if (rootBlock.Childrens.Count > 0)
            {
                AddBackLine(lv1_x, tag_y);
            }

            if (rootBlock.Childrens.Count > 0)
            {
                double lv1_e_x = lv2_x - lineLength;
                double lv1_e_y = tag_y + BlockHeight / 2;

                foreach (BlockObject lv2Block in rootBlock.Childrens)
                {
                    lv2Block.PositionX = lv2_x;
                    lv2Block.PositionY = tag_y;

                    if (!blocks.ContainsKey(lv2Block.Resource.ResourcePath))
                    {
                        blocks.Add(lv2Block.Resource.ResourcePath, lv2Block);
                    }

                    AddFrontLine(lv2_x, tag_y);

                    double lv1_s_x = lv2_x - lineLength;
                    double lv1_s_y = tag_y + BlockHeight / 2;

                    lines.Add(new LineObject() { StartX = lv1_s_x, StartY = lv1_s_y, EndX = lv1_e_x, EndY = lv1_e_y });

                    if (lv2Block.Childrens.Count > 0)
                    {
                        AddBackLine(lv2_x, tag_y);
                    }

                    tag_y = tag_y + BlockHeight + BlockVerticalSpace;


                    if (lv2Block.Childrens.Count > 0)
                    {
                        tag_y = tag_y - BlockHeight - BlockVerticalSpace;

                        double lv2_e_x = lv3_x - lineLength;
                        double lv2_e_y = tag_y + BlockHeight / 2;

                        foreach (BlockObject lv3Block in lv2Block.Childrens)
                        {
                            lv3Block.PositionX = lv3_x;
                            lv3Block.PositionY = tag_y;

                            if (!blocks.ContainsKey(lv3Block.Resource.ResourcePath))
                            {
                                blocks.Add(lv3Block.Resource.ResourcePath, lv3Block);
                            }

                            AddFrontLine(lv3_x, tag_y);

                            double lv2_s_x = lv3_x - lineLength;
                            double lv2_s_y = tag_y + BlockHeight / 2;

                            lines.Add(new LineObject() { StartX = lv2_s_x, StartY = lv2_s_y, EndX = lv2_e_x, EndY = lv2_e_y });

                            if (lv3Block.Childrens.Count > 0)
                            {
                                AddBackLine(lv3_x, tag_y);
                            }

                            tag_y = tag_y + BlockHeight + BlockVerticalSpace;

                            if (lv3Block.Childrens.Count > 0)
                            {
                                tag_y = tag_y - BlockHeight - BlockVerticalSpace;

                                double lv3_e_x = lv4_x - lineLength;
                                double lv3_e_y = tag_y + BlockHeight / 2;

                                foreach (BlockObject lv4Block in lv3Block.Childrens)
                                {
                                    lv4Block.PositionX = lv4_x;
                                    lv4Block.PositionY = tag_y;

                                    if (!blocks.ContainsKey(lv4Block.Resource.ResourcePath))
                                    {
                                        blocks.Add(lv4Block.Resource.ResourcePath, lv4Block);
                                    }

                                    AddFrontLine(lv4_x, tag_y);

                                    double lv3_s_x = lv4_x - lineLength;
                                    double lv3_s_y = tag_y + BlockHeight / 2;

                                    lines.Add(new LineObject() { StartX = lv3_s_x, StartY = lv3_s_y, EndX = lv3_e_x, EndY = lv3_e_y });

                                    if (lv4Block.Childrens.Count > 0)
                                    {
                                        AddBackLine(lv4_x, tag_y);
                                    }

                                    /*
                                    if (lv4_res.ResourceName == "latest")
                                    {
                                        LatestObject latest = new LatestObject();
                                        latest.PositionX = lv5_x;
                                        latest.PositionY = tag_y;
                                        latest.LatestPath = lv4_res.ResourcePath;

                                        latestViews.Add(latest);

                                        tag_y = tag_y + LatestViewHeight + BlockVerticalSpace;
                                    }
                                     * */

                                    tag_y = tag_y + BlockHeight + BlockVerticalSpace;

                                    if (lv4Block.Childrens.Count > 0)
                                    {
                                        tag_y = tag_y - BlockHeight - BlockVerticalSpace;

                                        double lv4_e_x = lv5_x - lineLength;
                                        double lv4_e_y = tag_y + BlockHeight / 2;

                                        foreach (BlockObject lv5Block in lv4Block.Childrens)
                                        {
                                            lv5Block.PositionX = lv5_x;
                                            lv5Block.PositionY = tag_y;

                                            if (!blocks.ContainsKey(lv5Block.Resource.ResourcePath))
                                            {
                                                blocks.Add(lv5Block.Resource.ResourcePath, lv5Block);
                                            }

                                            AddFrontLine(lv5_x, tag_y);

                                            double lv4_s_x = lv5_x - lineLength;
                                            double lv4_s_y = tag_y + BlockHeight / 2;

                                            lines.Add(new LineObject() { StartX = lv4_s_x, StartY = lv4_s_y, EndX = lv4_e_x, EndY = lv4_e_y });

                                            tag_y = tag_y + BlockHeight + BlockVerticalSpace;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (tag_y > CavHeight)
            {
                CavHeight = tag_y + 50;
            }
        }

        private void UpdateChart()
        {
            isActived = true;
            while (isActived)
            {
                ResourceObject[] resources = rm.GetAllResource();
                //Debug.WriteLine("Get all resourece [" + resources.Length + "]");

                List<ResourceObject> lstResoureces = resources.ToList<ResourceObject>();

                BlockObject root = MakeTree(lstResoureces);

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
                this.cavLineView.Height = CavHeight;
            }));
        }

        private void DrawLine()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.cavLineView.Height = CavHeight;

                this.cavLineView.Children.Clear();

                foreach (LineObject lineInfo in lines)
                {
                    Line line = new Line();

                    line.Stroke = Brushes.Red;
                    line.StrokeThickness = 1;
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

                foreach (string path in blocks.Keys)
                {
                    ucResource resBlock = null;

                    if (ucResources.ContainsKey(path))
                    {
                        resBlock = ucResources[path];

                        BlockObject block = blocks[path];

                        Canvas.SetLeft(resBlock, block.PositionX);
                        Canvas.SetTop(resBlock, block.PositionY);
                    }
                    else
                    {
                        BlockObject block = blocks[path];

                        resBlock = new ucResource(Loader.GetServerUri());
                        resBlock.Width = BlockWidth;
                        resBlock.Height = BlockHeight;
                        if (!isFirstTime)
                        {
                            resBlock.ShowNewTag();
                        }
                        resBlock.ResourceName = block.Resource.ResourceName;
                        resBlock.ResourcePath = block.Resource.ResourcePath;
                        resBlock.ResourceType = block.Resource.ResourceType;

                        resBlock.RetriveResoureceCompleted += (s, e) =>
                        {
                            ucResource selectResource = s as ucResource;

                            UnCheckOtherResources(selectResource);

                            this.txtMsgShow.Text = e.MessageBody;

                            if (e.ResourceType == "SemanticDescription")
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

                        Canvas.SetLeft(resBlock, block.PositionX);
                        Canvas.SetTop(resBlock, block.PositionY);

                        this.cavBlockView.Children.Add(resBlock);
                        ucResources.Add(resBlock.ResourcePath, resBlock);
                    }

                }

                List<string> lstDeletePath = new List<string>();

                string[] paths = ucResources.Keys.ToArray();

                for (int i = 0; i < paths.Length; i++)
                {
                    if (!blocks.ContainsKey(paths[i]))
                    {
                        ucResource uc = ucResources[paths[i]];
                        ucResources.Remove(paths[i]);
                        this.cavBlockView.Children.Remove(uc);
                    }
                }
                isFirstTime = false;
            }));
        }

        private void UnCheckOtherResources(ucResource current)
        {
            foreach(string key in ucResources.Keys)
            {
                if (current != ucResources[key])
                {
                    ucResources[key].Uncheck();
                }
            }
        }

        private void DecodeSementicDescription(string msg)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(msg);

            XmlNamespaceManager nmspc = new XmlNamespaceManager(doc.NameTable);
            nmspc.AddNamespace("m2m", "http://www.onem2m.org/xml/protocols");
            nmspc.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");

            XmlNode ndDspt = doc.SelectSingleNode("m2m:sd/dspt", nmspc);

            string dspt_base64 = ndDspt.InnerText;
            string dspt = Encoding.UTF8.GetString(Convert.FromBase64String(dspt_base64));

            SemanticDisplayWindow window = new SemanticDisplayWindow(dspt);
            window.Show();
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
            bool isResult = false;

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
            BlockObject rootBlock = new BlockObject();

            if (totalResources.Count >= 1)
            {
                ResourceObject root = totalResources[0];

                rootBlock.Resource = root;

                for (int i = 0; i < totalResources.Count; i++)
                {
                    if (rootBlock.Resource.ResourcePath == totalResources[i].ParentPath)
                    {
                        BlockObject lv1 = new BlockObject();
                        lv1.Resource = totalResources[i];

                        //if (!lv1.Resource.ResourceName.Contains(lv2Filter)
                        //  && lv2Filter.Length != 0)
                        //{
                        //    continue;
                        //}

                        if (!IsContains(lv1.Resource.ResourceName, lv2Filter) && lv2Filter.Count != 0)
                        {
                            continue;
                        }

                        if (rootBlock.Resource.ResourceType == "Container"
                            && lv1.Resource.ResourceType == "ContentInstance")
                        {
                            if (!isOverMaxCinNum(rootBlock.Childrens))
                            {
                                rootBlock.Childrens.Add(lv1);
                            }
                        }
                        else
                        {
                            rootBlock.Childrens.Add(lv1);
                        }

                        totalResources.RemoveAt(i);
                        i--;

                        for (int j = 0; j < totalResources.Count; j++)
                        {
                            if (lv1.Resource.ResourcePath == totalResources[j].ParentPath)
                            {
                                BlockObject lv2 = new BlockObject();
                                lv2.Resource = totalResources[j];

                                //if (!lv2.Resource.ResourceName.Contains(lv3Filter)
                                //    && lv3Filter.Length != 0)
                                //{
                                //    continue;
                                //}

                                if (IsContains(lv2.Resource.ResourceName, lv3Filter) && lv3Filter.Count != 0)
                                {
                                    continue;
                                }

                                if (lv1.Resource.ResourceType == "Container"
                                    && lv2.Resource.ResourceType == "ContentInstance")
                                {
                                    if (!isOverMaxCinNum(lv1.Childrens))
                                    {
                                        lv1.Childrens.Add(lv2);
                                    }
                                }
                                else
                                {
                                    lv1.Childrens.Add(lv2);
                                }

                                totalResources.RemoveAt(j);
                                j--;

                                for (int x = 0; x < totalResources.Count; x++)
                                {
                                    if (lv2.Resource.ResourcePath == totalResources[x].ParentPath)
                                    {
                                        BlockObject lv3 = new BlockObject();
                                        lv3.Resource = totalResources[x];

                                        //if (!lv3.Resource.ResourceName.Contains(lv4Filter)
                                        //&& lv4Filter.Length != 0)
                                        //{
                                        //    continue;
                                        //}

                                        if (IsContains(lv3.Resource.ResourceName, lv4Filter) && lv4Filter.Count != 0)
                                        {
                                            continue;
                                        }

                                        if (lv2.Resource.ResourceType == "Container"
                                            && lv3.Resource.ResourceType == "ContentInstance")
                                        {
                                            if (!isOverMaxCinNum(lv2.Childrens))
                                            {
                                                lv2.Childrens.Add(lv3);
                                            }
                                        }
                                        else
                                        {
                                            lv2.Childrens.Add(lv3);
                                        }

                                        //lv2.Childrens.Add(lv3);

                                        totalResources.RemoveAt(x);
                                        x--;

                                        for (int z = 0; z < totalResources.Count; z++)
                                        {
                                            if (lv3.Resource.ResourcePath == totalResources[z].ParentPath)
                                            {
                                                BlockObject lv4 = new BlockObject();
                                                lv4.Resource = totalResources[z];

                                                if (lv3.Resource.ResourceType == "Container"
                                                    && lv4.Resource.ResourceType == "ContentInstance")
                                                {
                                                    if (!isOverMaxCinNum(lv3.Childrens))
                                                    {
                                                        lv3.Childrens.Add(lv4);
                                                    }
                                                }
                                                else
                                                {
                                                    lv3.Childrens.Add(lv4);
                                                }

                                                totalResources.RemoveAt(z);
                                                z--;

                                                for (int y = 0; y < totalResources.Count; y++)
                                                {
                                                    if (lv4.Resource.ResourcePath == totalResources[y].ParentPath)
                                                    {
                                                        BlockObject lv5 = new BlockObject();
                                                        lv5.Resource = totalResources[y];

                                                        if (lv4.Resource.ResourceType == "Container"
                                                        && lv5.Resource.ResourceType == "ContentInstance")
                                                        {
                                                            if (!isOverMaxCinNum(lv4.Childrens))
                                                            {
                                                                lv4.Childrens.Add(lv5);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            lv4.Childrens.Add(lv5);
                                                        }

                                                        totalResources.RemoveAt(y);
                                                        y--;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
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
                //form = new ucProccessView();
                //form.Show();
                this.grdProgress.Visibility = System.Windows.Visibility.Visible;
                this.IsEnabled = false;
            }));
        }

        public void StopResourceManagerCompeleted()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                //form.Close();
                this.grdProgress.Visibility = System.Windows.Visibility.Hidden;
                this.IsEnabled = true;

                if (command == "closing")
                {
                    Environment.Exit(0);
                }
            }));
        }
    }

    public class BlockObject
    {
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public ResourceObject Resource { get; set; }
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
