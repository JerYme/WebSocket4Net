using System;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ClientEngine;
using WebSocket4Net.Common;

namespace WebSocket4Net.Command
{
    public abstract class WebSocketCommandBase : ICommand<WebSocket, WebSocketFrame>
    {
        public abstract void ExecuteCommand(WebSocket session, WebSocketFrame frame);

        public abstract string Name { get; }
    }
}
