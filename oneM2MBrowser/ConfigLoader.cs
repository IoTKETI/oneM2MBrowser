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
using System.IO;
using System.Text;
using System.Xml;

namespace MobiusResourceMonitor_sub
{
    public class ConfigLoader
    {
        private static string confPath = "conf.xml";
        private static XmlDocument doc = null;

        public string ResourcePath = "http://203.253.128.161:7579/mobius-yt/your-ae";
        public string NotificationProtocol = "MQTT";
        public string NotificationIP = "127.0.0.1";
        public string AppName = "";

        public ConfigLoader()
        {
            FileInfo file = new FileInfo(confPath);
            if (!file.Exists)
            {
                string xml = @"<?xml version='1.0' encoding='UTF-8' standalone='yes'?>";
                xml += @"<configuration>";
                xml += @"<appname>" + "rtvt-" + RandomString(10) + "</appname>";
                xml += @"<resource>" + ResourcePath + "</resource>";
                xml += @"<notification>";
                xml += @"<protocol>" + NotificationProtocol + "</protocol>";
                xml += @"<ip>" + NotificationIP + "</ip>";
                xml += @"</notification>";
                xml += @"</configuration>";

                FileStream fs = new FileStream(confPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(xml);
                sw.Flush();
                sw.Close();
            }
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

        public void initConf()
        {
            GetResourcePath();
            GetNotificationProtocol();
            GetNotificationIP();
            GetAppName();
        }


        public void SetResourcePath(string uri)
        {
            doc = new XmlDocument();
            doc.Load(confPath);

            doc.SelectSingleNode("configuration/resource").InnerText = uri;

            doc.Save(confPath);
        }

        public void SetNotificationProtocol(string protocol)
        {
            doc = new XmlDocument();
            doc.Load(confPath);

            doc.SelectSingleNode("configuration/notification/protocol").InnerText = protocol;

            doc.Save(confPath);
        }

        public string GetResourcePath()
        {
            doc = new XmlDocument();
            doc.Load(confPath);

            ResourcePath = doc.SelectSingleNode("configuration/resource").InnerText;

            return ResourcePath;
        }

        public string GetNotificationProtocol()
        {
            doc = new XmlDocument();
            doc.Load(confPath);

            NotificationProtocol = doc.SelectSingleNode("configuration/notification/protocol").InnerText;

            return NotificationProtocol;
        }

        public void SetNotificationIP(string ip)
        {
            doc = new XmlDocument();
            doc.Load(confPath);

            doc.SelectSingleNode("configuration/notification/ip").InnerText = ip;

            doc.Save(confPath);
        }

        public string GetNotificationIP()
        {
            doc = new XmlDocument();
            doc.Load(confPath);

            NotificationIP = doc.SelectSingleNode("configuration/notification/ip").InnerText;

            return NotificationIP;
        }

        public string GetAppName()
        {
            doc = new XmlDocument();
            doc.Load(confPath);

            AppName = doc.SelectSingleNode("configuration/appname").InnerText;

            return AppName;
        }
    }
}
