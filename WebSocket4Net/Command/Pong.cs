using System;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ClientEngine;

namespace WebSocket4Net.Command
{
    public class Pong : WebSocketCommandBase
    {
        public override void ExecuteCommand(WebSocket session, WebSocketFrame frame)
        {
            session.LastActiveTime = DateTime.Now;
            session.LastPongResponse = frame.Text;
        }

        public override string Name
        {
            get { return OpCode.Pong.ToString(); }
        }
    }
}
