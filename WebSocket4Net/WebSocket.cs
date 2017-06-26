using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using SuperSocket.ClientEngine;
using WebSocket4Net.Command;
using WebSocket4Net.Common;
using WebSocket4Net.Protocol;

namespace WebSocket4Net
{
    public partial class WebSocket : IDisposable
    {
        private static readonly ProtocolProcessorFactory _protocolProcessorFactory;

        private EndPoint _remoteEndPoint;
        private EventHandler<ErrorEventArgs> _error;
        private EndPoint _httpConnectProxy;
        private readonly Dictionary<string, ICommand<WebSocket, WebSocketFrame>> _commandDict = new Dictionary<string, ICommand<WebSocket, WebSocketFrame>>(StringComparer.OrdinalIgnoreCase);
        private int _stateCode;

        public const int DefaultReceiveBufferSize = 4096;

        internal TcpClientSession Client { get; private set; }

        /// <summary>
        /// Gets the version of the websocket protocol.
        /// </summary>
        public WebSocketVersion Version { get; private set; }

        /// <summary>
        /// Gets the last active time of the websocket.
        /// </summary>
        public DateTime LastActiveTime { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable auto send ping].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable auto send ping]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableAutoSendPing { get; set; }

        /// <summary>
        /// Gets or sets the interval of ping auto sending, in seconds.
        /// </summary>
        /// <value>
        /// The auto send ping internal.
        /// </value>
        public int AutoSendPingInterval { get; set; }

        protected const string UserAgentKey = "User-Agent";

        internal IProtocolProcessor ProtocolProcessor { get; private set; }

        public bool SupportBinary => ProtocolProcessor.SupportBinary;

        internal Uri TargetUri { get; private set; }

        internal string SubProtocol { get; private set; }

        internal IDictionary<string, object> Items { get; private set; }

        internal List<KeyValuePair<string, string>> Cookies { get; private set; }

        internal List<KeyValuePair<string, string>> CustomHeaderItems { get; private set; }


        internal int StateCode => _stateCode;

        public WebSocketState State => (WebSocketState)_stateCode;

        public bool Handshaked { get; private set; }

        public IProxyConnector Proxy { get; set; }

        internal EndPoint HttpConnectProxy => _httpConnectProxy;

        protected HandshakeReaderBase HandshakeReader { get; private set; }
        protected DataReaderBase DataReader { get; private set; }

        internal bool NotSpecifiedVersion { get; private set; }

        /// <summary>
        /// It is used for ping/pong and closing handshake checking
        /// </summary>
        private Timer _webSocketTimer;

        internal string LastPongResponse { get; set; }

        private string _lastPingRequest;

        private const string _uriScheme = "ws";

        private const string _uriPrefix = _uriScheme + "://";

        private const string _secureUriScheme = "wss";
        private const int _securePort = 443;

        private const string _secureUriPrefix = _secureUriScheme + "://";
        internal string HandshakeHost { get; private set; }

        internal string Origin { get; private set; }

#if !__IOS__
        public bool NoDelay { get; set; }
#endif

#if !SILVERLIGHT
        /// <summary>
        /// set/get the local bind endpoint
        /// </summary>
        public EndPoint LocalEndPoint
        {
            get
            {
                return Client?.LocalEndPoint;
            }

            set
            {
                if (Client == null)
                    throw new Exception("Websocket client is not initilized.");

                Client.LocalEndPoint = value;
            }
        }
#endif

#if !SILVERLIGHT

        private SecurityOption _security;

        /// <summary>
        /// get the websocket's security options
        /// </summary>
        public SecurityOption Security
        {
            get
            {
                if (_security != null)
                    return _security;

                var secureClient = Client as SslStreamTcpSession;

                if (secureClient == null)
                    return _security = new SecurityOption();

                return _security = secureClient.Security;
            }
        }
#endif

        private bool _disposed;

        static WebSocket()
        {
            _protocolProcessorFactory = new ProtocolProcessorFactory(new Rfc6455Processor(), new DraftHybi10Processor(), new DraftHybi00Processor());
        }

        private EndPoint ResolveUri(string uri, int defaultPort, out int port)
        {
            TargetUri = new Uri(uri);

            if (string.IsNullOrEmpty(Origin))
                Origin = TargetUri.GetOrigin();

            IPAddress ipAddress;

            EndPoint remoteEndPoint;

            port = TargetUri.Port;

            if (port <= 0)
                port = defaultPort;

            if (IPAddress.TryParse(TargetUri.Host, out ipAddress))
                remoteEndPoint = new IPEndPoint(ipAddress, port);
            else
                remoteEndPoint = new DnsEndPoint(TargetUri.Host, port);

            return remoteEndPoint;
        }

        TcpClientSession CreateClient(string uri)
        {
            int port;
            _remoteEndPoint = ResolveUri(uri, 80, out port);

            if (port == 80)
                HandshakeHost = TargetUri.Host;
            else
                HandshakeHost = TargetUri.Host + ":" + port;

            return new AsyncTcpSession();
        }


#if !NETFX_CORE

        TcpClientSession CreateSecureClient(string uri)
        {
            int hostPos = uri.IndexOf('/', _secureUriPrefix.Length);

            if (hostPos < 0)//wss://localhost or wss://localhost:xxxx
            {
                hostPos = uri.IndexOf(':', _secureUriPrefix.Length, uri.Length - _secureUriPrefix.Length);

                if (hostPos < 0)
                    uri = uri + ":" + _securePort + "/";
                else
                    uri = uri + "/";
            }
            else if (hostPos == _secureUriPrefix.Length)//wss://
            {
                throw new ArgumentException(@"Invalid uri", nameof(uri));
            }
            else//wss://xxx/xxx
            {
                int colonPos = uri.IndexOf(':', _secureUriPrefix.Length, hostPos - _secureUriPrefix.Length);

                if (colonPos < 0)
                {
                    uri = uri.Substring(0, hostPos) + ":" + _securePort + uri.Substring(hostPos);
                }
            }

            int port;
            _remoteEndPoint = ResolveUri(uri, _securePort, out port);

            if (_httpConnectProxy != null)
            {
                _remoteEndPoint = _httpConnectProxy;
            }

            if (port == _securePort)
                HandshakeHost = TargetUri.Host;
            else
                HandshakeHost = TargetUri.Host + ":" + port;

            return CreateSecureTcpSession();
        }
#endif

        private void Initialize(string uri, string subProtocol, List<KeyValuePair<string, string>> cookies, List<KeyValuePair<string, string>> customHeaderItems, string userAgent, string origin, WebSocketVersion version, EndPoint httpConnectProxy, int receiveBufferSize)
        {
            if (version == WebSocketVersion.None)
            {
                NotSpecifiedVersion = true;
                version = WebSocketVersion.Rfc6455;
            }

            Version = version;
            ProtocolProcessor = GetProtocolProcessor(version);

            Cookies = cookies;

            Origin = origin;

            if (!string.IsNullOrEmpty(userAgent))
            {
                if (customHeaderItems == null)
                    customHeaderItems = new List<KeyValuePair<string, string>>();

                customHeaderItems.Add(new KeyValuePair<string, string>(UserAgentKey, userAgent));
            }

            if (customHeaderItems != null && customHeaderItems.Count > 0)
                CustomHeaderItems = customHeaderItems;

            var handshakeCmd = new Handshake();
            _commandDict.Add(handshakeCmd.Name, handshakeCmd);
            var textCmd = new Text();
            _commandDict.Add(textCmd.Name, textCmd);
            var dataCmd = new Binary();
            _commandDict.Add(dataCmd.Name, dataCmd);
            var closeCmd = new Close();
            _commandDict.Add(closeCmd.Name, closeCmd);
            var pingCmd = new Ping();
            _commandDict.Add(pingCmd.Name, pingCmd);
            var pongCmd = new Pong();
            _commandDict.Add(pongCmd.Name, pongCmd);
            var badRequestCmd = new BadRequest();
            _commandDict.Add(badRequestCmd.Name, badRequestCmd);

            _stateCode = WebSocketStateConst.None;

            SubProtocol = subProtocol;

            Items = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            _httpConnectProxy = httpConnectProxy;



            TcpClientSession client;

            if (uri.StartsWith(_uriPrefix, StringComparison.OrdinalIgnoreCase))
            {
                client = CreateClient(uri);
            }
            else if (uri.StartsWith(_secureUriPrefix, StringComparison.OrdinalIgnoreCase))
            {
#if !NETFX_CORE
                client = CreateSecureClient(uri);
#else
                throw new NotSupportedException("WebSocket4Net still has not supported secure websocket for UWP yet.");
#endif
            }
            else
            {
                throw new ArgumentException(@"Invalid uri", nameof(uri));
            }

            client.ReceiveBufferSize = receiveBufferSize > 0 ? receiveBufferSize : DefaultReceiveBufferSize;
            client.Connected += client_Connected;
            client.Closed += client_Closed;
            client.Error += client_Error;
            client.DataReceived += client_DataReceived;

            Client = client;

            //Ping auto sending is enabled by default
            EnableAutoSendPing = true;
        }

        void client_DataReceived(object sender, DataEventArgs e)
        {
            OnDataReceived(e.Data, e.Offset, e.Length);
        }

        void client_Error(object sender, ErrorEventArgs e)
        {
            OnError(e);

            //Also fire close event if the connection fail to connect
            OnClosed();
        }

        void client_Closed(object sender, EventArgs e)
        {
            OnClosed();
        }

        void client_Connected(object sender, EventArgs e)
        {
            OnConnected();
        }

        internal bool GetAvailableProcessor(int[] availableVersions)
        {
            var processor = _protocolProcessorFactory.GetPreferedProcessorFromAvialable(availableVersions);

            if (processor == null)
                return false;

            ProtocolProcessor = processor;
            return true;
        }

        public int ReceiveBufferSize
        {
            get { return Client.ReceiveBufferSize; }
            set { Client.ReceiveBufferSize = value; }
        }

        public void Open()
        {
            _stateCode = WebSocketStateConst.Connecting;

            if (Proxy != null)
                Client.Proxy = Proxy;

#if !__IOS__
            Client.NoDelay = NoDelay;
#endif

#if SILVERLIGHT
#if !WINDOWS_PHONE
            Client.ClientAccessPolicyProtocol = ClientAccessPolicyProtocol;
#endif
#endif
            Client.Connect(_remoteEndPoint);
        }

        private static IProtocolProcessor GetProtocolProcessor(WebSocketVersion version)
        {
            var processor = _protocolProcessorFactory.GetProcessorByVersion(version);

            if (processor == null)
                throw new ArgumentException("Invalid websocket version");

            return processor;
        }

        void OnConnected()
        {
            HandshakeReader = ProtocolProcessor.CreateHandshakeReader(this);
            DataReader = null;

            if (Items.Count > 0)
                Items.Clear();

            ProtocolProcessor.SendHandshake(this);
        }

        protected internal virtual void OnHandshaked()
        {
            _stateCode = WebSocketStateConst.Open;

            Handshaked = true;

            if (EnableAutoSendPing && ProtocolProcessor.SupportPingPong)
            {
                //Ping auto sending interval's default value is 60 seconds
                if (AutoSendPingInterval <= 0)
                    AutoSendPingInterval = 60;

                _webSocketTimer = new Timer(OnPingTimerCallback, ProtocolProcessor, AutoSendPingInterval * 1000, AutoSendPingInterval * 1000);
            }

            _opened?.Invoke(this, EventArgs.Empty);
        }


        private void OnPingTimerCallback(object state)
        {

            if (!string.IsNullOrEmpty(_lastPingRequest) && !_lastPingRequest.Equals(LastPongResponse))
            {
                // have not got last response
                // Verify that the remote endpoint is still responsive 
                // by sending an un-solicited PONG frame:
                try
                {
                    ((IProtocolProcessor)state).SendPong(this, "");
                }
                catch (Exception e)
                {
                    OnError(e);
                    return;
                }
            }

            _lastPingRequest = DateTime.Now.ToString();

            try
            {
                ((IProtocolProcessor)state).SendPing(this, _lastPingRequest);
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }

        private EventHandler _opened;

        public event EventHandler Opened
        {
            add { _opened += value; }
            remove { _opened -= value; }
        }

        private EventHandler<MessageReceivedEventArgs> _messageReceived;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived
        {
            add { _messageReceived += value; }
            remove { _messageReceived -= value; }
        }

        internal void FireMessageReceived(string message) => _messageReceived?.Invoke(this, new MessageReceivedEventArgs(message));

        private EventHandler<DataReceivedEventArgs> _dataReceived;

        public event EventHandler<DataReceivedEventArgs> DataReceived
        {
            add { _dataReceived += value; }
            remove { _dataReceived -= value; }
        }

        internal void FireDataReceived(byte[] data) => _dataReceived?.Invoke(this, new DataReceivedEventArgs(data));

        private const string _notOpenSendingMessage = "You must send data by websocket after websocket is opened!";

        private bool EnsureWebSocketOpen()
        {
            if (Handshaked) return true;
            OnError(new Exception(_notOpenSendingMessage));
            return false;
        }

        public void Send(string message)
        {
            if (!EnsureWebSocketOpen()) return;
            ProtocolProcessor.SendMessage(this, message);
        }

        public void Send(byte[] data, int offset, int length)
        {
            if (!EnsureWebSocketOpen()) return;
            ProtocolProcessor.SendData(this, data, offset, length);
        }

        public void Send(IList<ArraySegment<byte>> segments)
        {
            if (!EnsureWebSocketOpen()) return;
            ProtocolProcessor.SendData(this, segments);
        }

        private ClosedEventArgs _closedArgs;

        private void OnClosed()
        {
            var fireBaseClose = (_stateCode == WebSocketStateConst.Closing || _stateCode == WebSocketStateConst.Open || _stateCode == WebSocketStateConst.Connecting);

            _stateCode = WebSocketStateConst.Closed;

            if (fireBaseClose)
                FireClosed();
        }

        public void Close() => Close(string.Empty);

        public void Close(string reason) => Close(ProtocolProcessor.CloseStatusCode.NormalClosure, reason);

        public void Close(int statusCode, string reason)
        {
            _closedArgs = new ClosedEventArgs((short)statusCode, reason);

            //The websocket never be opened
            if (Interlocked.CompareExchange(ref _stateCode, WebSocketStateConst.Closed, WebSocketStateConst.None) == WebSocketStateConst.None)
            {
                OnClosed();
                return;
            }

            //The websocket is connecting or in handshake
            if (Interlocked.CompareExchange(ref _stateCode, WebSocketStateConst.Closing, WebSocketStateConst.Connecting) == WebSocketStateConst.Connecting)
            {
                var client = Client;

                if (client != null && client.IsConnected)
                {
                    client.Close();
                    return;
                }

                OnClosed();
                return;
            }

            _stateCode = WebSocketStateConst.Closing;

            //Disable auto ping
            ClearTimer();
            //Set closing hadnshake checking timer
            _webSocketTimer = new Timer(CheckCloseHandshake, null, 5 * 1000, Timeout.Infinite);

            try
            {
                ProtocolProcessor.SendCloseHandshake(this, statusCode, reason);
            }
            catch (Exception e)
            {
                if (Client != null) OnError(e);
            }
        }

        private void CheckCloseHandshake(object state)
        {
            if (_stateCode == WebSocketStateConst.Closed) return;

            try
            {
                CloseWithoutHandshake();
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }

        internal void CloseWithoutHandshake() => Client?.Close();

        protected bool ExecuteCommand(WebSocketFrame frame)
        {
            if (frame == null) return false;
            if (frame.Key == null) return true;
            ICommand<WebSocket, WebSocketFrame> command;
            if (_commandDict.TryGetValue(frame.Key, out command)) command.ExecuteCommand(this, frame);
            return true;
        }


        private void OnDataReceived(byte[] data, int offset, int length)
        {
            var ah = new ArrayHolder<byte>(data);
            while (true)
            {
                int lengthToProcess = 0;
                bool executed = false;
                bool processed = false;

                var handshakeReader = HandshakeReader;
                ArrayView<byte> arrayView = null;

                if (handshakeReader != null)
                {
                    bool success;
                    var handShakeFrame = handshakeReader.BuildHandShakeFrame(data, offset, length, out lengthToProcess, out success);
                    arrayView = handshakeReader.ArrayView;
                    executed = ExecuteCommand(handShakeFrame);

                    if (success)
                    {
                        HandshakeReader = null;
                        DataReader = ProtocolProcessor.CreateDataReader(this, null);
                        if (lengthToProcess <= 0) break;

                        offset = offset + length - lengthToProcess;
                        length = lengthToProcess;
                    }
                }

                var dataReader = DataReader;
                if (dataReader != null)
                {
                    var frame = dataReader.TryBuildWebSocketFrame(ah, offset, length);
                    arrayView = dataReader.ArrayView;
                    processed = frame;
                    executed = ExecuteCommand(frame);
                    lengthToProcess = frame;
                }


                if (lengthToProcess <= 0 && executed) break;

                if (lengthToProcess <= 0)
                {
                    ah.CopyBuffer();
                    break;
                }

                if (lengthToProcess > 0 && !processed)
                {
                    arrayView?.TrimEnd(lengthToProcess);
                }

                offset = offset + length - lengthToProcess;
                length = lengthToProcess;
            }
        }

        internal void FireError(Exception error) => OnError(error);

        private EventHandler _closed;

        public event EventHandler Closed
        {
            add { _closed += value; }
            remove { _closed -= value; }
        }

        private void ClearTimer()
        {
            var timer = _webSocketTimer;

            if (timer == null) return;

            lock (this)
            {
                if (_webSocketTimer == null) return;

                timer.Change(Timeout.Infinite, Timeout.Infinite);
                timer.Dispose();

                _webSocketTimer = null;
            }
        }

        private void FireClosed()
        {
            ClearTimer();
            _closed?.Invoke(this, _closedArgs ?? EventArgs.Empty);
        }

        public event EventHandler<ErrorEventArgs> Error
        {
            add { _error += value; }
            remove { _error -= value; }
        }

        private void OnError(ErrorEventArgs e) => _error?.Invoke(this, e);

        private void OnError(Exception e) => OnError(new ErrorEventArgs(e));

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                var client = Client;

                if (client != null)
                {
                    client.Connected -= client_Connected;
                    client.Closed -= client_Closed;
                    client.Error -= client_Error;
                    client.DataReceived -= client_DataReceived;

                    if (client.IsConnected) client.Close();

                    Client = null;
                }

                ClearTimer();
            }

            _disposed = true;
        }

        ~WebSocket()
        {
            Dispose(false);
        }
    }
}
