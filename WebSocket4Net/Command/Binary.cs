using System;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ClientEngine;

namespace WebSocket4Net.Command
{
    public class Binary : WebSocketCommandBase
    {
        public override void ExecuteCommand(WebSocket session, WebSocketFrame frame)
        {
            session.FireDataReceived(frame.Data);
        }

        public override string Name
        {
            get { return OpCode.Binary.ToString(); }
        }
    }
}
