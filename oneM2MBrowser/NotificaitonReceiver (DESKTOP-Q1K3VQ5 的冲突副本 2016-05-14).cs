using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using uhttpsharp;
using uhttpsharp.Headers;
using uhttpsharp.Listeners;
using uhttpsharp.RequestProviders;
using uPLibrary.Networking.M2Mqtt;

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

                        list_headers.Add(new KeyValuePair<string, string>("X-M2M-RSC", "2000"));
                        list_headers.Add(new KeyValuePair<string, string>("X-M2M-RI", msg_id));

                        var response_headers = new ListHttpHeaders(list_headers);

                        context.Response = uhttpsharp.StringHttpResponse.Create("", HttpResponseCode.Ok, "text/html", true, response_headers);
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

        public NotifiacitonMqttServer(string broker)
        {
            this.broker_ip = broker;
            this.clientId = RandomString(20);
        }

        public void SubscripTopic(string topic)
        {
            this.client.Subscribe(new string[] { topic }, new byte[] { 0 });
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

                client.MqttMsgPublishReceived += (s, e) =>
                {
                    var strMsg = Encoding.UTF8.GetString(e.Message);
                    var topic = e.Topic;

                    string[] path_array = topic.Split('/');

                    if (path_array.Length == 6)
                    {
                        var obj = new MqttNotificationRequest(strMsg);

                        var id = path_array[3];
                        var ae_id = path_array[4];
                        var res_path = obj.SubscriptionPath;

                        string[] path_arr = res_path.Split('/');

                        if (path_arr.Length > 0)
                        {
                            var noti_name = path_arr[path_arr.Length - 1];
                            var noti_parent = res_path.Remove(res_path.LastIndexOf("/"), noti_name.Length + 1);

                            //call handler
                            Debug.WriteLine("[" + res_path + "] receive a message");
                            if(this.handler != null)
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
                };

                client.MqttMsgSubscribed += (s, e) =>
                {
                    Debug.WriteLine("[" + e.MessageId + "] scripted successfully!");
                };

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
            try
            {
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
                FindResourceName(noti_xml);
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

            index_s = str.IndexOf("<subscriptionReference>");
            index_e = str.IndexOf("</subscriptionReference>");

            if (index_s > 0 && index_e > 0)
            {
                this.Path = str.Substring(index_s + 23, index_e - index_s - 23);
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

            index_s = str.IndexOf("<content>");
            index_e = str.IndexOf("</content>");

            if (index_s > 0 && index_e > 0)
            {
                this.Content = str.Substring(index_s + 9, index_e - 9);
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
                this.ResourceName = str.Substring(index_s + 4, index_e - 4);
                return;
            }

            index_s = str.IndexOf("<resourceName>");
            index_e = str.IndexOf("</resourceName>");

            if (index_s > 0 && index_e > 0)
            {
                this.ResourceName = str.Substring(index_s + 14, index_e - 14);
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

            index_s = str.IndexOf("<resourceType>");
            index_e = str.IndexOf("</resourceType>");

            if (index_s > 0 && index_e > 0)
            {
                this.ResourceType = getResourceTypeName(str.Substring(index_s + 14, index_e - 14));
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
                case "23": strResult = "Subscription"; break;
                case "24": strResult = "SemanticDescription"; break;
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

            index_s = str.IndexOf("<notificationEventType>");
            index_e = str.IndexOf("</notificationEventType>");

            if (index_s > 0 && index_e > 0)
            {
                this.Evt = str.Substring(index_s + 11, index_e - index_s - 11);
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

            index_s = str.IndexOf("<subscriptionReference>");
            index_e = str.IndexOf("</subscriptionReference>");

            if (index_s > 0 && index_e > 0)
            {
                this.SubscriptionPath = str.Substring(index_s + 23, index_e - index_s - 23);
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

            index_s = str.IndexOf("<resourceName>");
            index_e = str.IndexOf("</resourceName>");

            if (index_s > 0 && index_e > 0)
            {
                this.ResourceName = str.Substring(index_s + 14, index_e - index_s - 14);
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
                this.ResourceType = str.Substring(index_s + 4, index_e - index_s - 4);
                return;
            }

            index_s = str.IndexOf("<resourceType>");
            index_e = str.IndexOf("</resourceType>");

            if (index_s > 0 && index_e > 0)
            {
                this.ResourceType = str.Substring(index_s + 14, index_e - index_s - 14);
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
