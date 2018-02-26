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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MobiusResourceMonitor_sub
{
    public interface IGetResourceInfoCallback
    {
        void GetResourceInfoFinish(string msg);
    }

    public class ResourceManager : INotificaitonReceiver
    {
        private const int MaxContentInstanceCount = 25;

        private NotifiacitonMqttServer mqttServer;

        private ResourceObject cseBase;

        private Dictionary<string, ResourceObject> lstAe = new Dictionary<string, ResourceObject>();
        private Dictionary<string, ResourceObject> lstCon = new Dictionary<string, ResourceObject>();
        private Dictionary<string, ResourceObject> lstSub = new Dictionary<string, ResourceObject>();
        private Dictionary<string, ResourceObject> lstSem = new Dictionary<string, ResourceObject>();
        private Dictionary<string, ResourceObject> lstTs = new Dictionary<string, ResourceObject>();
        private Dictionary<string, ResourceObject> lstGr = new Dictionary<string, ResourceObject>();
        private Dictionary<string, ResourceObject> lstBase = new Dictionary<string, ResourceObject>();

        private List<ResourceObject> lstCin = new List<ResourceObject>();
        private List<ResourceObject> lstTsCin = new List<ResourceObject>();

        private string strReqAeUrl = "";
        private string strReqConUrl = "";
        private string strReqSubUrl = "";
        private string strReqSemUrl = "";
        private string strReqTsUrl = "";
        private string strReqGrUrl = "";

        private string rootUrl = "";
        private string cseName = "";
        public string aeName = "";
        private string aeID = "";
        private string origin = "";

        private string resourcePath = "";

        private string protocol = "HTTP";
        private string ip = "127.0.0.1";

        public int maxCinCount = 5;

        private int nHttpConnectTimeout = 25000;

        private IProgressChanged handler = null;
        private IInitResourceManagerHanler initHandler = null;

        public string Status = "stopped";

        public ResourceManager(string rescPath, string aeName, string aeID, string origin)
        {
            this.resourcePath = rescPath;

            Uri mUri = new Uri(rescPath);

            this.rootUrl = rescPath.Replace(mUri.PathAndQuery, "");
            this.cseName = mUri.AbsolutePath.Split('/')[1];
            this.aeName = aeName;
            this.aeID = aeID;
            this.origin = origin;

            this.strReqAeUrl = rescPath + "?fu=1&ty=2";
            this.strReqConUrl = rescPath + "?fu=1&ty=3";
            this.strReqSubUrl = rescPath + "?fu=1&ty=23";
            this.strReqSemUrl = rescPath + "?fu=1&ty=24";
            this.strReqTsUrl = rescPath + "?fu=1&ty=29";
            this.strReqGrUrl = rescPath + "?fu=1&ty=9";
        }

        public void GetBaseResources()
        {
            Uri mUri = new Uri(this.resourcePath);

            string path = mUri.AbsolutePath;

            string[] pathAry = path.Split('/');

            if (pathAry.Length >= 2)
            {
                string parent = "";

                for (int i = 1; i < pathAry.Length; i++)
                {
                    string rescPath = parent + "/" + pathAry[i];
                    string httpHost = this.resourcePath.Replace(mUri.PathAndQuery, "");
                    string strReqUri = httpHost + rescPath;

                    Debug.WriteLine("Request URL: [GET] " + strReqUri);
                    try
                    {
                        HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strReqUri);
                        req.Proxy = null;
                        req.Method = "GET";
                        req.Accept = "application/xml";
                        req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                        //req.Headers.Add("X-M2M-Origin", "C" + this.aeName);
                        req.Headers.Add("X-M2M-Origin", this.origin);
                        req.Timeout = nHttpConnectTimeout;

                        HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                        if (resp.StatusCode == HttpStatusCode.OK)
                        {
                            StreamReader sr = new StreamReader(resp.GetResponseStream());

                            string content = sr.ReadToEnd();
                            sr.Close();

                            int index_s = -1;
                            int index_e = -1;

                            index_s = content.IndexOf("<ty>");
                            index_e = content.IndexOf("</ty>");

                            if (index_s > 0 && index_e > 0)
                            {
                                string strType = content.Substring(index_s + 4, index_e - index_s - 4);

                                ResourceObject resc = new ResourceObject();
                                resc.ResourcePath = rescPath;
                                resc.ResourceName = pathAry[i];
                                resc.ParentPath = parent;
                                resc.ResourceType = GetResoureceTypeString(strType);

                                if (strType == "2")
                                {
                                    if (!lstAe.ContainsKey(resc.ResourcePath))
                                    {
                                        lstAe.Add(resc.ResourcePath, resc);
                                    }

                                    if (i != pathAry.Length - 1)
                                    {
                                        if (!lstBase.ContainsKey(resc.ResourcePath))
                                        {
                                            lstBase.Add(resc.ResourcePath, resc);
                                        }
                                    }
                                }
                                else if (strType == "3")
                                {
                                    if (i != pathAry.Length - 1)
                                    {
                                        if (!lstCon.ContainsKey(resc.ResourcePath))
                                        {
                                            lstCon.Add(resc.ResourcePath, resc);
                                        }

                                        if (!lstBase.ContainsKey(resc.ResourcePath))
                                        {
                                            lstBase.Add(resc.ResourcePath, resc);
                                        }
                                    }
                                }
                                else if (strType == "4")
                                {
                                    if (i != pathAry.Length - 1)
                                    {
                                        lstCin.Add(resc);

                                        if (!lstBase.ContainsKey(resc.ResourcePath))
                                        {
                                            lstBase.Add(resc.ResourcePath, resc);
                                        }
                                    }
                                }
                                else if (strType == "5")
                                {
                                    if (i != pathAry.Length - 1)
                                    {
                                        this.cseBase = resc;

                                        if (!lstBase.ContainsKey(resc.ResourcePath))
                                        {
                                            lstBase.Add(resc.ResourcePath, resc);
                                        }
                                    }
                                }
                                else if (strType == "23")
                                {
                                    if (i != pathAry.Length - 1)
                                    {
                                        if (!lstSub.ContainsKey(resc.ResourcePath))
                                        {
                                            lstSub.Add(resc.ResourcePath, resc);
                                        }

                                        if (!lstBase.ContainsKey(resc.ResourcePath))
                                        {
                                            lstBase.Add(resc.ResourcePath, resc);
                                        }
                                    }
                                }
                                else if (strType == "24")
                                {
                                    if (i != pathAry.Length - 1)
                                    {
                                        if (!lstSem.ContainsKey(resc.ResourcePath))
                                        {
                                            lstSem.Add(resc.ResourcePath, resc);
                                        }

                                        if (!lstBase.ContainsKey(resc.ResourcePath))
                                        {
                                            lstBase.Add(resc.ResourcePath, resc);
                                        }
                                    }
                                }
                                else if (strType == "25")
                                {
                                    if (i != pathAry.Length - 1)
                                    {
                                        if (!lstTs.ContainsKey(resc.ResourcePath))
                                        {
                                            lstTs.Add(resc.ResourcePath, resc);
                                        }

                                        if (!lstBase.ContainsKey(resc.ResourcePath))
                                        {
                                            lstBase.Add(resc.ResourcePath, resc);
                                        }
                                    }
                                }
                                else if (strType == "26")
                                {
                                    if (i != pathAry.Length - 1)
                                    {
                                        lstTsCin.Add(resc);

                                        if (!lstBase.ContainsKey(resc.ResourcePath))
                                        {
                                            lstBase.Add(resc.ResourcePath, resc);
                                        }
                                    }
                                }
                            }
                        }
                        resp.Close();
                    }
                    catch (WebException)
                    {
                        Debug.WriteLine("AE resource request timeout");
                    }

                    parent = rescPath;
                }
            }
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
            lstTs.Clear();
            lstTsCin.Clear();

            Task task = new Task(() =>
            {

                InitAEInfoFromServer();

                GetBaseResources();

                DiscoveResource();

                if (this.initHandler != null)
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
                if (mqttServer.IsActived)
                {
                    string topic = @"/oneM2M/req/+/" + this.aeID + @"/#";
                    mqttServer.UnSubscripTopic(topic);
                    mqttServer.Stop();
                }

                ProgressChanged(0, "Stop process...");
                //DeleteSubscriptionForCSEBase();
                DeleteSubscriptionForAE();
                DeleteSubscriptionForContainer();
                DeleteSubscriptionForTimeSeries();

                //DeleteAppAE();
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
            string topic = @"/oneM2M/req/+/" + this.aeID + @"/#";
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

            GetAEResourceData();
            Console.WriteLine("AE Resource number: " + lstAe.Count);
            GetAEIDs();

            ProgressChanged(1, "Request Container resource...");

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
                        //string path = cins[j].ResourcePath;
                        lstCin.Add(cins[j]);
                    }
                }

                double dValue = (24.0 / lstCon.Count) * i + 2;

                ProgressChanged(dValue, "Request for [" + lstCon[keys[i]].ResourcePath + "]");
            }
            Debug.WriteLine("Content Instance resource number is " + lstCin.Count);

            GetTimeSeriesResourceData();

            keys = lstTs.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                ResourceObject[] tscins = GetTimeSeriesContentInstanceResoureceDataByTimeSeries(lstTs[keys[i]].ResourcePath);

                if (tscins != null)
                {
                    for (int j = 0; j < tscins.Length; j++)
                    {
                        //string path = tscins[j].ResourcePath;
                        lstTsCin.Add(tscins[j]);
                    }
                }

                double dValue = (23.0 / lstCon.Count) * i + 26;

                ProgressChanged(dValue, "Request for [" + lstTs[keys[i]].ResourcePath + "]");
            }

            ProgressChanged(50, "Request Group resource...");
            GetGroupResourceData();

            ProgressChanged(51, "Request Semantic Discription resource...");
            //Thread.Sleep(200);
            GetSementicDescriptionResourceData();
            Debug.WriteLine("Semantic Description resource number is " + lstSem.Count);

            //CreateSubscriptionForCseBase();
            CreateSubscriptionForAE();
            CreateSubscriptionForConatiner();
            CreateSubscriptionForTimSeries();

            ProgressChanged(99, "Request Subscription resource...");
            //Thread.Sleep(200);
            GetSubscriptionResourceData();
            Debug.WriteLine("Subscription resource number is " + lstSub.Count);

            ProgressChanged(100, "Request finished...");
        }

        private void CreateSubscriptionForAE()
        {
            string noti_uri = @"mqtt://" + this.ip + @"/" + this.aeID + @"?ct=xml";
            string sub_name = this.aeName + @"_sub";

            string[] keys = lstAe.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                if (lstBase.ContainsKey(keys[i]))
                {
                    continue;
                }

                var p_path = lstAe[keys[i]].ResourcePath;
                var r_path = p_path + @"/" + sub_name;

                SubscriptionObject sub = new SubscriptionObject();
                sub.RN = sub_name;
                sub.NU = new string[] { noti_uri };
                sub.NET = new string[] { "3", "4" };

                if (IsSubscriptionExisted(r_path))
                {
                    UpdateSubscription(r_path, sub);
                }
                else
                {
                    CreateSubscription(p_path, sub);
                }

                double dValue = (10.0 / lstAe.Count) * i + 52;

                ProgressChanged(dValue, "Create subscription for AE[" + lstAe[keys[i]].ResourceName + "]");
            }
        }

        private void CreateSubscriptionForConatiner()
        {
            string noti_uri = @"mqtt://" + this.ip + @"/" + this.aeID + @"?ct=xml";
            string sub_name = this.aeName + @"_sub";

            string[] keys = lstCon.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                if (lstBase.ContainsKey(keys[i]))
                {
                    continue;
                }

                var p_path = lstCon[keys[i]].ResourcePath;
                var r_path = p_path + @"/" + sub_name;

                SubscriptionObject sub = new SubscriptionObject();
                sub.RN = sub_name;
                sub.NU = new string[] { noti_uri };
                sub.NET = new string[] { "3", "4" };

                if (IsSubscriptionExisted(r_path))
                {
                    UpdateSubscription(r_path, sub);
                }
                else
                {
                    CreateSubscription(p_path, sub);
                }

                double dValue = (20.0 / lstCon.Count) * i + 62;

                ProgressChanged(dValue, "Create subscription for Container[" + lstCon[keys[i]].ResourceName + "]");
            }
        }

        private void CreateSubscriptionForTimSeries()
        {
            string noti_uri = @"mqtt://" + this.ip + @"/" + this.aeID + @"?ct=xml";
            string sub_name = this.aeName + @"_sub";

            string[] keys = lstTs.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                if (lstBase.ContainsKey(keys[i]))
                {
                    continue;
                }

                var p_path = lstTs[keys[i]].ResourcePath;
                var r_path = p_path + @"/" + sub_name;

                SubscriptionObject sub = new SubscriptionObject();
                sub.RN = sub_name;
                sub.NU = new string[] { noti_uri };
                sub.NET = new string[] { "3", "4" };

                if (IsSubscriptionExisted(r_path))
                {
                    UpdateSubscription(r_path, sub);
                }
                else
                {
                    CreateSubscription(p_path, sub);
                }

                double dValue = (17.0 / lstCon.Count) * i + 82;

                ProgressChanged(dValue, "Create subscription for TimeSeries[" + lstTs[keys[i]].ResourceName + "]");
            }
        }

        private void DeleteSubscriptionForCSEBase()
        {
            string sub_name = this.aeName + @"_sub";

            string path = @"/" + this.cseName + @"/" + sub_name;

            string acp = lstSub[path].AccessControlPolicy;

            DeleteResource(path, this.aeID);

            ProgressChanged(10, "Delete subscription " + sub_name + " from CSEBase...");
        }

        private void DeleteSubscriptionForAE()
        {
            string sub_name = this.aeName + @"_sub";

            string[] keys = lstAe.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                string path = lstAe[keys[i]].ResourcePath + @"/" + sub_name;

                string acp = lstAe[keys[i]].AccessControlPolicy;

                DeleteResource(path, this.aeID);

                double dValue = (20.0 / lstAe.Count) * i + 10;

                ProgressChanged(dValue, "Delete subscription " + sub_name + " from [" + lstAe[keys[i]].ResourcePath + "]...");

                Thread.Sleep(10);
            }
        }

        private void DeleteSubscriptionForContainer()
        {
            string sub_name = this.aeName + @"_sub";

            string[] keys = lstCon.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                string path = lstCon[keys[i]].ResourcePath + @"/" + sub_name;

                string acp = lstCon[keys[i]].AccessControlPolicy;

                DeleteResource(path, this.aeID);

                double dValue = (35.0 / lstCon.Count) * i + 30;

                ProgressChanged(dValue, "Delete subscription " + sub_name + " from [" + lstCon[keys[i]].ResourcePath + "]...");
            }
        }

        private void DeleteSubscriptionForTimeSeries()
        {
            string sub_name = this.aeName + @"_sub";

            string[] keys = lstTs.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                string path = lstTs[keys[i]].ResourcePath + @"/" + sub_name;
                string acp = lstTs[keys[i]].AccessControlPolicy;

                DeleteResource(path, this.aeID);

                double dValue = (35.0 / lstCon.Count) * i + 65;

                ProgressChanged(dValue, "Delete subscription " + sub_name + " from [" + lstTs[keys[i]].ResourcePath + "]...");
            }
        }

        public void SetProgressChangedHandler(IProgressChanged handler)
        {
            this.handler = handler;
        }

        public void SetInitHandler(IInitResourceManagerHanler handler)
        {
            this.initHandler = handler;
        }

        private void GetAEIDs()
        {
            if(lstAe != null && lstAe.Count > 0)
            {
                string[] keys = lstAe.Keys.ToArray();

                for(int i = 0; i < keys.Length; i++)
                {
                    string ae_path = lstAe[keys[i]].ResourcePath;

                    AEObject ae_obj = GetAE(ae_path);

                    lstAe[keys[i]].AccessControlPolicy = ae_obj.AEID;
                }
            }
        }

        private void GetAEResourceData()
        {
            Debug.WriteLine("Request URL: [GET] " + strReqAeUrl);
            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strReqAeUrl);
                req.Proxy = null;
                req.Method = "GET";
                req.Accept = "application/json";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                //req.Headers.Add("X-M2M-Origin", "C" + this.aeName);
                req.Headers.Add("X-M2M-Origin", this.origin);
                req.Timeout = nHttpConnectTimeout;

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader sr = new StreamReader(resp.GetResponseStream());

                    string content = sr.ReadToEnd();
                    sr.Close();

                    string[] aryPath = parseUrilsFormJSON(content);
                    int count = 0;

                    for (int i = 0; i < aryPath.Length; i++)
                    {
                        if (aryPath[i].Trim().Length > 0)
                        {
                            //test
                            if (!aryPath[i].StartsWith("/"))
                            {
                                aryPath[i] = "/" + aryPath[i];
                            }

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
                req.Accept = "application/json";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                //req.Headers.Add("X-M2M-Origin", "C" + this.aeName);
                req.Headers.Add("X-M2M-Origin", this.origin);
                req.Timeout = nHttpConnectTimeout;

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader sr = new StreamReader(resp.GetResponseStream());

                    string content = sr.ReadToEnd();
                    sr.Close();

                    string[] aryPath = parseUrilsFormJSON(content);

                    int count = 0;

                    for (int i = 0; i < aryPath.Length; i++)
                    {
                        if (aryPath[i].Trim().Length > 0)
                        {
                            //test
                            if (!aryPath[i].StartsWith("/"))
                            {
                                aryPath[i] = "/" + aryPath[i];
                            }

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
                }
                resp.Close();
            }
            catch (WebException)
            {
                Debug.WriteLine("Container resource request timeout");
            }
        }

        private void GetTimeSeriesResourceData()
        {
            Debug.WriteLine("Request URL: [GET] " + strReqTsUrl);
            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strReqTsUrl);
                req.Proxy = null;
                req.Method = "GET";
                req.Accept = "application/json";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                //req.Headers.Add("X-M2M-Origin", "C" + this.aeName);
                req.Headers.Add("X-M2M-Origin", this.origin);
                req.Timeout = nHttpConnectTimeout;

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader sr = new StreamReader(resp.GetResponseStream());

                    string content = sr.ReadToEnd();

                    sr.Close();

                    string[] aryPath = parseUrilsFormJSON(content);
                    int count = 0;

                    for (int i = 0; i < aryPath.Length; i++)
                    {
                        if (aryPath[i].Trim().Length > 0)
                        {
                            //test
                            if (!aryPath[i].StartsWith("/"))
                            {
                                aryPath[i] = "/" + aryPath[i];
                            }

                            string[] strAry = aryPath[i].Split('/');

                            ResourceObject resc = new ResourceObject();
                            resc.ResourceType = "TimeSeries";
                            resc.ResourcePath = aryPath[i];
                            int startIndex = aryPath[i].LastIndexOf('/') + 1;
                            int length = aryPath[i].Length - startIndex;
                            resc.ResourceName = aryPath[i].Substring(startIndex, length);
                            resc.ParentPath = aryPath[i].Substring(0, aryPath[i].Length - length - 1);
                            count++;
                            if (!lstTs.ContainsKey(resc.ResourcePath))
                            {
                                lstTs.Add(resc.ResourcePath, resc);
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

        private string[] parseUrilsFormJSON(string jsonMsg)
        {
            List<string> result = new List<string>();
            var json = JObject.Parse(jsonMsg);

            if (json["m2m:uril"].Type == JTokenType.Array)
            {

                JArray jsonArry = json["m2m:uril"] as JArray;

                foreach (var item in jsonArry.Children())
                {
                    string value = item.ToString();

                    result.Add(value);
                }

                return result.ToArray();

            }
            else if (json["m2m:uril"].Type == JTokenType.String)
            {
                return json["m2m:uril"].ToString().Split(' ');
            }
            else
            {
                return result.ToArray();
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
                req.Accept = "application/json";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                //req.Headers.Add("X-M2M-Origin", "C" + this.aeName);
                req.Headers.Add("X-M2M-Origin", this.origin);
                req.Timeout = nHttpConnectTimeout;

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader sr = new StreamReader(resp.GetResponseStream());

                    var content = sr.ReadToEnd();
                    sr.Close();

                    string[] aryPath = parseUrilsFormJSON(content);

                    int count = 0;

                    for (int i = 0; i < aryPath.Length; i++)
                    {
                        if (aryPath[i].Trim().Length > 0)
                        {
                            //test
                            if (!aryPath[i].StartsWith("/"))
                            {
                                aryPath[i] = "/" + aryPath[i];
                            }

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
                req.Accept = "application/json";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                //req.Headers.Add("X-M2M-Origin", "C" + this.aeName);
                req.Headers.Add("X-M2M-Origin", this.origin);
                req.Timeout = nHttpConnectTimeout;

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader sr = new StreamReader(resp.GetResponseStream());

                    string content = sr.ReadToEnd();
                    sr.Close();

                    string[] aryPath = parseUrilsFormJSON(content);

                    int count = 0;

                    for (int i = 0; i < aryPath.Length; i++)
                    {
                        if (aryPath[i].Trim().Length > 0)
                        {
                            //test
                            if (!aryPath[i].StartsWith("/"))
                            {
                                aryPath[i] = "/" + aryPath[i];
                            }

                            string[] strAry = aryPath[i].Split('/');

                            ResourceObject resc = new ResourceObject();
                            resc.ResourceType = "SemanticDescriptor";
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

        private void GetGroupResourceData()
        {
            Debug.WriteLine("Request URL: [GET] " + strReqGrUrl);
            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strReqGrUrl);
                req.Proxy = null;
                req.Method = "GET";
                req.Accept = "application/json";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                //req.Headers.Add("X-M2M-Origin", "C" + this.aeName);
                req.Headers.Add("X-M2M-Origin", this.origin);
                req.Timeout = nHttpConnectTimeout;

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader sr = new StreamReader(resp.GetResponseStream());

                    string content = sr.ReadToEnd();
                    sr.Close();

                    string[] aryPath = parseUrilsFormJSON(content);

                    int count = 0;

                    for (int i = 0; i < aryPath.Length; i++)
                    {
                        if (aryPath[i].Trim().Length > 0)
                        {
                            //test
                            if (!aryPath[i].StartsWith("/"))
                            {
                                aryPath[i] = "/" + aryPath[i];
                            }

                            string[] strAry = aryPath[i].Split('/');

                            ResourceObject resc = new ResourceObject();
                            resc.ResourceType = "Group";
                            resc.ResourcePath = aryPath[i];
                            int startIndex = aryPath[i].LastIndexOf('/') + 1;
                            int length = aryPath[i].Length - startIndex;
                            resc.ResourceName = aryPath[i].Substring(startIndex, length);
                            resc.ParentPath = aryPath[i].Substring(0, aryPath[i].Length - length - 1);
                            count++;
                            if (!lstGr.ContainsKey(resc.ResourcePath))
                            {
                                lstGr.Add(resc.ResourcePath, resc);
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

            string strUrl = this.rootUrl + containerPath + @"?fu=1&ty=4&lim=5&lvl=1";

            Debug.WriteLine("Request URL: [GET] " + strUrl);

            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);
                req.Proxy = null;
                req.Method = "GET";
                req.Accept = "application/json";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                //req.Headers.Add("X-M2M-Origin", "C" + this.aeName);
                req.Headers.Add("X-M2M-Origin", this.origin);

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                List<ResourceObject> lstResources = new List<ResourceObject>();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader sr = new StreamReader(resp.GetResponseStream());

                    string content = sr.ReadToEnd();

                    JObject json = JObject.Parse(content);

                    if (json["m2m:uril"] != null)
                    {
                        string[] aryPath = parseUrilsFormJSON(content);

                        for (int i = 0; i < aryPath.Length; i++)
                        {
                            if (aryPath[i].Trim().Length > 0)
                            {
                                //Debug.WriteLine(aryPath[i]);
                                //test
                                if (!aryPath[i].StartsWith("/"))
                                {
                                    aryPath[i] = "/" + aryPath[i];
                                }

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

        private ResourceObject[] GetTimeSeriesContentInstanceResoureceDataByTimeSeries(string containerPath)
        {
            ResourceObject[] resources = null; ;

            string strUrl = this.rootUrl + containerPath + @"?fu=1&ty=30&lim=5&lvl=1";

            Debug.WriteLine("Request URL: [GET] " + strUrl);

            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);
                req.Proxy = null;
                req.Method = "GET";
                req.Accept = "application/json";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                //req.Headers.Add("X-M2M-Origin", "C" + this.aeName);
                req.Headers.Add("X-M2M-Origin", this.origin);

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                List<ResourceObject> lstResources = new List<ResourceObject>();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader sr = new StreamReader(resp.GetResponseStream());

                    string content = sr.ReadToEnd();

                    JObject json = JObject.Parse(content);

                    if (json["m2m:uril"] != null)
                    {
                        string[] aryPath = parseUrilsFormJSON(content);

                        for (int i = 0; i < aryPath.Length; i++)
                        {
                            if (aryPath[i].Trim().Length > 0)
                            {
                                //Debug.WriteLine(aryPath[i]);
                                //test
                                if (!aryPath[i].StartsWith("/"))
                                {
                                    aryPath[i] = "/" + aryPath[i];
                                }

                                string[] strAry = aryPath[i].Split('/');

                                ResourceObject resc = new ResourceObject();
                                resc.ResourceType = "TimeSeriesContentInstance";
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

        public bool IsSubscriptionExisted(string path)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(path);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [GET] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "GET";
                req.ContentType = "application/vnd.onem2m-res+xml";
                req.Accept = "application/xml";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                //req.Headers.Add("X-M2M-Origin", "C" + this.aeName);
                req.Headers.Add("X-M2M-Origin", this.origin);

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

        public bool CreateSubscription(string path, SubscriptionObject sub)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(path);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [POST] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "POST";
                req.Accept = "application/xml";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=23";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                req.Headers.Add("X-M2M-Origin", this.origin);

                string req_content = sub.ToString(OneM2MResourceMessageType.XML);

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

                            sub.Parse(resp_content, OneM2MResourceMessageType.XML);
                        }
                    }
                }
                resp.Close();
            }
            catch (WebException exp)
            {
                Debug.WriteLine(exp.Message);
            }

            return bResult;
        }

        public bool UpdateSubscription(string path, SubscriptionObject sub)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(path).Append(@"/").Append(sub.RN);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [PUT] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "PUT";
                req.Accept = "application/xml";
                req.ContentType = "application/vnd.onem2m-res+xml";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                //req.Headers.Add("X-M2M-Origin", "C" + this.aeName);
                req.Headers.Add("X-M2M-Origin", this.origin);

                string req_content = sub.ToString(OneM2MResourceMessageType.XML);

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
            catch (Exception exp)
            {
                Debug.WriteLine(exp.Message);
            }

            return bResult;
        }

        public AEObject GetAE(string path)
        {
            var ae = new AEObject();
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(path);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL:[GET] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "GET";
                req.Accept = "application/xml";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                req.Headers.Add("X-M2M-Origin", "S");

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    {
                        string resp_content = sr.ReadToEnd();
                        sr.Close();

                        ae.Parse(resp_content, OneM2MResourceMessageType.XML);
                    }
                }

                resp.Close();
            }
            catch (WebException exp)
            {
                Debug.WriteLine(exp.Message);
            }

            return ae;
        }

        public bool CreaetAE(string path, AEObject ae)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(path);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [POST] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "POST";
                req.Accept = "application/xml";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=2";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                req.Headers.Add("X-M2M-Origin", this.origin);

                string req_content = ae.ToString(OneM2MResourceMessageType.XML);

                using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(req_content);
                    sw.Flush();
                    sw.Close();
                }

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.Created)
                {
                    bResult = true;

                    if (resp.StatusCode == HttpStatusCode.Created)
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            string resp_content = sr.ReadToEnd();
                            sr.Close();

                            ae.Parse(resp_content, OneM2MResourceMessageType.XML);
                        }
                    }
                }
                resp.Close();
            }
            catch (WebException exp)
            {
                Debug.WriteLine(exp.Message);
            }

            return bResult;
        }

        public bool CreaetAE(string path, AEObject ae, string acp)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(path);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [POST] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "POST";
                req.Accept = "application/xml";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=2";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                if (acp == null)
                {
                    req.Headers.Add("X-M2M-Origin", this.origin);
                }
                else
                {
                    req.Headers.Add("X-M2M-Origin", acp);
                }
            
                string req_content = ae.ToString(OneM2MResourceMessageType.XML);

                using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(req_content);
                    sw.Flush();
                    sw.Close();
                }

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.Created)
                {
                    bResult = true;

                    if (resp.StatusCode == HttpStatusCode.Created)
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            string resp_content = sr.ReadToEnd();
                            sr.Close();

                            ae.Parse(resp_content, OneM2MResourceMessageType.XML);
                        }
                    }
                }
                resp.Close();
            }
            catch (WebException exp)
            {
                Debug.WriteLine(exp.Message);
            }

            return bResult;
        }

        public bool CreateContainer(string path, ContainerObject cnt)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(path);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [POST] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "POST";
                req.Accept = "application/xml";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=3";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                req.Headers.Add("X-M2M-Origin", this.origin);

                string req_content = cnt.ToString(OneM2MResourceMessageType.XML);

                using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(req_content);
                    sw.Flush();
                    sw.Close();
                }

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.Created)
                {
                    bResult = true;

                    if (resp.StatusCode == HttpStatusCode.Created)
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            string resp_content = sr.ReadToEnd();
                            sr.Close();

                            cnt.Parse(resp_content, OneM2MResourceMessageType.XML);
                        }
                    }
                }
                resp.Close();
            }
            catch (WebException exp)
            {
                Debug.WriteLine(exp.Message);
            }

            return bResult;
        }

        public bool CreateContainer(string path, ContainerObject cnt, string acp)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(path);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [POST] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "POST";
                req.Accept = "application/xml";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=3";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                if (acp == null)
                {
                    req.Headers.Add("X-M2M-Origin", this.origin);
                }
                else
                {
                    req.Headers.Add("X-M2M-Origin", acp);
                }

                string req_content = cnt.ToString(OneM2MResourceMessageType.XML);

                using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(req_content);
                    sw.Flush();
                    sw.Close();
                }

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.Created)
                {
                    bResult = true;

                    if (resp.StatusCode == HttpStatusCode.Created)
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            string resp_content = sr.ReadToEnd();
                            sr.Close();

                            cnt.Parse(resp_content, OneM2MResourceMessageType.XML);
                        }
                    }
                }
                resp.Close();
            }
            catch (WebException exp)
            {
                Debug.WriteLine(exp.Message);
            }

            return bResult;
        }

        public bool CreateContentInstance(string path, ContentInstanceObject cin)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(path);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [POST] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "POST";
                req.Accept = "application/xml";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=4";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                req.Headers.Add("X-M2M-Origin", this.origin);

                string req_content = cin.ToString(OneM2MResourceMessageType.XML);

                using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(req_content);
                    sw.Flush();
                    sw.Close();
                }

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.Created)
                {
                    bResult = true;

                    if (resp.StatusCode == HttpStatusCode.Created)
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            string resp_content = sr.ReadToEnd();
                            sr.Close();

                            cin.Parse(resp_content, OneM2MResourceMessageType.XML);
                        }
                    }
                }
                resp.Close();
            }
            catch (WebException exp)
            {
                Debug.WriteLine(exp.Message);
            }

            return bResult;
        }

        public bool CreateContentInstance(string path, ContentInstanceObject cin, string acp)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(path);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [POST] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "POST";
                req.Accept = "application/xml";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=4";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                if (acp == null)
                {
                    req.Headers.Add("X-M2M-Origin", this.origin);
                }
                else
                {
                    req.Headers.Add("X-M2M-Origin", acp);
                }

                string req_content = cin.ToString(OneM2MResourceMessageType.XML);

                using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(req_content);
                    sw.Flush();
                    sw.Close();
                }

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.Created)
                {
                    bResult = true;

                    if (resp.StatusCode == HttpStatusCode.Created)
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            string resp_content = sr.ReadToEnd();
                            sr.Close();

                            cin.Parse(resp_content, OneM2MResourceMessageType.XML);
                        }
                    }
                }
                resp.Close();
            }
            catch (WebException exp)
            {
                Debug.WriteLine(exp.Message);
            }

            return bResult;
        }

        public bool CreateGroup(string path, GroupObject grp)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(path);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [POST] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "POST";
                req.Accept = "application/xml";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=9";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                req.Headers.Add("X-M2M-Origin", this.origin);

                string req_content = grp.ToString(OneM2MResourceMessageType.XML);

                using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(req_content);
                    sw.Flush();
                    sw.Close();
                }

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.Created)
                {
                    bResult = true;

                    if (resp.StatusCode == HttpStatusCode.Created)
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            string resp_content = sr.ReadToEnd();
                            sr.Close();

                            grp.Parse(resp_content, OneM2MResourceMessageType.XML);
                        }
                    }
                }
                resp.Close();
            }
            catch (WebException exp)
            {
                Debug.WriteLine(exp.Message);
            }

            return bResult;
        }

        public bool CreateGroup(string path, GroupObject grp, string acp)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(path);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [POST] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "POST";
                req.Accept = "application/xml";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=9";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                if (acp == null)
                {
                    req.Headers.Add("X-M2M-Origin", this.origin);
                }else
                {
                    req.Headers.Add("X-M2M-Origin", acp);
                }

                string req_content = grp.ToString(OneM2MResourceMessageType.XML);

                using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(req_content);
                    sw.Flush();
                    sw.Close();
                }

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.Created)
                {
                    bResult = true;

                    if (resp.StatusCode == HttpStatusCode.Created)
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            string resp_content = sr.ReadToEnd();
                            sr.Close();

                            grp.Parse(resp_content, OneM2MResourceMessageType.XML);
                        }
                    }
                }
                resp.Close();
            }
            catch (WebException exp)
            {
                Debug.WriteLine(exp.Message);
            }

            return bResult;
        }

        public bool CreateTimeSeries(string path, TimeSeriesObject ts)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(path);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [POST] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "POST";
                req.Accept = "application/xml";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=29";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                req.Headers.Add("X-M2M-Origin", this.origin);

                string req_content = ts.ToString(OneM2MResourceMessageType.XML);

                using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(req_content);
                    sw.Flush();
                    sw.Close();
                }

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.Created)
                {
                    bResult = true;

                    if (resp.StatusCode == HttpStatusCode.Created)
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            string resp_content = sr.ReadToEnd();
                            sr.Close();

                            ts.Parse(resp_content, OneM2MResourceMessageType.XML);
                        }
                    }
                }
                resp.Close();
            }
            catch (WebException exp)
            {
                Debug.WriteLine(exp.Message);
            }

            return bResult;
        }

        public bool CreateTimeSeriesContentInstance(string path, TimeSeriesContentInstanceObject tsi)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(path);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [POST] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "POST";
                req.Accept = "application/xml";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=30";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                req.Headers.Add("X-M2M-Origin", this.origin);

                string req_content = tsi.ToString(OneM2MResourceMessageType.XML);

                using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(req_content);
                    sw.Flush();
                    sw.Close();
                }

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.Created)
                {
                    bResult = true;

                    if (resp.StatusCode == HttpStatusCode.Created)
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            string resp_content = sr.ReadToEnd();
                            sr.Close();

                            tsi.Parse(resp_content, OneM2MResourceMessageType.XML);
                        }
                    }
                }
                resp.Close();
            }
            catch (WebException exp)
            {
                Debug.WriteLine(exp.Message);
            }

            return bResult;
        }

        public bool CreateSemanticDescriptor(string path, SemanticDescriptorObject sd)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(path);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [POST] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "POST";
                req.Accept = "application/xml";
                req.ContentType = "application/vnd.onem2m-res+xml;ty=24";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                req.Headers.Add("X-M2M-Origin", this.origin);

                string req_content = sd.ToString(OneM2MResourceMessageType.XML);

                using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(req_content);
                    sw.Flush();
                    sw.Close();
                }

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.Created)
                {
                    bResult = true;

                    if (resp.StatusCode == HttpStatusCode.Created)
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            string resp_content = sr.ReadToEnd();
                            sr.Close();

                            sd.Parse(resp_content, OneM2MResourceMessageType.XML);
                        }
                    }
                }
                resp.Close();
            }
            catch (WebException exp)
            {
                Debug.WriteLine(exp.Message);
            }

            return bResult;
        }

        public string GetResourceInfo(string resc_path, string body_type)
        {
            string strResult = "";
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(resc_path);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [GET] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);
                req.Method = "GET";
                if (body_type == "XML")
                {
                    req.Accept = "application/xml";
                }
                else
                {
                    req.Accept = "application/json";
                }
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                req.Headers.Add("X-M2M-Origin", origin);
                req.Timeout = 10000;

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader sr = new StreamReader(resp.GetResponseStream());
                    string content = sr.ReadToEnd();

                    strResult = content;
                }
            }
            catch (WebException exp)
            {
                throw exp;
                //MessageBox.Show("Can not get resource information from mobius. checke the network status and try it again!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return strResult;
        }

        public void GetResourceInfoAsync(string resc_path, string body_type, IGetResourceInfoCallback callback)
        {
            Task task = new Task(() =>
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(this.rootUrl).Append(resc_path);

                    string strUrl = sb.ToString();
                    Debug.WriteLine("Request URL: [GET] " + strUrl);

                    HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);
                    req.Method = "GET";
                    if (body_type == "XML")
                    {
                        req.Accept = "application/xml";
                    }
                    else
                    {
                        req.Accept = "application/json";
                    }
                    req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                    req.Headers.Add("X-M2M-Origin", origin);
                    req.Timeout = 10000;

                    var resp = (HttpWebResponse)req.GetResponse();

                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        StreamReader sr = new StreamReader(resp.GetResponseStream());
                        string content = sr.ReadToEnd();

                        if (callback != null)
                        {
                            callback.GetResourceInfoFinish(content);
                        }
                    }
                }
                catch (WebException exp)
                {
                    throw exp;
                    //MessageBox.Show("Can not get resource information from mobius. checke the network status and try it again!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            task.Start();
        }

        public bool DeleteResource(string resc_path, string acp)
        {
            bool bResult = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.rootUrl).Append(resc_path);

                string strUrl = sb.ToString();
                Debug.WriteLine("Request URL: [DELETE] " + strUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);

                req.Proxy = null;
                req.Method = "DELETE";
                req.Accept = "application/onem2m-resource+xml";
                req.Headers.Add("X-M2M-RI", Guid.NewGuid().ToString());
                //req.Headers.Add("X-M2M-Origin", this.origin);
                if (acp == null)
                {
                    req.Headers.Add("X-M2M-Origin", this.origin);
                }
                else
                {
                    req.Headers.Add("X-M2M-Origin", acp);
                }
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    bResult = true;
                }
                resp.Close();
            }
            catch (WebException exp)
            {
                HttpWebResponse resp = exp.Response as HttpWebResponse;
                HttpStatusCode status_code = resp.StatusCode;

                if(status_code == HttpStatusCode.Forbidden)
                {
                    OneM2MException m2m_exp = new OneM2MException();
                    m2m_exp.ExceptionCode = 403;
                    
                    throw m2m_exp;
                }
                
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
                if (this.cseBase != null)
                {

                    lstTemp.Add(this.cseBase);

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
                    foreach (string path in lstTs.Keys)
                    {
                        lstTemp.Add(lstTs[path]);
                    }
                    for (int i = 0; i < lstTsCin.Count; i++)
                    {
                        lstTemp.Add(lstTsCin[i]);
                    }
                    foreach (string path in lstGr.Keys)
                    {
                        lstTemp.Add(lstGr[path]);
                    }
                    foreach (string path in lstSub.Keys)
                    {
                        lstTemp.Add(lstSub[path]);
                    }
                    foreach (string path in lstSem.Keys)
                    {
                        lstTemp.Add(lstSem[path]);
                    }
                }
            }
            catch
            {

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
                case "5": return "CSEBase";
                case "9": return "Group";
                case "23": return "Subscription";
                case "24": return "SemanticDescriptor";
                case "29": return "TimeSeries";
                case "30": return "TimeSeriesContentInstance";
                default: return "Unknown";
            }
        }

        public void AddResource(string r_name, string p_path, string r_type)
        {
            if (Status == "working")
            {
                var resourceName = r_name;
                var parentPath = p_path;
                var resourcePath = parentPath + @"/" + resourceName;
                var resourceType = GetResoureceTypeString(r_type);

                Debug.WriteLine("Receive a creation notification form [" + r_type + "] resourece named [" + r_name + "]");

                ResourceObject newResc = new ResourceObject()
                {
                    ParentPath = parentPath,
                    ResourceName = resourceName,
                    ResourcePath = resourcePath,
                    ResourceType = resourceType,
                    ResourceStatus = ResourceStatusOption.New
                };

                if (r_type == "2")
                {
                    if (!lstAe.ContainsKey(resourcePath))
                    {
                        lstAe.Add(resourcePath, newResc);

                        string noti_uri = @"mqtt://" + this.ip + @"/" + this.aeID + @"?ct=xml";
                        string sub_name = this.aeName + @"_sub";
                        string parent_path = resourcePath;
                        string resc_path = parent_path + @"/" + sub_name;

                        SubscriptionObject sub = new SubscriptionObject();
                        sub.RN = sub_name;
                        sub.NU = new string[] { noti_uri };
                        sub.NET = new string[] { "3", "4" };

                        if (CreateSubscription(parent_path, sub))
                        {
                            ResourceObject subResourece = new ResourceObject()
                            {
                                ParentPath = resourcePath,
                                ResourceName = sub.RN,
                                ResourcePath = resourcePath + @"/" + sub.RN,
                                ResourceType = "Subscription",
                                ResourceStatus = ResourceStatusOption.New
                            };

                            if (!lstSub.ContainsKey(subResourece.ResourcePath))
                            {
                                try
                                {
                                    lstSub.Add(subResourece.ResourcePath, subResourece);
                                }
                                catch
                                {

                                }
                            }
                        }
                    }
                }
                else if (r_type == "3")
                {
                    if (!lstCon.ContainsKey(resourcePath))
                    {
                        lstCon.Add(resourcePath, newResc);

                        string noti_uri = @"mqtt://" + this.ip + @"/" + this.aeID + @"?ct=xml";
                        string sub_name = this.aeName + @"_sub";
                        string parent_path = resourcePath;
                        string resc_path = parent_path + @"/" + sub_name;

                        SubscriptionObject sub = new SubscriptionObject();
                        sub.RN = sub_name;
                        sub.NU = new string[] { noti_uri };
                        sub.NET = new string[] { "3", "4" };

                        if (CreateSubscription(parent_path, sub))
                        {
                            ResourceObject subResourece = new ResourceObject()
                            {
                                ParentPath = resourcePath,
                                ResourceName = sub.RN,
                                ResourcePath = resourcePath + @"/" + sub.RN,
                                ResourceType = "Subscription",
                                ResourceStatus = ResourceStatusOption.New
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
                    for (int i = 0; i < lstCin.Count; i++)
                    {
                        if (lstCin[i].ResourcePath == resourcePath)
                        {
                            return;
                        }
                    }

                    List<ResourceObject> temp = new List<ResourceObject>();

                    for (int i = 0; i < lstCin.Count; i++)
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
                        lstCin.Add(temp[i]);
                    }
                }
                else if (r_type == "9")
                {
                    if (!lstGr.ContainsKey(newResc.ResourcePath))
                    {
                        lstGr.Add(newResc.ResourcePath, newResc);
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
                else if (r_type == "29")
                {
                    if (!lstTs.ContainsKey(resourcePath))
                    {
                        lstTs.Add(resourcePath, newResc);

                        string noti_uri = @"mqtt://" + this.ip + @"/" + this.aeID + @"?ct=xml";
                        string sub_name = this.aeName + @"_sub";
                        string parent_path = resourcePath;
                        string resc_path = parent_path + @"/" + sub_name;

                        SubscriptionObject sub = new SubscriptionObject();
                        sub.RN = sub_name;
                        sub.NU = new string[] { noti_uri };
                        sub.NET = new string[] { "3", "4" };

                        if (CreateSubscription(parent_path, sub))
                        {
                            ResourceObject subResourece = new ResourceObject()
                            {
                                ParentPath = resourcePath,
                                ResourceName = sub.RN,
                                ResourcePath = resourcePath + @"/" + sub.RN,
                                ResourceType = "Subscription",
                                ResourceStatus = ResourceStatusOption.New
                            };

                            if (!lstSub.ContainsKey(subResourece.ResourcePath))
                            {
                                lstSub.Add(subResourece.ResourcePath, subResourece);
                            }
                        }
                    }
                }
                else if (r_type == "30")
                {
                    for (int i = 0; i < lstTsCin.Count; i++)
                    {
                        if (lstTsCin[i].ResourcePath == resourcePath)
                        {
                            return;
                        }
                    }

                    List<ResourceObject> temp = new List<ResourceObject>();

                    for (int i = 0; i < lstTsCin.Count; i++)
                    {
                        if (lstTsCin[i].ParentPath == parentPath)
                        {
                            temp.Add(lstTsCin[i]);
                        }
                    }

                    for (int i = 0; i < temp.Count; i++)
                    {
                        lstTsCin.Remove(temp[i]);
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
                        //Debug.WriteLine("[" + i + "]: " + temp[i].ResourcePath);
                        lstTsCin.Add(temp[i]);
                    }
                }
            }
        }

        public void RemoveResource(string r_name, string p_path, string r_type)
        {
            if (Status == "working")
            {
                var resourceName = r_name;
                var parentPath = p_path;
                var resourcePath = parentPath + @"/" + resourceName;
                var resourceType = GetResoureceTypeString(r_type);

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

                        keys = lstTs.Keys.ToArray();

                        for (int i = 0; i < keys.Length; i++)
                        {
                            if (keys[i].StartsWith(resourcePath + @"/"))
                            {
                                lstTs.Remove(keys[i]);
                            }
                        }

                        for (int i = 0; i < lstTsCin.Count; i++)
                        {
                            if (lstTsCin[i].ResourcePath.StartsWith(resourcePath + @"/"))
                            {
                                lstTsCin.RemoveAt(i);
                                i--;
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

                        keys = lstTs.Keys.ToArray();

                        for (int i = 0; i < keys.Length; i++)
                        {
                            if (keys[i].StartsWith(resourcePath + @"/"))
                            {
                                lstTs.Remove(keys[i]);
                            }
                        }

                        for (int i = 0; i < lstTsCin.Count; i++)
                        {
                            if (lstTsCin[i].ResourcePath.StartsWith(resourcePath + @"/"))
                            {
                                lstTsCin.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }
                else if (r_type == "4")
                {
                    for (int i = 0; i < lstCin.Count; i++)
                    {
                        if (lstCin[i].ResourcePath == resourcePath)
                        {
                            lstCin.RemoveAt(i);
                            i--;
                        }
                    }

                    ResourceObject[] cins = GetContentInstanceResourceDataByContainer(parentPath);

                    Dictionary<string, ResourceObject> temps = new Dictionary<string, ResourceObject>();

                    for (int i = 0; i < lstCin.Count; i++)
                    {
                        if (!temps.ContainsKey(lstCin[i].ResourcePath))
                        {
                            temps.Add(lstCin[i].ResourcePath, lstCin[i]);
                        }
                    }

                    if (cins != null && cins.Length > 0)
                    {
                        for (int i = 0; i < cins.Length; i++)
                        {
                            if (!temps.ContainsKey(cins[i].ResourcePath))
                            {
                                cins[i].ResourceStatus = ResourceStatusOption.Old;
                                lstCin.Add(cins[i]);
                            }
                        }
                    }
                }
                else if (r_type == "9")
                {
                    if (lstGr.ContainsKey(resourcePath))
                    {
                        lstGr.Remove(resourcePath);
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
                else if (r_type == "29")
                {
                    if (lstTs.ContainsKey(resourcePath))
                    {
                        lstTs.Remove(resourcePath);

                        string[] keys = lstAe.Keys.ToArray();

                        for (int i = 0; i < keys.Length; i++)
                        {
                            if (keys[i].StartsWith(resourcePath + @"/"))
                            {
                                lstAe.Remove(keys[i]);
                            }
                        }

                        keys = lstCon.Keys.ToArray();

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

                        for (int i = 0; i < lstTsCin.Count; i++)
                        {
                            if (lstTsCin[i].ResourcePath.StartsWith(resourcePath + @"/"))
                            {
                                lstTsCin.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }
                else if (r_type == "30")
                {
                    for (int i = 0; i < lstTsCin.Count; i++)
                    {
                        if (lstTsCin[i].ResourcePath == resourcePath)
                        {
                            lstTsCin.RemoveAt(i);
                            i--;
                        }
                    }

                    ResourceObject[] cins = GetTimeSeriesContentInstanceResoureceDataByTimeSeries(parentPath);

                    Dictionary<string, ResourceObject> temps = new Dictionary<string, ResourceObject>();

                    for (int i = 0; i < lstTsCin.Count; i++)
                    {
                        if (!temps.ContainsKey(lstTsCin[i].ResourcePath))
                        {
                            temps.Add(lstTsCin[i].ResourcePath, lstTsCin[i]);
                        }
                    }

                    if (cins != null && cins.Length > 0)
                    {
                        for (int i = 0; i < cins.Length; i++)
                        {
                            if (!temps.ContainsKey(cins[i].ResourcePath))
                            {
                                cins[i].ResourceStatus = ResourceStatusOption.Old;
                                lstTsCin.Add(cins[i]);
                            }
                        }
                    }
                }
            }
        }

        public void ReceiveNotificationMessage(string r_name, string p_path, string r_type, string evt_type)
        {
            if (evt_type == "3")
            {
                AddResource(r_name, p_path, r_type);
            }
            else if (evt_type == "4")
            {
                RemoveResource(r_name, p_path, r_type);
            }
        }

        public string EncodeResourceType(string rt_str)
        {
            string rt_code = "0";

            if (rt_str == "CSEBase" || rt_str == "cse")
            {
                rt_code = "5";
            }
            else if (rt_str == "AE" || rt_str == "ae")
            {
                rt_code = "2";
            }
            else if (rt_str == "Container" || rt_str == "cnt")
            {
                rt_code = "3";
            }
            else if (rt_str == "ContentInstance" || rt_str == "cin")
            {
                rt_code = "4";
            }
            else if (rt_str == "Subscription" || rt_str == "sub")
            {
                rt_code = "23";
            }
            else if (rt_str == "SemanticDescriptor" || rt_str == "smd")
            {
                rt_code = "24";
            }
            else if (rt_str == "TimeSeries" || rt_str == "ts")
            {
                rt_code = "25";
            }
            else if (rt_str == "TimeSeriesContentInstance" || rt_str == "tsi")
            {
                rt_code = "26";
            }
            else if (rt_str == "Group" || rt_str == "grp")
            {
                rt_code = "9";
            }

            return rt_code;
        }
    }

    public interface IInitResourceManagerHanler
    {
        void InitResourceManagerCompeleted();
        void StopResourceManagerBegined();
        void StopResourceManagerCompeleted();
    }

    public enum OneM2MResourceType
    {
        CSEBase = 5,
        AE = 2,
        Container = 3,
        ContentInstance = 4,
        Group = 9,
        Subscription = 23,
        SemanticDescriptor = 24,
        TimeSeries = 29,
        TimeSeriesContentInstance = 30,
        Unknown = 0
    }

    public enum OneM2MResourceMessageType
    {
        XML, JSON, Unknown
    }

    public abstract class OneM2MResource
    {
        public const string PREFIX_M2M = @"http://www.onem2m.org/xml/protocols";
        public const string PREFIX_XSI = @"http://www.w3.org/2001/XMLSchema-instance";

        public OneM2MResourceType ResourceType { get; set; }
        public string RN { get; set; }
        //public string ResourcePath { get; set; }
        //public string ParentResourcePath { get; set; }
        public string RI { get; set; }
        public string PI { get; set; }
        public string ResourceBody { get; set; }

        public OneM2MResource(OneM2MResourceType rt)
        {
            this.ResourceType = rt;
        }

        public static OneM2MResourceType ParseResourceTypeFromCode(string code)
        {
            if (code == "5") return OneM2MResourceType.CSEBase;
            else if (code == "2") return OneM2MResourceType.AE;
            else if (code == "3") return OneM2MResourceType.Container;
            else if (code == "4") return OneM2MResourceType.ContentInstance;
            else if (code == "9") return OneM2MResourceType.Group;
            else if (code == "23") return OneM2MResourceType.Subscription;
            else if (code == "24") return OneM2MResourceType.SemanticDescriptor;
            else if (code == "29") return OneM2MResourceType.TimeSeries;
            else if (code == "30") return OneM2MResourceType.TimeSeriesContentInstance;
            else return OneM2MResourceType.Unknown;
        }

        public virtual string ToString(OneM2MResourceMessageType body_type)
        {
            if (body_type == OneM2MResourceMessageType.XML)
            {
                return MakeXMLBody();
            }
            else if (body_type == OneM2MResourceMessageType.JSON)
            {
                return MakeJSONBody();
            }
            else
            {
                return "";
            }
        }

        public virtual void Parse(string msg, OneM2MResourceMessageType body_type)
        {
            if (body_type == OneM2MResourceMessageType.XML)
            {
                ParseXML(msg);
            }
            else if (body_type == OneM2MResourceMessageType.JSON)
            {
                ParseJSON(msg);
            }

            this.ResourceBody = msg;
        }

        protected abstract string MakeXMLBody();

        protected abstract string MakeJSONBody();

        protected abstract void ParseXML(string xml);

        protected abstract void ParseJSON(string json);
    }

    public class CSEObject : OneM2MResource
    {
        public CSEObject() : base(OneM2MResourceType.CSEBase)
        {
        }

        public string CSI { get; set; }
        public string CST { get; set; }
        public string[] SRT { get; set; }
        public string[] POA { get; set; }

        protected override string MakeXMLBody()
        {
            throw new NotImplementedException();
        }

        protected override string MakeJSONBody()
        {
            throw new NotImplementedException();
        }

        protected override void ParseXML(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("m2m", OneM2MResource.PREFIX_M2M);
            nsMgr.AddNamespace("xsi", OneM2MResource.PREFIX_XSI);

            XmlElement eleRoot = doc.SelectSingleNode("m2m:cb") as XmlElement;

            if (eleRoot != null)
            {
                XmlElement eleCSI = eleRoot.SelectSingleNode("csi") as XmlElement;
                XmlElement eleCST = eleRoot.SelectSingleNode("cst") as XmlElement;
                XmlElement eleSRT = eleRoot.SelectSingleNode("srt") as XmlElement;
                XmlElement elePOA = eleRoot.SelectSingleNode("poa") as XmlElement;
                XmlElement eleRI = eleRoot.SelectSingleNode("ri") as XmlElement;
                XmlElement eleRN = eleRoot.SelectSingleNode("rn") as XmlElement;

                if (eleCSI != null)
                {
                    this.CSI = eleCSI.InnerText;
                }

                if (eleCST != null)
                {
                    this.CST = eleCST.InnerText;
                }

                if (eleSRT != null)
                {
                    var str = eleSRT.InnerText;
                    if (str.Contains(" "))
                    {
                        this.SRT = str.Split(' ');
                    }
                    else
                    {
                        this.SRT[0] = str;
                    }
                }

                if (elePOA != null)
                {
                    var str = elePOA.InnerText;
                    if (str.Contains(" "))
                    {
                        this.POA = str.Split(' ');
                    }
                    else
                    {
                        this.POA[0] = str;
                    }
                }

                if (eleRI != null)
                {
                    this.RI = eleRI.InnerText;
                }

                if (eleRN != null)
                {
                    this.RN = eleRN.InnerText;
                }
            }
        }

        protected override void ParseJSON(string json)
        {
        }
    }

    public class AEObject : OneM2MResource
    {
        public string AppID { get; set; }
        public string AEID { get; set; }
        public string RR { get; set; }

        public AEObject() : base(OneM2MResourceType.AE)
        {
        }

        protected override string MakeXMLBody()
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration xmldc = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");

            XmlElement eleRoot = doc.CreateElement("m2m", "ae", OneM2MResource.PREFIX_M2M);

            eleRoot.SetAttribute("xmlns:m2m", OneM2MResource.PREFIX_M2M);
            eleRoot.SetAttribute("xmlns:xsi", OneM2MResource.PREFIX_XSI);

            if (RN != null && RN.Length > 0)
            {
                eleRoot.SetAttribute("rn", this.RN);
            }

            if (AppID != null && AppID.Length > 0)
            {
                XmlElement ele = doc.CreateElement("api");
                ele.InnerText = this.AppID;
                eleRoot.AppendChild(ele);
            }

            XmlElement eleRR = doc.CreateElement("rr");
            eleRR.InnerText = "true";
            eleRoot.AppendChild(eleRR);

            doc.AppendChild(eleRoot);

            doc.InsertBefore(xmldc, eleRoot);

            return doc.OuterXml;
        }

        protected override string MakeJSONBody()
        {
            throw new NotImplementedException();
        }

        protected override void ParseXML(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("m2m", OneM2MResource.PREFIX_M2M);
            nsMgr.AddNamespace("xsi", OneM2MResource.PREFIX_XSI);

            XmlElement eleRoot = doc.SelectSingleNode("m2m:ae", nsMgr) as XmlElement;

            if (eleRoot != null)
            {
                XmlElement eleAPI = eleRoot.SelectSingleNode("api") as XmlElement;
                XmlElement eleAEI = eleRoot.SelectSingleNode("aei") as XmlElement;
                XmlElement elePI = eleRoot.SelectSingleNode("pi") as XmlElement;
                XmlElement eleRI = eleRoot.SelectSingleNode("ri") as XmlElement;
                XmlElement eleRR = eleRoot.SelectSingleNode("rr") as XmlElement;

                if (eleAPI != null)
                {
                    this.AppID = eleAPI.InnerText;
                }

                if (eleAEI != null)
                {
                    this.AEID = eleAEI.InnerText;
                }

                if (eleRR != null)
                {
                    this.RR = eleRR.InnerText;
                }

                if (eleRI != null)
                {
                    this.RI = eleRI.InnerText;
                }

                this.RN = eleRoot.GetAttribute("rn");
            }
        }

        protected override void ParseJSON(string json)
        {
            throw new NotImplementedException();
        }
    }

    public class ContainerObject : OneM2MResource
    {
        public ContainerObject() : base(OneM2MResourceType.Container)
        {
        }

        protected override string MakeJSONBody()
        {
            throw new NotImplementedException();
        }

        protected override string MakeXMLBody()
        {

            XmlDocument doc = new XmlDocument();

            XmlDeclaration xmldc = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");

            XmlElement eleRoot = doc.CreateElement("m2m", "cnt", OneM2MResource.PREFIX_M2M);

            eleRoot.SetAttribute("xmlns:m2m", OneM2MResource.PREFIX_M2M);
            eleRoot.SetAttribute("xmlns:xsi", OneM2MResource.PREFIX_XSI);

            if (RN != null && RN.Length > 0)
            {
                eleRoot.SetAttribute("rn", this.RN);
            }

            doc.AppendChild(eleRoot);

            doc.InsertBefore(xmldc, eleRoot);

            return doc.OuterXml;
        }

        protected override void ParseJSON(string json)
        {
            throw new NotImplementedException();
        }

        protected override void ParseXML(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("m2m", OneM2MResource.PREFIX_M2M);
            nsMgr.AddNamespace("xsi", OneM2MResource.PREFIX_XSI);

            XmlElement eleRoot = doc.SelectSingleNode("m2m:cnt", nsMgr) as XmlElement;

            if (eleRoot != null)
            {
                XmlElement elePI = eleRoot.SelectSingleNode("pi") as XmlElement;
                XmlElement eleRI = eleRoot.SelectSingleNode("ri") as XmlElement;

                if (elePI != null)
                {
                    this.PI = elePI.InnerText;
                }

                if (eleRI != null)
                {
                    this.RI = eleRI.InnerText;
                }

                this.RN = eleRoot.GetAttribute("rn");
            }
        }
    }

    public class ContentInstanceObject : OneM2MResource
    {
        public string CON { get; set; }

        public ContentInstanceObject() : base(OneM2MResourceType.ContentInstance) { }

        protected override string MakeJSONBody()
        {
            throw new NotImplementedException();
        }

        protected override string MakeXMLBody()
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration xmldc = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");

            XmlElement eleRoot = doc.CreateElement("m2m", "cin", OneM2MResource.PREFIX_M2M);

            eleRoot.SetAttribute("xmlns:m2m", OneM2MResource.PREFIX_M2M);
            eleRoot.SetAttribute("xmlns:xsi", OneM2MResource.PREFIX_XSI);

            if (RN != null && RN.Length > 0)
            {
                eleRoot.SetAttribute("rn", this.RN);
            }

            if (CON != null && CON.Length > 0)
            {
                XmlElement ele = doc.CreateElement("con");
                ele.InnerText = this.CON;
                eleRoot.AppendChild(ele);
            }

            doc.AppendChild(eleRoot);

            doc.InsertBefore(xmldc, eleRoot);

            return doc.OuterXml;
        }

        protected override void ParseJSON(string json)
        {
            throw new NotImplementedException();
        }

        protected override void ParseXML(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("m2m", OneM2MResource.PREFIX_M2M);
            nsMgr.AddNamespace("xsi", OneM2MResource.PREFIX_XSI);

            XmlElement eleRoot = doc.SelectSingleNode("m2m:cin", nsMgr) as XmlElement;

            if (eleRoot != null)
            {
                XmlElement elePI = eleRoot.SelectSingleNode("pi") as XmlElement;
                XmlElement eleRI = eleRoot.SelectSingleNode("ri") as XmlElement;
                XmlElement eleCON = eleRoot.SelectSingleNode("con") as XmlElement;

                if (elePI != null)
                {
                    this.PI = elePI.InnerText;
                }

                if (eleRI != null)
                {
                    this.RI = eleRI.InnerText;
                }

                if (eleCON != null)
                {
                    this.CON = eleCON.InnerText;
                }

                this.RN = eleRoot.GetAttribute("rn");
            }
        }
    }

    public class SubscriptionObject : OneM2MResource
    {
        public SubscriptionObject() : base(OneM2MResourceType.Subscription)
        {
        }

        public string[] NU { get; set; }
        public string[] NET { get; set; }

        protected override string MakeXMLBody()
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration xmldc = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");

            XmlElement eleRoot = doc.CreateElement("m2m", "sub", OneM2MResource.PREFIX_M2M);
            XmlElement eleENC = doc.CreateElement("enc");

            eleRoot.SetAttribute("xmlns:m2m", OneM2MResource.PREFIX_M2M);
            eleRoot.SetAttribute("xmlns:xsi", OneM2MResource.PREFIX_XSI);

            if (RN != null && RN.Length > 0)
            {
                eleRoot.SetAttribute("rn", this.RN);
            }

            if (NET != null && NET.Length > 0)
            {
                var str = "";

                for (int i = 0; i < NET.Length; i++)
                {
                    if (i == 0)
                    {
                        str += NET[i];
                    } else
                    {
                        str += " " + NET[i];
                    }
                }

                XmlElement ele = doc.CreateElement("net");
                ele.InnerText = str;
                eleENC.AppendChild(ele);
                eleRoot.AppendChild(eleENC);
            }

            if (NU != null && NU.Length > 0)
            {
                var str = "";

                for (int i = 0; i < NU.Length; i++)
                {
                    if (i == 0)
                    {
                        str += NU[i];
                    }
                    else
                    {
                        str += " " + NU[i];
                    }
                }

                XmlElement ele = doc.CreateElement("nu");
                ele.InnerText = str;
                eleRoot.AppendChild(ele);
            }

            XmlElement elePN = doc.CreateElement("pn");
            elePN.InnerText = "1";
            eleRoot.AppendChild(elePN);

            XmlElement eleNCT = doc.CreateElement("nct");
            eleNCT.InnerText = "2";
            eleRoot.AppendChild(eleNCT);

            doc.AppendChild(eleRoot);

            doc.InsertBefore(xmldc, eleRoot);

            return doc.OuterXml;
        }

        protected override string MakeJSONBody()
        {
            throw new NotImplementedException();
        }

        protected override void ParseXML(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("m2m", OneM2MResource.PREFIX_M2M);
            nsMgr.AddNamespace("xsi", OneM2MResource.PREFIX_XSI);

            XmlElement eleRoot = doc.SelectSingleNode("m2m:cin", nsMgr) as XmlElement;

            if (eleRoot != null)
            {
                XmlElement elePI = eleRoot.SelectSingleNode("pi") as XmlElement;
                XmlElement eleRI = eleRoot.SelectSingleNode("ri") as XmlElement;
                XmlElement eleENC = eleRoot.SelectSingleNode("enc") as XmlElement;
                XmlElement eleNU = eleRoot.SelectSingleNode("nu") as XmlElement;

                if (elePI != null)
                {
                    this.PI = elePI.InnerText;
                }

                if (eleRI != null)
                {
                    this.RI = eleRI.InnerText;
                }

                if (eleENC != null)
                {
                    XmlElement eleNET = eleENC.SelectSingleNode("nu") as XmlElement;

                    if (eleNET != null)
                    {
                        var str = eleNET.InnerText;

                        this.NET = str.Split(' ');
                    }
                }

                if (eleNU != null)
                {
                    var str = eleNU.InnerText;

                    this.NU = str.Split(' ');
                }

                this.RN = eleRoot.GetAttribute("rn");
            }
        }

        protected override void ParseJSON(string json)
        {
            throw new NotImplementedException();
        }
    }

    public class GroupObject : OneM2MResource
    {
        public string[] MID { get; set; }
        public string MNM { get; set; }

        public GroupObject() : base(OneM2MResourceType.Group) { }

        protected override string MakeJSONBody()
        {
            throw new NotImplementedException();
        }

        protected override string MakeXMLBody()
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration xmldc = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");

            XmlElement eleRoot = doc.CreateElement("m2m", "grp", OneM2MResource.PREFIX_M2M);

            eleRoot.SetAttribute("xmlns:m2m", OneM2MResource.PREFIX_M2M);
            eleRoot.SetAttribute("xmlns:xsi", OneM2MResource.PREFIX_XSI);

            if (RN != null && RN.Length > 0)
            {
                eleRoot.SetAttribute("rn", this.RN);
            }

            if (MID != null && MID.Length > 0)
            {
                string str = "";

                for (int i = 0; i < MID.Length; i++)
                {
                    if (i == 0)
                    {
                        str += MID[i];
                    }
                    else
                    {
                        str += " " + MID[i];
                    }
                }

                XmlElement ele = doc.CreateElement("mid");
                ele.InnerText = str;
                eleRoot.AppendChild(ele);
            }

            if (MNM != null && MNM.Length > 0)
            {
                XmlElement ele = doc.CreateElement("mnm");
                ele.InnerText = MNM;
                eleRoot.AppendChild(ele);
            }

            doc.AppendChild(eleRoot);

            doc.InsertBefore(xmldc, eleRoot);

            return doc.OuterXml;
        }

        protected override void ParseJSON(string json)
        {
            throw new NotImplementedException();
        }

        protected override void ParseXML(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("m2m", OneM2MResource.PREFIX_M2M);
            nsMgr.AddNamespace("xsi", OneM2MResource.PREFIX_XSI);

            XmlElement eleRoot = doc.SelectSingleNode("m2m:grp", nsMgr) as XmlElement;

            if (eleRoot != null)
            {
                XmlElement elePI = eleRoot.SelectSingleNode("pi") as XmlElement;
                XmlElement eleRI = eleRoot.SelectSingleNode("ri") as XmlElement;
                XmlElement eleMNM = eleRoot.SelectSingleNode("mnm") as XmlElement;
                XmlElement eleMID = eleRoot.SelectSingleNode("mid") as XmlElement;

                if (elePI != null)
                {
                    this.PI = elePI.InnerText;
                }

                if (eleRI != null)
                {
                    this.RI = eleRI.InnerText;
                }

                if (eleMNM != null)
                {
                    this.MNM = eleMNM.InnerText;
                }

                if (eleMID != null)
                {
                    var str = eleMID.InnerText;

                    this.MID = str.Split(' ');
                }

                this.RN = eleRoot.GetAttribute("rn");
            }
        }
    }

    public class TimeSeriesObject : OneM2MResource
    {
        public string PEI { get; set; }
        public string MDD { get; set; }
        public string MDDT { get; set; }

        public TimeSeriesObject() : base(OneM2MResourceType.TimeSeries) { }

        protected override string MakeJSONBody()
        {
            throw new NotImplementedException();
        }

        protected override string MakeXMLBody()
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration xmldc = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");

            XmlElement eleRoot = doc.CreateElement("m2m", "ts", OneM2MResource.PREFIX_M2M);

            eleRoot.SetAttribute("xmlns:m2m", OneM2MResource.PREFIX_M2M);
            eleRoot.SetAttribute("xmlns:xsi", OneM2MResource.PREFIX_XSI);

            if (RN != null && RN.Length > 0)
            {
                eleRoot.SetAttribute("rn", this.RN);
            }

            XmlElement elePEI = doc.CreateElement("pei");
            elePEI.InnerText = this.PEI;
            eleRoot.AppendChild(elePEI);

            XmlElement eleMDD = doc.CreateElement("mdd");
            eleMDD.InnerText = this.MDD;
            eleRoot.AppendChild(eleMDD);

            XmlElement eleMDDT = doc.CreateElement("mddt");
            eleMDDT.InnerText = this.MDDT;
            eleRoot.AppendChild(eleMDDT);

            doc.AppendChild(eleRoot);

            doc.InsertBefore(xmldc, eleRoot);

            return doc.OuterXml;
        }

        protected override void ParseJSON(string json)
        {
            throw new NotImplementedException();
        }

        protected override void ParseXML(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("m2m", OneM2MResource.PREFIX_M2M);
            nsMgr.AddNamespace("xsi", OneM2MResource.PREFIX_XSI);

            XmlElement eleRoot = doc.SelectSingleNode("m2m:ts", nsMgr) as XmlElement;

            if (eleRoot != null)
            {
                XmlElement elePI = eleRoot.SelectSingleNode("pi") as XmlElement;
                XmlElement eleRI = eleRoot.SelectSingleNode("ri") as XmlElement;
                XmlElement elePEI = eleRoot.SelectSingleNode("pei") as XmlElement;
                XmlElement eleMDD = eleRoot.SelectSingleNode("mdd") as XmlElement;
                XmlElement eleMDDT = eleRoot.SelectSingleNode("mddt") as XmlElement;

                if (elePI != null)
                {
                    this.PI = elePI.InnerText;
                }

                if (eleRI != null)
                {
                    this.RI = eleRI.InnerText;
                }

                if (elePEI != null)
                {
                    this.PEI = elePEI.InnerText;
                }

                if (eleMDD != null)
                {
                    this.MDD = elePEI.InnerText;
                }

                if (eleMDDT != null)
                {
                    this.MDDT = eleMDDT.InnerText;
                }

                this.RN = eleRoot.GetAttribute("rn");
            }
        }
    }

    public class TimeSeriesContentInstanceObject : OneM2MResource
    {
        public string CON { get; set; }
        public string DGT { get; set; }

        public TimeSeriesContentInstanceObject() : base(OneM2MResourceType.TimeSeriesContentInstance)
        {
        }

        protected override string MakeJSONBody()
        {
            throw new NotImplementedException();
        }

        protected override string MakeXMLBody()
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration xmldc = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");

            XmlElement eleRoot = doc.CreateElement("m2m", "tsi", OneM2MResource.PREFIX_M2M);

            eleRoot.SetAttribute("xmlns:m2m", OneM2MResource.PREFIX_M2M);
            eleRoot.SetAttribute("xmlns:xsi", OneM2MResource.PREFIX_XSI);

            if (RN != null && RN.Length > 0)
            {
                eleRoot.SetAttribute("rn", this.RN);
            }

            XmlElement eleDGT = doc.CreateElement("dgt");
            eleDGT.InnerText = this.DGT;
            eleRoot.AppendChild(eleDGT);

            XmlElement eleCON = doc.CreateElement("con");
            eleCON.InnerText = this.CON;
            eleRoot.AppendChild(eleCON);

            doc.AppendChild(eleRoot);

            doc.InsertBefore(xmldc, eleRoot);

            return doc.OuterXml;
        }

        protected override void ParseJSON(string json)
        {
            throw new NotImplementedException();
        }

        protected override void ParseXML(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("m2m", OneM2MResource.PREFIX_M2M);
            nsMgr.AddNamespace("xsi", OneM2MResource.PREFIX_XSI);

            XmlElement eleRoot = doc.SelectSingleNode("m2m:tsi", nsMgr) as XmlElement;

            if (eleRoot != null)
            {
                XmlElement elePI = eleRoot.SelectSingleNode("pi") as XmlElement;
                XmlElement eleRI = eleRoot.SelectSingleNode("ri") as XmlElement;
                XmlElement eleDGT = eleRoot.SelectSingleNode("dgt") as XmlElement;
                XmlElement eleCON = eleRoot.SelectSingleNode("con") as XmlElement;

                if (elePI != null)
                {
                    this.PI = elePI.InnerText;
                }

                if (eleRI != null)
                {
                    this.RI = eleRI.InnerText;
                }

                if (eleDGT != null)
                {
                    this.DGT = eleDGT.InnerText;
                }

                if (eleCON != null)
                {
                    this.CON = eleCON.InnerText;
                }

                this.RN = eleRoot.GetAttribute("rn");
            }
        }
    }

    public class SemanticDescriptorObject : OneM2MResource
    {
        public string DCRP { get; set; }

        public SemanticDescriptorObject() : base(OneM2MResourceType.SemanticDescriptor)
        {
        }

        protected override string MakeJSONBody()
        {
            throw new NotImplementedException();
        }

        protected override string MakeXMLBody()
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration xmldc = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");

            XmlElement eleRoot = doc.CreateElement("m2m", "smd", OneM2MResource.PREFIX_M2M);

            eleRoot.SetAttribute("xmlns:m2m", OneM2MResource.PREFIX_M2M);
            eleRoot.SetAttribute("xmlns:xsi", OneM2MResource.PREFIX_XSI);

            if (RN != null && RN.Length > 0)
            {
                eleRoot.SetAttribute("rn", this.RN);
            }

            if (DCRP != null && DCRP.Length > 0)
            {
                XmlElement ele = doc.CreateElement("dcrp");
                ele.InnerText = EncodingHelper.Base64Encode(this.DCRP);
                //ele.InnerText = this.DSPT;
                eleRoot.AppendChild(ele);
            }

            doc.AppendChild(eleRoot);

            doc.InsertBefore(xmldc, eleRoot);

            return doc.OuterXml;
        }

        protected override void ParseJSON(string json)
        {
            JObject doc = JObject.Parse(json);

            JObject root = doc["m2m:smd"] as JObject;

            if(root != null)
            {
                JValue jRN = root["rn"] as JValue;
                JValue jPI = root["pi"] as JValue;
                JValue jRI = root["ri"] as JValue;
                JValue jDSPT = root["dcrp"] as JValue;

                if(jPI != null)
                {
                    this.PI = jPI.ToObject<string>();
                }

                if(jRI != null)
                {
                    this.RI = jRI.ToObject<string>();
                }

                if (jRN != null)
                {
                    this.RN = jRN.ToObject<string>();
                }

                if (jDSPT != null)
                {
                    this.DCRP = jDSPT.ToObject<string>();
                    this.DCRP = EncodingHelper.Base64Decode(this.DCRP);
                }
            }
        }

        protected override void ParseXML(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("m2m", OneM2MResource.PREFIX_M2M);
            nsMgr.AddNamespace("xsi", OneM2MResource.PREFIX_XSI);

            XmlElement eleRoot = doc.SelectSingleNode("m2m:smd", nsMgr) as XmlElement;

            if (eleRoot != null)
            {
                XmlElement elePI = eleRoot.SelectSingleNode("pi") as XmlElement;
                XmlElement eleRI = eleRoot.SelectSingleNode("ri") as XmlElement;
                XmlElement eleDSPT = eleRoot.SelectSingleNode("dcrp") as XmlElement;

                if (elePI != null)
                {
                    this.PI = elePI.InnerText;
                }

                if (eleRI != null)
                {
                    this.RI = eleRI.InnerText;
                }

                if (eleDSPT != null)
                {
                    this.DCRP = EncodingHelper.Base64Decode(eleDSPT.InnerText);
                    //this.DSPT = eleDSPT.InnerText;
                }

                this.RN = eleRoot.GetAttribute("rn");
            }
        }
    }

    public interface IProgressChanged
    {
        void ProgressChanged(double percent, string messgage);
    }

    public class EncodingHelper
    {
        public static string Base64Encode(string data)
        {
            try
            {
                byte[] encData_byte = new byte[data.Length];
                encData_byte = System.Text.Encoding.UTF8.GetBytes(data);
                string encodedData = Convert.ToBase64String(encData_byte);
                return encodedData;
            }
            catch (Exception e)
            {
                throw new Exception("Error in Base64Encode: " + e.Message);
            }
        }

        public static string Base64Decode(string data)
        {
            try
            {
                System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
                System.Text.Decoder utf8Decode = encoder.GetDecoder();

                byte[] todecode_byte = Convert.FromBase64String(data);
                int charCount = utf8Decode.GetCharCount(todecode_byte, 0, todecode_byte.Length);
                char[] decoded_char = new char[charCount];
                utf8Decode.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
                string result = new String(decoded_char);
                return result;
            }
            catch (Exception e)
            {
                throw new Exception("Error in Base64Decode: " + e.Message);
            }
        }
    }

    public class OneM2MException : Exception
    {
        public int ExceptionCode { get; set; }
    }
}
