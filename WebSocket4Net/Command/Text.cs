using System;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ClientEngine;

namespace WebSocket4Net.Command
{
    public class Text : WebSocketCommandBase
    {
        public override void ExecuteCommand(WebSocket session, WebSocketFrame frame)
        {
            session.FireMessageReceived(frame.Text);
        }

        public override string Name
        {
            get { return OpCode.Text.ToString(); }
        }
    }
}
