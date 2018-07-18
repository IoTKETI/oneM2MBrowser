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
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using uhttpsharp;
using uhttpsharp.Headers;
using uhttpsharp.Listeners;
using uhttpsharp.RequestProviders;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MobiusResourceMonitor_sub
{
    interface INotificationServer
    {
        void Start();
        void Stop();
        void SetReceiverHandler(INotificaitonReceiver receiver);
    }

    interface INotificaitonReceiver
    {
        void ReceiveNotificationMessage(string r_name, string p_path, string r_type, string evt_type);
    }

    class NotificationHttpServer : INotificationServer
    {
        private HttpRequestProvider provider = new HttpRequestProvider();
        private TcpListener tcpListener = null;
        private TcpListenerAdapter tcpListenerAdpt = null;
        private HttpServer server = null;
        private INotificaitonReceiver handler = null;
        public bool IsActived = false; 

        public void Start()
        {
            try
            {
                server = new HttpServer(provider);
                tcpListener = new TcpListener(IPAddress.Any, 11000);
                tcpListenerAdpt = new TcpListenerAdapter(tcpListener);

                server.Use(tcpListenerAdpt);
                server.Use((context, next) =>
                {
                    Debug.WriteLine("Receiver a HTTP request!================================");

                    var request_method = context.Request.Method.ToString();
                    var request_protocol = context.Request.Protocol;
                    var request_uri = context.Request.Uri.ToString();
                    var request_content = Encoding.UTF8.GetString(context.Request.Post.Raw);
                    var headers = context.Request.Headers;

                    Debug.WriteLine("Method: " + request_method);
                    Debug.WriteLine("Protocol: " + request_protocol);
                    Debug.WriteLine("Uri: " + request_uri);
                    Debug.WriteLine("Body: " + request_content);

                    if (request_method.ToUpper() == "POST")
                    {
                        var obj = new NotificationObject(request_content);

                        Debug.WriteLine("Path[" + obj.Path + "] receive data: " + obj.Content);

                        var resource_path = obj.Path;
                        var resource_name = obj.ResourceName;
                        var msg_id = context.Request.Headers.GetByName("X-M2M-RI");

                        //call handler

                        var list_headers = new List<KeyValuePair<string, string>>();

                        list_headers.Add(new KeyValuePair<string, string>("X-M2M-RSC", "2001"));
                        list_headers.Add(new KeyValuePair<string, string>("X-M2M-RI", msg_id));

                        var response_headers = new ListHttpHeaders(list_headers);

                        context.Response = uhttpsharp.StringHttpResponse.Create("", HttpResponseCode.Created, "text/html", true, response_headers);
                    }

                    return Task.Factory.GetCompleted();
                });

                server.Start();

                IsActived = true;
            }
            catch (Exception exp)
            {
                IsActived = false;
                throw exp;
            }
        }
         
        public void Stop()
        {
            server.Dispose();
            tcpListener.Stop();
            IsActived = false;
        }

        public void SetReceiverHandler(INotificaitonReceiver receiver)
        {
            this.handler = receiver;
        }
    }

    class NotifiacitonMqttServer : INotificationServer
    {
        private INotificaitonReceiver handler = null;
        private MqttClient client = null;
        private string clientId = "";
        private string broker_ip = "";
        public bool IsActived = false;
        public string topic = "";

        public NotifiacitonMqttServer(string broker)
        {
            this.broker_ip = broker;
            this.clientId = RandomString(20);
        }

        public void SubscripTopic(string topic)
        {
            this.topic = topic;
            this.client.Subscribe(new string[] { topic }, new byte[] { 0 });
        }

        public void UnSubscripTopic(string topic)
        {
            this.client.Unsubscribe(new string[] { topic });
            this.topic = "";
        }

        private void PublishMessage(string topic, string msg)
        {
            if (msg != null && msg.Length > 0)
            {
                byte[] buff = ASCIIEncoding.UTF8.GetBytes(msg);

                this.client.Publish(topic, buff);
            }
        }

        public void Start()
        {
            try
            {
                this.client = new MqttClient(this.broker_ip);
                this.client.Connect(this.clientId);

                this.client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                this.client.MqttMsgSubscribed += client_MqttMsgSubscribed;

                this.client.ConnectionClosed += client_ConnectionClosed;

                IsActived = true;
            }
            catch (Exception exp)
            {
                IsActived = false;
                throw exp;
            }
        }

        protected void client_ConnectionClosed(object sender, EventArgs e)
        {
            while (!this.client.IsConnected && IsActived)
            {
                this.client = new MqttClient(this.broker_ip);
                this.client.Connect(this.clientId);

                this.client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                this.client.MqttMsgSubscribed += client_MqttMsgSubscribed;

                this.client.ConnectionClosed += client_ConnectionClosed;

                this.client.Subscribe(new string[] { topic }, new byte[] { 0 });

                Thread.Sleep(1000);
            }
        }

        protected void client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            Debug.WriteLine("[" + e.MessageId + "] scripted successfully!");
        }

        protected void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                var strMsg = Encoding.UTF8.GetString(e.Message);
                var topic = e.Topic;

                Debug.WriteLine("Topic: " + topic);
                Debug.WriteLine("Message: " + strMsg);

                string[] path_array = topic.Split('/');

                if (path_array.Length == 6 && strMsg.Length > 0)
                {
                    var obj = new MqttNotificationRequest(strMsg);

                    var id = path_array[3];
                    var ae_id = path_array[4];
                    var res_path = obj.SubscriptionPath;

                    var path_arr = res_path.Split('/');

                    if (path_arr.Length > 0)
                    {
                        var noti_name = path_arr[path_arr.Length - 1];
                        var noti_parent = res_path.Remove(res_path.LastIndexOf("/"), noti_name.Length + 1);

                        //call handler
                        Debug.WriteLine("[" + res_path + "] receive a message");

                        if (!noti_parent.StartsWith("/")) noti_parent = @"/" + noti_parent;

                        if (this.handler != null)
                        {
                            this.handler.ReceiveNotificationMessage(obj.ResourceName, noti_parent, obj.ResourceType, obj.Evt);
                        }

                        var resp_topic = "/oneM2M/resp/" + id + "/" + ae_id + "/xml";

                        var respMsg = new MqttNotificationResponse();
                        respMsg.ResponseCode = "2000";
                        respMsg.RequestID = obj.RequestID;
                        respMsg.Fr = ae_id;
                        respMsg.To = "";
                        respMsg.Pc = "";

                        PublishMessage(resp_topic, respMsg.ToXMLString());
                    }
                }
            }catch(Exception exp)
            {
                Debug.WriteLine(exp.Message);
            }
        }

        public void Stop()
        {
            try
            {
                //this.client.Unsubscribe(new string[] { topic });
                this.client.Disconnect();
                IsActived = false;
            }
            catch
            {
                Environment.Exit(0);
            }
        }

        public void SetReceiverHandler(INotificaitonReceiver receiver)
        {
            this.handler = receiver;
        }
         
        private string RandomString(int length)
        {
            string str = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            
            var random = new Random(Guid.NewGuid().GetHashCode());

            var sb = new StringBuilder();
           
            for (int i = 0; i < length; i++)
            {
                int num = random.Next(62);
                sb.Append(str.Substring(num, 1));
            }
            return sb.ToString();
        }
    }

    class NotificationObject
    {
        public string Path { get; set; }
        public string Content { get; set; }
        public string ResourceName { get; set; }
        public string ResourceType { get; set; }

        public NotificationObject(string xml)
        {
            ParseXML(xml);
        }

        private void ParseXML(string noti_xml)
        {
            try
            {
                FindResourcePath(noti_xml);
                FindContent(noti_xml);
                FindResourceName(noti_xml);
                FindResourceType(noti_xml);
            }
            catch (Exception exp)
            {
                Debug.WriteLine(exp.Message);
            }
        }

        private void FindResourcePath(string str)
        {
            int index_s = -1;
            int index_e = -1;

            index_s = str.IndexOf("<sur>");
            index_e = str.IndexOf("</sur>");

            if (index_s > 0 && index_e > 0)
            {
                this.Path = str.Substring(index_s + 5, index_e - index_s - 5);
                return;
            }
        }

        private void FindContent(string str)
        {
            int index_s = -1;
            int index_e = -1;

            index_s = str.IndexOf("<con>");
            index_e = str.IndexOf("</con>");

            if (index_s > 0 && index_e > 0)
            {
                this.Content = str.Substring(index_s + 5, index_e - 5);
                return;
            }
        }

        private void FindResourceName(string str)
        {
            int index_s = -1;
            int index_e = -1;

            index_s = str.IndexOf("rn=\"");
            index_e = str.IndexOf("\"");

            if (index_s > 0 && index_e > 0)
            {
                this.ResourceName = str.Substring(index_s + 4, index_e);
                return;
            }
        }

        private void FindResourceType(string str)
        {
            int index_s = -1;
            int index_e = -1;

            index_s = str.IndexOf("<ty>");
            index_e = str.IndexOf("</ty>");

            if (index_s > 0 && index_e > 0)
            {
                this.ResourceType = getResourceTypeName(str.Substring(index_s + 4, index_e - 4));
                return;
            }
        }

        private string getResourceTypeName(string ty_code)
        {
            string strResult = "";

            switch (ty_code)
            {
                case "1": strResult = "CSEBase"; break;
                case "2": strResult = "AE"; break;
                case "3": strResult = "Container"; break;
                case "4": strResult = "ContentInstance"; break;
                case "9": strResult = "Group"; break;
                case "23": strResult = "Subscription"; break;
                case "24": strResult = "SemanticDescription"; break;
                case "25": strResult = "TimeSeries"; break;
                case "26": strResult = "TimeSeriesContentInstance"; break;
                default: strResult = "Unkown"; break;
            }

            return strResult;
        }
    }

    class MqttNotificationRequest
    {
        public string Op { get; set; }
        public string To { get; set; }
        public string Fr { get; set; }
        public string Evt { get; set; }
        public string RequestID { get; set; }
        public string SubscriptionPath { get; set; }
        public string ResourceName { get; set; }
        public string ResourceType { get; set; }

        public MqttNotificationRequest(string msg)
        {
            if (getMessageFormatType(msg) == "xml")
            {
                parseXML(msg);
            }
            else if (getMessageFormatType(msg) == "json")
            {
                parseJSON(msg);
            }
        }

        private void parseXML(string strXML)
        {
            try
            {
                FindOP(strXML);
                FindTo(strXML);
                FindFr(strXML);
                FindEvt(strXML);
                FindRequestID(strXML);
                FindSubscriptionPath(strXML);
                FindResourceName(strXML);
                FindResourceType(strXML);
            }
            catch
            {
                Debug.WriteLine("Not match xml format！");
            }
        }

        private void FindOP(string str)
        {
            int index_s = -1;
            int index_e = -1;

            index_s = str.IndexOf("<op>");
            index_e = str.IndexOf("</op>");

            if (index_s > 0 && index_e > 0)
            {
                this.Op = str.Substring(index_s + 4, index_e - index_s - 4);
                return;
            }
        }

        private void FindEvt(string str)
        {
            int index_s = -1;
            int index_e = -1;

            index_s = str.IndexOf("<net>");
            index_e = str.IndexOf("</net>");

            if (index_s > 0 && index_e > 0)
            {
                this.Evt = str.Substring(index_s + 5, index_e - index_s - 5);
                return;
            }
        }

        private void FindTo(string str)
        {
            int index_s = -1;
            int index_e = -1;

            index_s = str.IndexOf("<to>");
            index_e = str.IndexOf("</to>");

            if (index_s > 0 && index_e > 0)
            {
                this.To = str.Substring(index_s + 4, index_e - index_s - 4);
                return;
            }
        }

        private void FindFr(string str)
        {
            int index_s = -1;
            int index_e = -1;

            index_s = str.IndexOf("<fr>");
            index_e = str.IndexOf("</fr>");

            if (index_s > 0 && index_e > 0)
            {
                this.Fr = str.Substring(index_s + 4, index_e - index_s - 4);
                return;
            }
        }

        private void FindRequestID(string str)
        {
            int index_s = -1;
            int index_e = -1;

            index_s = str.IndexOf("<rqi>");
            index_e = str.IndexOf("</rqi>");

            if (index_s > 0 && index_e > 0)
            {
                this.RequestID = str.Substring(index_s + 5, index_e - index_s - 5);
                return;
            }
        }

        private void FindSubscriptionPath(string str)
        {
            int index_s = -1;
            int index_e = -1;

            index_s = str.IndexOf("<sur>");
            index_e = str.IndexOf("</sur>");

            if (index_s > 0 && index_e > 0)
            {
                this.SubscriptionPath = str.Substring(index_s + 5, index_e - index_s - 5);
                return;
            }
        }

        private void FindResourceName(string str)
        {
            int index_s = -1;
            int index_e = -1;

            index_s = str.IndexOf("<rn>");
            index_e = str.IndexOf("</rn>");

            if (index_s > 0 && index_e > 0)
            {
                this.ResourceName = str.Substring(index_s + 4, index_e - index_s - 4);
                return;
            }

            index_s = str.IndexOf(" rn=\"");

            if (index_s > 0)
            {
                for (int i = index_s; i < str.Length - 1; i++)
                {
                    if (str[i] == '\"' && str[i + 1] == '>')
                    {
                        index_e = i;
                        break;
                    }
                }

                if (index_s > 0 && index_e > 0)
                {
                    this.ResourceName = str.Substring(index_s + 5, index_e - index_s - 5);
                    return;
                }
            }

            index_s = str.IndexOf(" rn=\'");

            if (index_s > 0)
            {
                for (int i = index_s; i < str.Length - 1; i++)
                {
                    if (str[i] == '\'' && str[i + 1] == '>')
                    {
                        index_e = i;
                        break;
                    }
                }

                if (index_s > 0 && index_e > 0)
                {
                    this.ResourceName = str.Substring(index_s + 5, index_e - index_s - 5);
                    return;
                }
            }
        }

        private void FindResourceType(string str)
        {
            int index_s = -1;
            int index_e = -1;

            index_s = str.IndexOf("<ty>");
            index_e = str.IndexOf("</ty>");

            if (index_s > 0 && index_e > 0)
            {
                this.ResourceType = str.Substring(index_s + 4, index_e - index_s - 4);
                return;
            }
        }

        private void parseJSON(string strJson)
        {
            try
            {
                var temp = strJson.Remove(strJson.IndexOf(':'), 1);

                var node = JsonConvert.DeserializeXNode(temp, "root");

                var strXML = node.ToString();

                parseXML(strXML);
            }
            catch
            {

            }
        }

        private string getMessageFormatType(string strMsg)
        {
            string trimStr = strMsg.Trim();
            if ((trimStr.StartsWith(@"{") && trimStr.EndsWith(@"}"))
                || (trimStr.StartsWith(@"[") && trimStr.EndsWith(@"]")))
            {
                return "json";
            }
            else if (trimStr.StartsWith(@"<") && trimStr.EndsWith(@">"))
            {
                return "xml";
            }
            else return "unknown";
        }
    }

    class MqttNotificationResponse
    {
        public string ResponseCode { get; set; }
        public string RequestID { get; set; }
        public string To { get; set; }
        public string Fr { get; set; }
        public string Pc { get; set; }

        public string ToXMLString()
        {
            string strResult = "";

            var doc = new XmlDocument();
            doc.CreateXmlDeclaration("1.0", "utf-8", "yes");

            var eleRoot = doc.CreateElement("m2m", "rsp", "http://www.onem2m.org/xml/protocols");
            eleRoot.SetAttribute("xmlns:xsi", @"http://www.w3.org/2001/XMLSchema-instance");

            var eleRsc = doc.CreateElement("rsc");
            var eleTo = doc.CreateElement("to");
            var eleFr = doc.CreateElement("fr");
            var eleRi = doc.CreateElement("rqi");
            var elePc = doc.CreateElement("pc");

            eleRsc.InnerText = this.ResponseCode;
            eleTo.InnerText = this.To;
            eleFr.InnerText = this.Fr;
            eleRi.InnerText = this.RequestID;
            elePc.InnerText = this.Pc;

            eleRoot.AppendChild(eleRsc);
            eleRoot.AppendChild(eleTo);
            eleRoot.AppendChild(eleFr);
            eleRoot.AppendChild(eleRi);
            eleRoot.AppendChild(elePc);
            doc.AppendChild(eleRoot);

            strResult = doc.OuterXml;

            return strResult;
        }
    }
}
