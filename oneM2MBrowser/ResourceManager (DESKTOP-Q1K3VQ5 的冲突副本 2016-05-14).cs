using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MobiusResourceMonitor_sub
{
    class ResourceManager : INotificaitonReceiver
    {
        private const int MaxContentInstanceCount = 25;

        private NotifiacitonMqttServer mqttServer;

        private Dictionary<string, ResourceObject> lstAe = new Dictionary<string, ResourceObject>();
        private Dictionary<string, ResourceObject> lstCon = new Dictionary<string, ResourceObject>();
        private List<ResourceObject> lstCin = new List<ResourceObject>();
        private Dictionary<string, ResourceObject> lstSub = new Dictionary<string, ResourceObject>();
        private Dictionary<string, ResourceObject> lstSem = new Dictionary<string, ResourceObject>();

        private List<Task> lstTasks = new List<Task>();

        private string strReqAeUrl = "";
        private string strReqConUrl = "";
        private string strReqCinUrl = "";
        private string strReqSubUrl = "";
        private string strReqSemUrl = "";
        private string rootUrl = "";
        private string cseName = "";
        private string aeName = "";
        private string aeID = "";

        private string protocol = "HTTP";
        private string ip = "127.0.0.1";

        public int maxCinCount = 5;

        //private int httpPort = 11000;

        private int nHttpConnectTimeout = 10000;

        private IProgressChanged handler = null;
        private IInitResourceManagerHanler initHandler = null;
        public string Status = "stopped";

        public ResourceManager(string rootUrl, string rootResourceName, string aeName)
        {
            this.rootUrl = rootUrl;
            this.cseName = rootResourceName;
            this.aeName = aeName;

            this.strReqAeUrl = this.rootUrl + @"/" + this.cseName + "?fu=1&rty=2";
            this.strReqConUrl = this.rootUrl + @"/" + this.cseName + "?fu=1&rty=3";
            this.strReqCinUrl = this.rootUrl + @"/" + this.cseName + "?fu=1&rty=4";
            this.strReqSubUrl = this.rootUrl + @"/" + this.cseName + "?fu=1&rty=23";
            this.strReqSemUrl = this.rootUrl + @"/" + this.cseName + "?fu=1&rty=24";
        }

        public void SetProtocolInfo(string protocol, string ip)
        {
            this.protocol = protocol;
            this.ip = ip;
        }

        public void Start()
        {
            Status = "loading";
            mqttServer = new NotifiacitonMqttServer(ip);
            mqttServer.SetReceiverHandler(this);
            mqttServer.Start();

            lstAe.Clear();
            lstCon.Clear();
            lstCin.Clear();
            lstSub.Clear();
            lstSem.Clear();
            lstTasks.Clear();

            Task task = new Task(() =>
            {
                InitAEInfoFromServer();

                DiscoveResource();

                if(this.initHandler!= null)
                {
                    this.initHandler.InitResourceManagerCompeleted();
                    Status = "working";
                }
            });

            task.Start();
        }

        public void Stop()
        {
            Task task = new Task(() =>
           {
               if (this.initHandler != null)
               {
                   this.initHandler.StopResourceManagerBegined();
               }

               Status = "stopBegined";
               if (mqttServer.IsActived) mqttServer.Stop();

               ProgressChanged(0, "Stop process...");
               DeleteSubscriptionForCSEBase();
               DeleteSubscriptionForAE();
               DeleteSubscriptionForContainer();

               ProgressChanged(100, "Task finished...");
               if (this.initHandler != null)
               {
                   this.initHandler.StopResourceManagerCompeleted();
               }
               Status = "stopped";
           });

            task.Start();
        }

        private void InitAEInfoFromServer()
        {
            AEObject ae = new AEObject();
            ae.AEName = this.aeName;
            ae.AppID = RandomString(15);

            if (!IsAeExisted(ae))
            {
                Debug.WriteLine("[" + ae.AEName + "] is not existed!");
                if (CreateAE(ae))
                {
                    Debug.WriteLine("[" + ae.AEName + "] is created!");
                }
            }
            else
            {
                Debug.WriteLine("[" + ae.AEName + "] is existed!");
            }
            Debug.WriteLine("[" + ae.AEName + "]'s AE ID is [" + ae.AEID + "]");
            this.aeID = ae.AEID;

            string topic = @"/oneM2M/req/+/" + ae.AEID + @"/#";
            mqttServer.SubscripTopic(topic);
        }

        private void ProgressChanged(double percent, string msg)
        {
            if (this.handler != null)
            {
                this.handler.ProgressChanged(percent, msg);
            }
        }

        private void DiscoveResource()
        {
            //CreateSubscriptionForCseBase();

            ProgressChanged(0, "Request AE resource...");
            //Thread.Sleep(200);
            GetAEResourceData();
            Console.WriteLine("AE Resource number: " + lstAe.Count);
            //Thread.Sleep(200);
            //CreateSubscriptionForAE();

            ProgressChanged(1, "Request Container resource...");
            //Thread.Sleep(200);
            GetContainerResourceData();
            Debug.WriteLine("Container resource number is " + lstCon.Count);

            string[] keys = lstCon.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                ResourceObject[] cins = GetContentInstanceResourceDataByContainer(lstCon[keys[i]].ResourcePath);

                if (cins != null)
                {
                    for (int j = 0; j < cins.Length; j++)
                    {
                        string path = cins[j].ResourcePath;
                        lstCin.Add(cins[j]);
                    }
                }

                double dValue = (48.0 / lstCon.Count) * i + 2;

                ProgressChanged(dValue, "Request for [" + lstCon[keys[i]].ResourcePath + "]");
            }
            Debug.WriteLine("Content Instance resource number is " + lstCin.Count);

            ProgressChanged(51, "Request Semantic Discription resource...");
            //Thread.Sleep(200);
            GetSementicDescriptionResourceData();
            Debug.WriteLine("Semantic Description resource number is " + lstSem.Count);

            CreateSubscriptionForCseBase();
            CreateSubscriptionForAE(); 
            CreateSubscriptionForConatiner();

            ProgressChanged(99, "Request Subscription resource...");
            //Thread.Sleep(200);
            GetSubscriptionResourceData();
            Debug.WriteLine("Subscription resource number is " + lstSub.Count);

            ProgressChanged(100, "Request finished...");
        }

        private void CreateSubscriptionForCseBase()
        {
            string noti_uri = @"mqtt://" + this.ip + @"/" + this.aeID;
            string sub_name = this.aeName + @"_sub";

            SubscriptionObject sub = new SubscriptionObject();
            sub.ParentPath = @"/" + this.cseName;
            sub.SubscriptionName = sub_name;
            sub.NoitificationUri = noti_uri;

            if (IsSubscriptionExisted(sub))
            {
                UpdateSubscription(sub);
            }
            else
            {
                CreateSubscription(sub);
            }

            ProgressChanged(52, "Create subscription for CSEBase...");
        }

        private void CreateSubscriptionForAE()
        {
            string noti_uri = @"mqtt://" + this.ip + @"/" + this.aeID;
            string sub_name = this.aeName + @"_sub";

            string[] keys = lstAe.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                SubscriptionObject sub = new SubscriptionObject();
                sub.ParentPath = lstAe[keys[i]].ResourcePath;
                sub.SubscriptionName = sub_name;
                sub.NoitificationUri = noti_uri;

                if (IsSubscriptionExisted(sub))
                {
                    UpdateSubscription(sub);
                }
                else
                {
                    CreateSubscription(sub);
                }

                double dValue = (10.0 / lstAe.Count) * i + 52;

                ProgressChanged(dValue, "Create subscription for AE[" + lstAe[keys[i]].ResourceName + "]");
            }
        }

        private void CreateSubscriptionForConatiner()
        {
            string noti_uri = @"mqtt://" + this.ip + @"/" + this.aeID;
            string sub_name = this.aeName + @"_sub";

            string[] keys = lstCon.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                SubscriptionObject sub = new SubscriptionObject();
                sub.ParentPath = lstCon[keys[i]].ResourcePath;
                sub.SubscriptionName = sub_name;
                sub.NoitificationUri = noti_uri;

                if (IsSubscriptionExisted(sub))
                {
                    UpdateSubscription(sub);
                }
                else
                {
                    CreateSubscription(sub);
                }

                double dValue = (37.0 / lstCon.Count) * i + 62;

                ProgressChanged(dValue, "Create subscription for Container[" + lstCon[keys[i]].ResourceName + "]");
            }
        }

        private void DeleteSubscriptionForCSEBase()
        {
            string sub_name = this.aeName + @"_sub";

            SubscriptionObject sub = new SubscriptionObject();
            sub.ParentPath = @"/" + this.cseName;
            sub.SubscriptionName = sub_name;

            DeleteSubscription(sub);

            ProgressChanged(10, "Delete subscription " + sub_name + " from CSEBase...");
        }

        private void DeleteSubscriptionForAE()
        {
            string sub_name = this.aeName + @"_sub";

            string[] keys = lstAe.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                SubscriptionObject sub = new SubscriptionObject();
                sub.ParentPath = lstAe[keys[i]].ResourcePath;
                sub.SubscriptionName = sub_name;

                DeleteSubscription(sub);

                double dValue = (20.0 / lstAe.Count) * i + 10;

                ProgressChanged(dValue, "Delete subscription " + sub_name + " from [" + lstAe[keys[i]].ResourcePath + "]...");

                Thread.Sleep(50);
            }
        }

        private void DeleteSubscriptionForContainer()
        {
            string sub_name = this.aeName + @"_sub";

            string[] keys = lstCon.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                SubscriptionObject sub = new SubscriptionObject();
                sub.ParentPath = lstCon[keys[i]].ResourcePath;
                sub.SubscriptionName = sub_name;

                DeleteSubscription(sub);

                double dValue = (70.0 / lstCon.Count) * i + 30;

                ProgressChanged(dValue, "Delete subscription " + sub_name + " from [" + lstCon[keys[i]].ResourcePath + "]...");
            }
        }

        public SubscriptionObject GetSubscriptionObject(string r_path) {
            string noti_uri = @"mqtt://" + this.ip + @"/" + this.aeID;
            string sub_name = this.aeName + @"_sub";

            SubscriptionObject sub = new SubscriptionObject();
            sub.ParentPath = r_path;
            sub.SubscriptionName = sub_name;
            sub.NoitificationUri = noti_uri;

            return sub;
        }

        public void SetProgressChangedHandler(IProgressChanged handler)
        {
            this.handler = handler;
        }

        public void SetInitHandler(IInitResourceManagerHanler handler)
        {
            this.initHandler = handler;
        }

        private void GetAEResourceData()
        {
            Debug.WriteLine("Request URL: [GET] " + strReqAeUrl);
            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strReqAeUrl);
                req.Proxy = null;
                req.Method = "GET";
                req.Accept = "application/xml";
                req.Headers.Add("X-M2M-RI", "12345");
                req.Headers.Add("X-M2M-Origin", "Origin");
                req.Headers.Add("nmtype", "short");
                req.Timeout = nHttpConnectTimeout;

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader sr = new StreamReader(resp.GetResponseStream());

                    string content = sr.ReadToEnd();
                    sr.Close();

                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(content);

                    XmlNamespaceManager nmspc = new XmlNamespaceManager(doc.NameTable);
                    nmspc.AddNamespace("m2m", "http://www.onem2m.org/xml/protocols");
                    nmspc.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");

                    string data = doc.SelectSingleNode("m2m:uril", nmspc).InnerText.ToLower();

                    string[] aryPath = data.Split(' ');
                    int count = 0;

                    for (int i = 0; i < aryPath.Length; i++)
                    {
                        if (aryPath[i].Trim().Length > 0)
                        {
                            string[] strAry = aryPath[i].Split('/');

                            ResourceObject resc = new ResourceObject();
                            resc.ResourceType = "AE";
                            resc.ResourcePath = aryPath[i];
                            int startIndex = aryPath[i].LastIndexOf('/') + 1;
                            int length = aryPath[i].Length - startIndex;
                            resc.ResourceName = aryPath[i].Substring(startIndex, length);
                            resc.ParentPath = aryPath[i].Substring(0, aryPath[i].Length - length - 1);
                            count++;
                            if (!lstAe.ContainsKey(resc.ResourcePath))
                            {
                                lstAe.Add(resc.ResourcePath, resc);
                            }
                        }
                    }
                    //Console.WriteLine("AE Resource number: " + count);
                }
                resp.Close();
            }
            catch (WebException)
            {
                Debug.WriteLine("AE resource request timeout");
            }
        }

        private void GetContainerResourceData()
        {
            Debug.WriteLine("Request URL: [GET] " + strReqConUrl);
            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strReqConUrl);
                req.Proxy = null;
                req.Method = "GET";
                req.Accept = "application/xml";
                req.Headers.Add("X-M2M-RI", "12345");
                req.Headers.Add("X-M2M-Origin", "Origin");
                req.Timeout = nHttpConnectTimeout;

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader sr = new StreamReader(resp.GetResponseStream());

                    string content = sr.ReadToEnd();

                    sr.Close();

                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(content);

                    XmlNamespaceManager nmspc = new XmlNamespaceManager(doc.NameTable);
                    nmspc.AddNamespace("m2m", "http://www.onem2m.org/xml/protocols");
                    nmspc.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");

                    string data = doc.SelectSingleNode("m2m:uril", nmspc).InnerText.ToLower();

                    string[] aryPath = data.Split(' ');
                    int count = 0;

                    for (int i = 0; i < aryPath.Length; i++)
                    {
                        if (aryPath[i].Trim().Length > 0)
                        {
                            string[] strAry = aryPath[i].Split('/');

                            ResourceObject resc = new ResourceObject();
                            resc.ResourceType = "Container";
                            resc.ResourcePath = aryPath[i];
                            int startIndex = aryPath[i].LastIndexOf('/') + 1;
                            int length = aryPath[i].Length - startIndex;
                            resc.ResourceName = aryPath[i].Substring(startIndex, length);
                            resc.ParentPath = aryPath[i].Substring(0, aryPath[i].Length - length - 1);
                            count++;
                            if (!lstCon.ContainsKey(resc.ResourcePath))
                            {
                                lstCon.Add(resc.ResourcePath, resc);
                            }
                        }
                    }

                    //Debug.WriteLine("Container resource number is " + count);
                }
                resp.Close();
            }
            catch (WebException)
            {
                Debug.WriteLine("Container resource request timeout");
            }
        }

        private void GetSubscriptionResourceData()
        {
            Debug.WriteLine("Request URL: [GET] " + strReqSubUrl);
            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strReqSubUrl);
                req.Proxy = null;
                req.Method = "GET";
                req.Accept = "application/xml";
                req.Headers.Add("X-M2M-RI", "12345");
                req.Headers.Add("X-M2M-Origin", "Origin");
                req.Timeout = nHttpConnectTimeout;

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader sr = new StreamReader(resp.GetResponseStream());

                    string content = sr.ReadToEnd();
                    sr.Close();

                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(content);

                    XmlNamespaceManager nmspc = new XmlNamespaceManager(doc.NameTable);
                    nmspc.AddNamespace("m2m", "http://www.onem2m.org/xml/protocols");
                    nmspc.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");

                    string data = doc.SelectSingleNode("m2m:uril", nmspc).InnerText.ToLower();

                    string[] aryPath = data.Split(' ');

                    int count = 0;

                    for (int i = 0; i < aryPath.Length; i++)
                    {
                        if (aryPath[i].Trim().Length > 0)
                        {
                            string[] strAry = aryPath[i].Split('/');

                            ResourceObject resc = new ResourceObject();
                            resc.ResourceType = "Subscription";
                            resc.ResourcePath = aryPath[i];
                            int startIndex = aryPath[i].LastIndexOf('/') + 1;
                            int length = aryPath[i].Length - startIndex;
                            resc.ResourceName = aryPath[i].Substring(startIndex, length);
                            resc.ParentPath = aryPath[i].Substring(0, aryPath[i].Length - length - 1);
                            count++;
                            if (!lstSub.ContainsKey(resc.ResourcePath))
                            {
                                lstSub.Add(resc.ResourcePath, resc);
                            }
                        }
                    }

                    //Debug.WriteLine("Subscription resource number is " + count);
                }
                resp.Close();
            }
            catch (WebException)
            {
                Debug.WriteLine("Subscription resource request timeout");
            }
        }

        private void GetSementicDescriptionResourceData()
        {
            Debug.WriteLine("Request URL: [GET] " + strReqSemUrl);
            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strReqSemUrl);
                req.Proxy = null;
                req.Method = "GET";
                req.Accept = "application/xml";
                req.Headers.Add("X-M2M-RI", "12345");
                req.Headers.Add("X-M2M-Origin", "Origin");
                req.Timeout = nHttpConnectTimeout;

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader sr = new StreamReader(resp.GetResponseStream());

                    string content = sr.ReadToEnd();
                    sr.Close();

                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(content);

                    XmlNamespaceManager nmspc = new XmlNamespaceManager(doc.NameTable);
                    nmspc.AddNamespace("m2m", "http://www.onem2m.org/xml/protocols");
                    nmspc.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");

                    string data = doc.SelectSingleNode("m2m:uril", nmspc).InnerText.ToLower();

                    string[] aryPath = data.Split(' ');

                    int count = 0;

                    for (int i = 0; i < aryPath.Length; i++)
                    {
                        if (aryPath[i].Trim().Length > 0)
                        {
                            string[] strAry = aryPath[i].Split('/');

                            ResourceObject resc = new ResourceObject();
                            resc.ResourceType = "SemanticDescription";
                            resc.ResourcePath = aryPath[i];
                            int startIndex = aryPath[i].LastIndexOf('/') + 1;
                            int length = aryPath[i].Length - startIndex;
                            resc.ResourceName = aryPath[i].Substring(startIndex, length);
                            resc.ParentPath = aryPath[i].Substring(0, aryPath[i].Length - length - 1);
                            count++;
                            if (!lstSem.ContainsKey(resc.ResourcePath))
                            {
                                lstSem.Add(resc.ResourcePath, resc);
                            }
                        }
                    }

                    //Debug.WriteLine("Semantic Description resource number is " + count);
                }
                resp.Close();
            }
            catch (WebException)
            {
                Debug.WriteLine("Semantic Description resource request timeout");
            }
        }

        private ResourceObject[] GetContentInstanceResourceDataByContainer(string containerPath)
        {
            ResourceObject[] resources = null; ;

            string strUrl = this.rootUrl + containerPath + @"?fu=1&rty=4&lim=5";

            Debug.WriteLine("Request URL: [GET] " + strUrl);

            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);
                req.Proxy = null;
                req.Method = "GET";
                req.Accept = "application/xml";
                req.Headers.Add("X-M2M-RI", "12345");
                req.Headers.Add("X-M2M-Origin", "Origin");
                req.Headers.Add("nmtype", "short");

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                List<ResourceObject> lstResources = new List<ResourceObject>();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader sr = new StreamReader(resp.GetResponseStream());

                    string content = sr.ReadToEnd();

                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(content);

                    XmlNamespaceManager nmspc = new XmlNamespaceManager(doc.NameTable);
                    nmspc.AddNamespace("m2m", "http://www.onem2m.org/xml/protocols");
                    nmspc.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");

                    XmlElement eleUrls = doc.SelectSingleNode("m2m:uril", nmspc) as XmlElement;

                    if (eleUrls != null)
                    {
                        string data = eleUrls.InnerText.ToLower();

                        string[] aryPath = data.Split(' ');

                        for (int i = 0; i < aryPath.Length; i++)
                        {
                            if (aryPath[i].Trim().Length > 0)
                            {
                                //Debug.WriteLine(aryPath[i]);

                                string[] strAry = aryPath[i].Split('/');

                                ResourceObject resc = new ResourceObject();
                                resc.ResourceType = "ContentInstance";
                                resc.ResourcePath = aryPath[i];
                                int startIndex = aryPath[i].LastIndexOf('/') + 1;
                                int length = aryPath[i].Length - startIndex;
                                resc.ResourceName = aryPath[i].Substring(startIndex, length);
                                resc.ParentPath = aryPath[i].Substring(0, aryPath[i].Length - length - 1);

                                lstResources.Add(resc);
                            }
                        }
                    }

                    resources = lstResources.ToArray();
                }

                resp.Close();
            }
            catch (WebException exp)
            {
                Debug.WriteLine(exp.Message);
                Debug.WriteLine(containerPath + " request timeout");
            }

            return resources;
        }

        private bool IsAeExisted(AEObject ae)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(@"/").Append(this.cseName).Append(@"/").Append(ae.AEName);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [GET] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "GET";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=2";
                req.Accept = "application/xml";
                req.Headers.Add("X-M2M-RI", "12345");
                req.Headers.Add("X-M2M-Origin", "Origin");
                req.Headers.Add("nmtype", "short");

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    {
                        string content = sr.ReadToEnd();
                        sr.Close();

                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(content);

                        XmlNamespaceManager nmspc = new XmlNamespaceManager(doc.NameTable);
                        nmspc.AddNamespace("m2m", "http://www.onem2m.org/xml/protocols");
                        nmspc.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");

                        ae.AEID = doc.SelectSingleNode("m2m:ae/aei", nmspc).InnerText;

                        bResult = true;
                    }
                }
                resp.Close();
            }
            catch
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
                sb.Append(this.rootUrl).Append(@"/").Append(this.cseName);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [POST] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "POST";
                req.Accept = "application/xml";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=2";
                req.Headers.Add("X-M2M-RI", "12345");
                req.Headers.Add("X-M2M-Origin", "Origin");
                req.Headers.Add("X-M2M-NM", ae.AEName);

                string req_content = ae.MakeBodyXML();

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

                            XmlDocument doc = new XmlDocument();
                            doc.LoadXml(resp_content);

                            XmlNamespaceManager nmspc = new XmlNamespaceManager(doc.NameTable);
                            nmspc.AddNamespace("m2m", "http://www.onem2m.org/xml/protocols");
                            nmspc.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");

                            ae.AEID = doc.SelectSingleNode("m2m:ae/aei", nmspc).InnerText;

                            bResult = true;
                        }
                    }
                }
                resp.Close();
            }
            catch
            {

            }

            return bResult;
        }

        private bool IsSubscriptionExisted(SubscriptionObject sub)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(sub.ParentPath).Append(@"/").Append(sub.SubscriptionName);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [GET] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "GET";
                req.ContentType = "application/vnd.onem2m-res+xml";
                req.Accept = "application/xml";
                req.Headers.Add("X-M2M-RI", "12345");
                req.Headers.Add("X-M2M-Origin", "Origin");
                req.Headers.Add("nmtype", "short");

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    bResult = true;
                }
                resp.Close();
            }
            catch
            {
                bResult = false;
            }
            return bResult;
        }

        private bool CreateSubscription(SubscriptionObject sub)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(sub.ParentPath);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [POST] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "POST";
                req.Accept = "application/xml";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=23";
                req.Headers.Add("X-M2M-RI", "12345");
                req.Headers.Add("X-M2M-Origin", "Origin");
                req.Headers.Add("X-M2M-NM", sub.SubscriptionName);

                string req_content = sub.MakeBodyXML();

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
                         }
                     }
                 }
                 resp.Close();
            }
            catch
            {

            }

            return bResult;
        }

        private bool UpdateSubscription(SubscriptionObject sub)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(sub.ParentPath).Append(@"/").Append(sub.SubscriptionName);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [PUT] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "PUT";
                req.Accept = "application/xml";
                req.ContentType = "application/vnd.onem2m-res+xml";
                req.Headers.Add("X-M2M-RI", "12345");
                req.Headers.Add("X-M2M-Origin", "Origin");
                req.Headers.Add("nmtype", "short");
                req.Headers.Add("X-M2M-NM", sub.SubscriptionName);

                string req_content = sub.MakeBodyXML();

                using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(req_content);
                    sw.Flush();
                    sw.Close();
                }

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    bResult = true;

                    if (resp.StatusCode == HttpStatusCode.Created)
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            string resp_content = sr.ReadToEnd();
                            sr.Close();
                        }
                    }
                }

                resp.Close();
            }
            catch(Exception exp)
            {
                Debug.WriteLine(exp.Message);
            }

            return bResult;
        }

        private bool DeleteSubscription(SubscriptionObject sub)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(sub.ParentPath).Append(@"/").Append(sub.SubscriptionName);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [DELETE] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "DELETE";
                req.Accept = "application/onem2m-resource+xml";
                req.Headers.Add("X-M2M-RI", "12345");
                req.Headers.Add("X-M2M-Origin", "Origin");

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

        public ResourceObject[] GetAllResource()
        {
            List<ResourceObject> lstTemp = new List<ResourceObject>();

            try
            {
                var CSEBase = new ResourceObject()
                {
                    ParentPath = "/",
                    ResourceName = this.cseName,
                    ResourcePath = "/" + this.cseName,
                    ResourceType = "CSEBase"
                };

                lstTemp.Add(CSEBase);

                foreach (string path in lstAe.Keys)
                {
                    lstTemp.Add(lstAe[path]);
                }
                foreach (string path in lstCon.Keys)
                {
                    lstTemp.Add(lstCon[path]);
                }
                for (int i = 0; i < lstCin.Count; i++)
                {
                    lstTemp.Add(lstCin[i]);
                }
                foreach (string path in lstSub.Keys)
                {
                    lstTemp.Add(lstSub[path]);
                }
                foreach (string path in lstSem.Keys)
                {
                    lstTemp.Add(lstSem[path]);
                }
            }catch {

            }

            return lstTemp.ToArray();
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

        private string GetResoureceTypeString(string type_code)
        {
            switch (type_code)
            {
                case "2": return "AE";
                case "3": return "Container";
                case "4": return "ContentInstance";
                case "23": return "Subscription";
                case "24": return "SemanticDescription";
                default: return "Unknown";
            }
        }

        public void ReceiveNotificationMessage(string r_name, string p_path, string r_type, string evt_type)
        {
            if (Status == "working")
            {
                var resourceName = r_name;
                var parentPath = p_path;
                var resourcePath = p_path + @"/" + r_name;
                var resourceType = GetResoureceTypeString(r_type);

                if (evt_type == "3")
                {
                    Debug.WriteLine("Receive a creation notification form [" + r_type + "] resourece named [" + r_name + "]");

                    ResourceObject newResc = new ResourceObject() 
                    { 
                        ParentPath = parentPath, 
                        ResourceName = resourceName, 
                        ResourcePath = resourcePath, 
                        ResourceType = resourceType 
                    };

                    if (r_type == "2")
                    {
                        if (!lstAe.ContainsKey(resourcePath))
                        {
                            lstAe.Add(resourcePath, newResc);

                            SubscriptionObject sub = GetSubscriptionObject(resourcePath);
                            if (CreateSubscription(sub))
                            {
                                ResourceObject subResourece = new ResourceObject()
                                {
                                    ParentPath = resourcePath,
                                    ResourceName = sub.SubscriptionName,
                                    ResourcePath = resourcePath + @"/" + sub.SubscriptionName,
                                    ResourceType = "Subscription"
                                };

                                if (!lstSub.ContainsKey(subResourece.ResourcePath))
                                {
                                    lstSub.Add(subResourece.ResourcePath, subResourece);
                                }
                            }
                        }
                    }
                    else if (r_type == "3")
                    {
                        if (!lstCon.ContainsKey(resourcePath))
                        {
                            lstCon.Add(resourcePath, newResc);

                            SubscriptionObject sub = GetSubscriptionObject(resourcePath);
                            if (CreateSubscription(sub))
                            {
                                ResourceObject subResourece = new ResourceObject()
                                {
                                    ParentPath = resourcePath,
                                    ResourceName = sub.SubscriptionName,
                                    ResourcePath = resourcePath + @"/" + sub.SubscriptionName,
                                    ResourceType = "Subscription"
                                };

                                if (!lstSub.ContainsKey(subResourece.ResourcePath))
                                {
                                    lstSub.Add(subResourece.ResourcePath, subResourece);
                                }
                            }
                        }
                    }
                    else if (r_type == "4")
                    {
                        List<ResourceObject> temp = new List<ResourceObject>();

                        for (int i = 0; i < lstCin.Count;i++ )
                        {
                            if (lstCin[i].ParentPath == parentPath)
                            {
                                temp.Add(lstCin[i]);
                            }
                        }

                        for (int i = 0; i < temp.Count; i++)
                        {
                            lstCin.Remove(temp[i]);
                        }

                        if (temp.Count >= maxCinCount)
                        {
                            temp.RemoveAt(temp.Count - 1);
                            temp.Insert(0, newResc);
                        }
                        else
                        {
                            temp.Insert(0, newResc);
                        }

                        for (int i = 0; i < temp.Count; i++)
                        {
                            Debug.WriteLine("[" + i + "]: " + temp[i].ResourcePath);
                            lstCin.Add(temp[i]);
                        }
                    }
                    else if (r_type == "23")
                    {
                        if (!lstSub.ContainsKey(newResc.ResourcePath))
                        {
                            lstSub.Add(newResc.ResourcePath, newResc);
                        }
                    }
                    else if (r_type == "24")
                    {
                        if (!lstSem.ContainsKey(newResc.ResourcePath))
                        {
                            lstSem.Add(newResc.ResourcePath, newResc);
                        }
                    }
                }
                else if (evt_type == "4")
                {
                    Debug.WriteLine("Receive a deletion notification form [" + r_type + "] resourece named [" + r_name + "]");

                    if (r_type == "2")
                    {
                        if (lstAe.ContainsKey(resourcePath))
                        {
                            lstAe.Remove(resourcePath);

                            string[] keys = lstCon.Keys.ToArray();

                            for (int i = 0; i < keys.Length; i++)
                            {
                                if (keys[i].StartsWith(resourcePath + @"/"))
                                {
                                    lstCon.Remove(keys[i]);
                                }
                            }

                            for (int i = 0; i < lstCin.Count; i++)
                            {
                                if (lstCin[i].ResourcePath.StartsWith(resourcePath + @"/"))
                                {
                                    lstCin.RemoveAt(i);
                                    i--;
                                }
                            }

                            keys = lstSub.Keys.ToArray();

                            for (int i = 0; i < keys.Length; i++)
                            {
                                if (keys[i].StartsWith(resourcePath + @"/"))
                                {
                                    lstSub.Remove(keys[i]);
                                }
                            }

                            keys = lstSem.Keys.ToArray();

                            for (int i = 0; i < keys.Length; i++)
                            {
                                if (keys[i].StartsWith(resourcePath + @"/"))
                                {
                                    lstSem.Remove(keys[i]);
                                }
                            }
                        }
                    }
                    else if (r_type == "3")
                    {
                        if (lstCon.ContainsKey(resourcePath))
                        {
                            lstCon.Remove(resourcePath);

                            string[] keys = lstAe.Keys.ToArray();

                            for (int i = 0; i < keys.Length; i++)
                            {
                                if (keys[i].StartsWith(resourcePath + @"/"))
                                {
                                    lstAe.Remove(keys[i]);
                                }
                            }

                            for (int i = 0; i < lstCin.Count; i++)
                            {
                                if (lstCin[i].ResourcePath.StartsWith(resourcePath + @"/"))
                                {
                                    lstCin.RemoveAt(i);
                                    i--;
                                }
                            }

                            keys = lstSub.Keys.ToArray();

                            for (int i = 0; i < keys.Length; i++)
                            {
                                if (keys[i].StartsWith(resourcePath + @"/"))
                                {
                                    lstSub.Remove(keys[i]);
                                }
                            }

                            keys = lstSem.Keys.ToArray();

                            for (int i = 0; i < keys.Length; i++)
                            {
                                if (keys[i].StartsWith(resourcePath + @"/"))
                                {
                                    lstSem.Remove(keys[i]);
                                }
                            }
                        }
                    }
                    else if (r_type == "4")
                    {
                        for (int i = 0; i < lstCin.Count; i++)
                        {
                            if (lstCin[i].ResourcePath == resourcePath )
                            {
                                lstCin.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                    else if (r_type == "23")
                    {
                        if (lstSub.ContainsKey(resourcePath))
                        {
                            lstSub.Remove(resourcePath);
                        }
                    }
                    else if (r_type == "24")
                    {
                        if (lstSem.ContainsKey(resourcePath))
                        {
                            lstSem.Remove(resourcePath);
                        }
                    }
                }
            }
        }
    }

    public interface IInitResourceManagerHanler
    {
        void InitResourceManagerCompeleted();
        void StopResourceManagerBegined();
        void StopResourceManagerCompeleted();
    }

    public class AEObject
    {
        public string AEName { get; set; }
        public string AppID { get; set; }
        public string AEID { get; set; }

        public string MakeBodyXML()
        {
            string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
            xml += "<m2m:ae ";
            xml += "xmlns:m2m=\"http://www.onem2m.org/xml/protocols\" ";
            xml += "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">";
            xml += "<api>" + AppID + "</api>";
            //xml += "<aei>" + "SO." + appID + "</aei>";
            xml += "</m2m:ae>";

            return xml;
        }
    }

    public class SubscriptionObject
    {
        public string SubscriptionName { get; set; }
        public string NoitificationUri { get; set; }
        public string ParentPath { get; set; }

        public string MakeBodyXML()
        {
            string strXML = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
            strXML += "<m2m:sub ";
            strXML += "xmlns:m2m=\"http://www.onem2m.org/xml/protocols\" ";
            strXML += "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">";
            strXML += "<enc>";
            strXML += "<net>3 4</net>";
            strXML += "</enc>";
            strXML += "<nu>" + NoitificationUri + "</nu>";
            strXML += "<pn>1</pn>";
            strXML += "<nct>2</nct>";
            strXML += "</m2m:sub>";

            return strXML;
        }
    }

    public interface IProgressChanged
    {
        void ProgressChanged(double percent, string messgage);
    }
}
