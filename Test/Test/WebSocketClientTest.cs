﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SuperSocket.Common;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase.Logging;
using SuperSocket.SocketEngine;
using SuperWebSocket;
using SuperWebSocket.SubProtocol;
using WebSocket4Net;

namespace WebSocket4Net.Test
{
    [TestFixture]
    public class SecureWebSocketClientTestHybi00 : WebSocketClientTest
    {
        public SecureWebSocketClientTestHybi00()
            : base(WebSocketVersion.DraftHybi00, "Tls", "supersocket.pfx", "supersocket")
        {

        }

        protected override string Host
        {
            get { return "wss://127.0.0.1"; }
        }
    }

    [TestFixture]
    public class SecureWebSocketClientTestHybi10 : WebSocketClientTest
    {
        public SecureWebSocketClientTestHybi10()
            : base(WebSocketVersion.DraftHybi10, "Tls", "supersocket.pfx", "supersocket")
        {

        }

        protected override string Host => "wss://127.0.0.1";

        [Test]
        public void TestWebSocketOrg()
        {
            WebSocket webSocketClient = new WebSocket("wss://echo.websocket.org", httpConnectProxy: new IPEndPoint(new IPAddress(new byte[] { 10, 96, 30, 38 }), 8080),sslProtocols: SslProtocols.Tls, version: WebSocketVersion.Rfc6455);
            webSocketClient.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(webSocketClient_Error);

            ConfigSecurity(webSocketClient);

            webSocketClient.Closed += new EventHandler(webSocketClient_Closed);
            webSocketClient.MessageReceived += new EventHandler<MessageReceivedEventArgs>(webSocketClient_MessageReceived);
            webSocketClient.Open();

            if (!m_OpenedEvent.WaitOne(5000 * 2))
                Assert.Fail("Failed to Opened session ontime");

            Assert.AreEqual(WebSocketState.Open, webSocketClient.State);

            for (var i = 0; i < 10; i++)
            {
                var message = Guid.NewGuid().ToString();

                webSocketClient.Send(message);

                if (!m_MessageReceiveEvent.WaitOne(5000))
                {
                    Assert.Fail("Failed to get echo messsage on time");
                    break;
                }

                Console.WriteLine("Received echo message: {0}", m_CurrentMessage);
                Assert.AreEqual(m_CurrentMessage, message);
            }

            webSocketClient.Close();

            if (!m_CloseEvent.WaitOne(5000))
                Assert.Fail("Failed to close session ontime");

            Assert.AreEqual(WebSocketState.Closed, webSocketClient.State);
        }

    }

    [TestFixture]
    public class WebSocketClientTestHybi00 : WebSocketClientTest
    {
        public WebSocketClientTestHybi00()
            : base(WebSocketVersion.DraftHybi00)
        {

        }
    }

    [TestFixture]
    public class WebSocketClientTestHybi10 : WebSocketClientTest
    {
        public WebSocketClientTestHybi10()
            : base(WebSocketVersion.DraftHybi10)
        {

        }

        [Test]
        public void TestWebSocketOrg()
        {
            WebSocket webSocketClient = new WebSocket("ws://echo.websocket.org", httpConnectProxy: new IPEndPoint(new IPAddress(new byte[] { 10, 96, 30, 38 }), 8080));
            webSocketClient.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(webSocketClient_Error);

            ConfigSecurity(webSocketClient);

            webSocketClient.Closed += new EventHandler(webSocketClient_Closed);
            webSocketClient.MessageReceived += new EventHandler<MessageReceivedEventArgs>(webSocketClient_MessageReceived);
            webSocketClient.Open();

            if (!m_OpenedEvent.WaitOne(5000 * 2))
                Assert.Fail("Failed to Opened session ontime");

            Assert.AreEqual(WebSocketState.Open, webSocketClient.State);

            for (var i = 0; i < 10; i++)
            {
                var message = Guid.NewGuid().ToString();

                webSocketClient.Send(message);

                if (!m_MessageReceiveEvent.WaitOne(5000))
                {
                    Assert.Fail("Failed to get echo messsage on time");
                    break;
                }

                Console.WriteLine("Received echo message: {0}", m_CurrentMessage);
                Assert.AreEqual(m_CurrentMessage, message);
            }

            webSocketClient.Close();

            if (!m_CloseEvent.WaitOne(5000))
                Assert.Fail("Failed to close session ontime");

            Assert.AreEqual(WebSocketState.Closed, webSocketClient.State);
        }
    }

    public abstract class WebSocketClientTest
    {
        protected WebSocketServer m_WebSocketServer;
        protected AutoResetEvent m_MessageReceiveEvent = new AutoResetEvent(false);
        protected AutoResetEvent m_OpenedEvent = new AutoResetEvent(false);
        protected AutoResetEvent m_CloseEvent = new AutoResetEvent(false);
        protected string m_CurrentMessage = string.Empty;

        private WebSocketVersion m_Version = WebSocketVersion.DraftHybi00;

        private string m_Security;
        private string m_CertificateFile;
        private string m_Password;

        protected virtual string Host
        {
            get { return "ws://127.0.0.1"; }
        }

        protected WebSocketClientTest(WebSocketVersion version)
            : this(version, string.Empty, string.Empty, string.Empty)
        {

        }

        protected WebSocketClientTest(WebSocketVersion version, string security, string certificateFile, string password)
        {
            m_Version = version;
            m_Security = security;
            m_CertificateFile = certificateFile;
            m_Password = password;
        }

        [TestFixtureSetUp]
        public virtual void Setup()
        {
            m_WebSocketServer = new WebSocketServer(new BasicSubProtocol("Basic", new List<Assembly> { this.GetType().Assembly }));
            m_WebSocketServer.NewDataReceived += new SessionHandler<WebSocketSession, byte[]>(m_WebSocketServer_NewDataReceived);
            m_WebSocketServer.Setup(new ServerConfig
            {
                Port = 2012,
                Ip = "Any",
                MaxConnectionNumber = 100,
                Mode = SocketMode.Tcp,
                Name = "SuperWebSocket Server",
                Security = m_Security,
                LogAllSocketException = true,
                Certificate = new CertificateConfig { FilePath = m_CertificateFile, Password = m_Password }
            }, logFactory: new ConsoleLogFactory());
        }

        void m_WebSocketServer_NewDataReceived(WebSocketSession session, byte[] e)
        {
            //Echo
            session.Send(new ArraySegment<byte>(e, 0, e.Length));
        }

        [SetUp]
        public void StartServer()
        {
            m_WebSocketServer.Start();
        }

        [TearDown]
        public void StopServer()
        {
            m_WebSocketServer.Stop();
        }

        protected void ConfigSecurity(WebSocket websocket)
        {
            var security = websocket.Security;

            if (security != null)
            {
                security.AllowUnstrustedCertificate = true;
                security.AllowNameMismatchCertificate = true;
            }
        }

        [Test, Repeat(5)]
        public void ConnectionTest()
        {
            WebSocket webSocketClient = new WebSocket(string.Format("{0}:{1}/websocket", Host, m_WebSocketServer.Config.Port), "basic", m_Version);
            webSocketClient.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(webSocketClient_Error);

            ConfigSecurity(webSocketClient);

            webSocketClient.Opened += new EventHandler(webSocketClient_Opened);
            webSocketClient.Closed += new EventHandler(webSocketClient_Closed);
            webSocketClient.MessageReceived += new EventHandler<MessageReceivedEventArgs>(webSocketClient_MessageReceived);
            webSocketClient.Open();

            if (!m_OpenedEvent.WaitOne(2000))
                Assert.Fail("Failed to Opened session ontime");

            Assert.AreEqual(WebSocketState.Open, webSocketClient.State);

            webSocketClient.Close();

            if (!m_CloseEvent.WaitOne(1000))
                Assert.Fail("Failed to close session ontime");

            Assert.AreEqual(WebSocketState.Closed, webSocketClient.State);
        }

        [Test, Repeat(5)]
        public void IncorrectDNSTest()
        {
            WebSocket webSocketClient = new WebSocket(string.Format("{0}:{1}/websocket", "ws://localhostx", m_WebSocketServer.Config.Port), "basic", m_Version);
            webSocketClient.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(webSocketClient_Error);

            ConfigSecurity(webSocketClient);

            webSocketClient.Opened += new EventHandler(webSocketClient_Opened);
            webSocketClient.Closed += new EventHandler(webSocketClient_Closed);
            webSocketClient.MessageReceived += new EventHandler<MessageReceivedEventArgs>(webSocketClient_MessageReceived);
            webSocketClient.Open();

            if (!m_CloseEvent.WaitOne(1000 * 20))
                Assert.Fail("Failed to wait session closed ontime");

            Assert.AreEqual(WebSocketState.Closed, webSocketClient.State);

        }

        [Test]
        public void ReconnectTest()
        {
            WebSocket webSocketClient = new WebSocket(string.Format("{0}:{1}/websocket", Host, m_WebSocketServer.Config.Port), "basic", m_Version);
            webSocketClient.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(webSocketClient_Error);

            ConfigSecurity(webSocketClient);

            webSocketClient.Opened += new EventHandler(webSocketClient_Opened);
            webSocketClient.Closed += new EventHandler(webSocketClient_Closed);
            webSocketClient.MessageReceived += new EventHandler<MessageReceivedEventArgs>(webSocketClient_MessageReceived);

            for (var i = 0; i < 2000; i++)
            {
                webSocketClient.Open();

                if (!m_OpenedEvent.WaitOne(5000))
                    Assert.Fail("Failed to Opened session ontime at round {0}", i);

                Assert.AreEqual(WebSocketState.Open, webSocketClient.State);

                webSocketClient.Close();

                if (!m_CloseEvent.WaitOne(5000))
                    Assert.Fail("Failed to close session ontime");

                Assert.AreEqual(WebSocketState.Closed, webSocketClient.State);
            }
        }

        [Test]
        public void UnreachableReconnectTestA()
        {
            WebSocket webSocketClient = new WebSocket(string.Format("{0}:{1}/websocket", Host, m_WebSocketServer.Config.Port), "basic", m_Version);
            webSocketClient.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(webSocketClient_Error);
            webSocketClient.Error += (s, e) => { m_OpenedEvent.Set(); };

            ConfigSecurity(webSocketClient);

            webSocketClient.Opened += new EventHandler(webSocketClient_Opened);
            webSocketClient.Closed += new EventHandler(webSocketClient_Closed);
            webSocketClient.MessageReceived += new EventHandler<MessageReceivedEventArgs>(webSocketClient_MessageReceived);

            webSocketClient.Open();

            if (!m_OpenedEvent.WaitOne(5000))
                Assert.Fail("Failed to Opened session ontime");

            Assert.AreEqual(WebSocketState.Open, webSocketClient.State);

            webSocketClient.Close();

            if (!m_CloseEvent.WaitOne(2000))
                Assert.Fail("Failed to close session ontime");

            Assert.AreEqual(WebSocketState.Closed, webSocketClient.State);

            StopServer();

            webSocketClient.Open();

            m_OpenedEvent.WaitOne();

            Assert.AreEqual(WebSocketState.Closed, webSocketClient.State);

            StartServer();

            webSocketClient.Open();

            if (!m_OpenedEvent.WaitOne(5000))
                Assert.Fail("Failed to Opened session ontime");

            Assert.AreEqual(WebSocketState.Open, webSocketClient.State);
        }

        [Test]
        public void UnreachableReconnectTestB()
        {
            StopServer();

            WebSocket webSocketClient = new WebSocket(string.Format("{0}:{1}/websocket", Host, m_WebSocketServer.Config.Port), "basic", m_Version);
            webSocketClient.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(webSocketClient_Error);
            webSocketClient.Error += (s, e) => { m_OpenedEvent.Set(); };

            ConfigSecurity(webSocketClient);

            webSocketClient.Opened += new EventHandler(webSocketClient_Opened);
            webSocketClient.Closed += new EventHandler(webSocketClient_Closed);
            webSocketClient.MessageReceived += new EventHandler<MessageReceivedEventArgs>(webSocketClient_MessageReceived);

            webSocketClient.Open();
            m_OpenedEvent.WaitOne();
            Assert.AreEqual(WebSocketState.Closed, webSocketClient.State);

            StartServer();

            webSocketClient.Open();
            if (!m_OpenedEvent.WaitOne(5000))
                Assert.Fail("Failed to Opened session ontime");

            Assert.AreEqual(WebSocketState.Open, webSocketClient.State);

            m_CloseEvent.Reset();

            webSocketClient.Close();

            if (!m_CloseEvent.WaitOne(2000))
                Assert.Fail("Failed to close session ontime");

            Console.WriteLine("State {0}: {1}", webSocketClient.GetHashCode(), webSocketClient.State);
            Assert.AreEqual(WebSocketState.Closed, webSocketClient.State);
        }

        //[Test]
        //public void RepeatFaildConnectTest()
        //{
        //    StopServer();

        //    int triedTimes = 0;

        //    try
        //    {
        //        for (triedTimes = 0; triedTimes < 30000; triedTimes++)
        //        {
        //            WebSocket webSocketClient = new WebSocket(string.Format("{0}:{1}/websocket", Host, 9999), "basic", m_Version);
        //            webSocketClient.Open();
        //            webSocketClient.Close();
        //        }
        //    }
        //    catch (TargetInvocationException targetException)
        //    {
        //        Assert.Fail("Exception throw when try " + triedTimes + " times, " + targetException.Message + Environment.NewLine + targetException.StackTrace);
        //    }            
        //}

        protected void webSocketClient_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Console.WriteLine(e.Exception.GetType() + ":" + e.Exception.Message + Environment.NewLine + e.Exception.StackTrace);

            if (e.Exception.InnerException != null)
            {
                Console.WriteLine(e.Exception.InnerException.GetType());
            }
        }

        [Test, Repeat(10)]
        public void CloseWebSocketTest()
        {
            WebSocket webSocketClient = new WebSocket(string.Format("{0}:{1}/websocket", Host, m_WebSocketServer.Config.Port), "basic", m_Version);

            ConfigSecurity(webSocketClient);

            webSocketClient.Opened += new EventHandler(webSocketClient_Opened);
            webSocketClient.Closed += new EventHandler(webSocketClient_Closed);
            webSocketClient.MessageReceived += new EventHandler<MessageReceivedEventArgs>(webSocketClient_MessageReceived);
            webSocketClient.Open();

            if (!m_OpenedEvent.WaitOne(2000))
                Assert.Fail("Failed to Opened session ontime");

            Assert.AreEqual(WebSocketState.Open, webSocketClient.State);

            webSocketClient.Send("QUIT");

            if (!m_CloseEvent.WaitOne(1000))
                Assert.Fail("Failed to close session ontime");

            Assert.AreEqual(WebSocketState.Closed, webSocketClient.State);
        }

        protected void webSocketClient_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            m_CurrentMessage = e.Message;
            m_MessageReceiveEvent.Set();
        }

        protected void webSocketClient_Closed(object sender, EventArgs e)
        {
            m_CloseEvent.Set();
        }

        protected void webSocketClient_Opened(object sender, EventArgs e)
        {
            m_OpenedEvent.Set();
        }

        [Test, Repeat(10)]
        public void SendMessageTest()
        {
            WebSocket webSocketClient = new WebSocket(string.Format("{0}:{1}/websocket", Host, m_WebSocketServer.Config.Port), "basic", m_Version);

            ConfigSecurity(webSocketClient);

            webSocketClient.Opened += new EventHandler(webSocketClient_Opened);
            webSocketClient.Closed += new EventHandler(webSocketClient_Closed);
            webSocketClient.MessageReceived += new EventHandler<MessageReceivedEventArgs>(webSocketClient_MessageReceived);
            webSocketClient.Open();

            if (!m_OpenedEvent.WaitOne(2000))
                Assert.Fail("Failed to Opened session ontime");

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < 10; i++)
            {
                sb.Append(Guid.NewGuid().ToString());
            }

            string messageSource = sb.ToString();

            Random rd = new Random();

            for (int i = 0; i < 100; i++)
            {
                int startPos = rd.Next(0, messageSource.Length - 2);
                int endPos = rd.Next(startPos + 1, messageSource.Length - 1);

                string message = messageSource.Substring(startPos, endPos - startPos);

                Console.WriteLine("Client:" + message);
                webSocketClient.Send("ECHO " + message);

                if (!m_MessageReceiveEvent.WaitOne(1000))
                    Assert.Fail("Cannot get response in time!");

                Assert.AreEqual(message, m_CurrentMessage);
            }

            webSocketClient.Close();

            if (!m_CloseEvent.WaitOne(1000))
                Assert.Fail("Failed to close session ontime");
        }

        [Test, Repeat(10)]
        public void SendDataTest()
        {
            WebSocket webSocketClient = new WebSocket(string.Format("{0}:{1}/websocket", Host, m_WebSocketServer.Config.Port), "basic", m_Version);

            if (!webSocketClient.SupportBinary)
                return;

            ConfigSecurity(webSocketClient);

            webSocketClient.Opened += new EventHandler(webSocketClient_Opened);
            webSocketClient.Closed += new EventHandler(webSocketClient_Closed);
            webSocketClient.DataReceived += new EventHandler<DataReceivedEventArgs>(webSocketClient_DataReceived);
            webSocketClient.Open();

            if (!m_OpenedEvent.WaitOne(2000))
                Assert.Fail("Failed to Opened session ontime");

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < 10; i++)
            {
                sb.Append(Guid.NewGuid().ToString());
            }

            string messageSource = sb.ToString();

            Random rd = new Random();

            for (int i = 0; i < 100; i++)
            {
                int startPos = rd.Next(0, messageSource.Length - 2);
                int endPos = rd.Next(startPos + 1, messageSource.Length - 1);

                string message = messageSource.Substring(startPos, endPos - startPos);

                Console.WriteLine("Client:" + message);
                var data = Encoding.UTF8.GetBytes(message);
                webSocketClient.Send(data, 0, data.Length);

                if (!m_MessageReceiveEvent.WaitOne(1000))
                    Assert.Fail("Cannot get response in time!");

                Assert.AreEqual(message, m_CurrentMessage);
            }

            webSocketClient.Close();

            if (!m_CloseEvent.WaitOne(1000))
                Assert.Fail("Failed to close session ontime");
        }

        [Test]
        public void SendPingReactTest()
        {
            WebSocket webSocketClient = new WebSocket(string.Format("{0}:{1}/websocket", Host, m_WebSocketServer.Config.Port), "basic", m_Version);

            ConfigSecurity(webSocketClient);

            webSocketClient.Opened += new EventHandler(webSocketClient_Opened);
            webSocketClient.Closed += new EventHandler(webSocketClient_Closed);
            webSocketClient.MessageReceived += new EventHandler<MessageReceivedEventArgs>(webSocketClient_MessageReceived);
            webSocketClient.Open();

            if (!m_OpenedEvent.WaitOne(2000))
                Assert.Fail("Failed to Opened session ontime");

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < 10; i++)
            {
                sb.Append(Guid.NewGuid().ToString());
            }

            string messageSource = sb.ToString();

            Random rd = new Random();

            for (int i = 0; i < 10; i++)
            {
                int startPos = rd.Next(0, messageSource.Length - 2);
                int endPos = rd.Next(startPos + 1, messageSource.Length - 1);

                string message = messageSource.Substring(startPos, endPos - startPos);

                Console.WriteLine("PING:" + message);
                webSocketClient.Send("PING " + message);
            }

            Thread.Sleep(5000);

            webSocketClient.Close();

            if (!m_CloseEvent.WaitOne(1000))
                Assert.Fail("Failed to close session ontime");
        }


        [Test]
        public void ConcurrentSendTest()
        {
            WebSocket webSocketClient = new WebSocket(string.Format("{0}:{1}/websocket", Host, m_WebSocketServer.Config.Port), "basic", m_Version);

            ConfigSecurity(webSocketClient);

            webSocketClient.Opened += new EventHandler(webSocketClient_Opened);
            webSocketClient.Closed += new EventHandler(webSocketClient_Closed);

            webSocketClient.Open();

            if (!m_OpenedEvent.WaitOne(2000))
                Assert.Fail("Failed to Opened session ontime");

            string[] lines = new string[100];

            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = Guid.NewGuid().ToString();
            }

            var messDict = lines.ToDictionary(l => l);

            webSocketClient.MessageReceived += (s, m) =>
            {
                messDict.Remove(m.Message);
                Console.WriteLine("R: {0}", m.Message);
            };

            Parallel.For(0, lines.Length, (i) =>
                {
                    webSocketClient.Send("ECHO " + lines[i]);
                });

            int waitRound = 0;

            while (waitRound < 10)
            {
                if (messDict.Count <= 0)
                    break;

                Thread.Sleep(500);
                waitRound++;
            }

            if (messDict.Count > 0)
            {
                Assert.Fail("Failed to receive message on time.");
            }

            webSocketClient.Close();

            if (!m_CloseEvent.WaitOne(1000))
                Assert.Fail("Failed to close session ontime");
        }


        protected void webSocketClient_DataReceived(object sender, DataReceivedEventArgs e)
        {
            m_CurrentMessage = Encoding.UTF8.GetString(e.Data);
            m_MessageReceiveEvent.Set();
        }
    }
}
