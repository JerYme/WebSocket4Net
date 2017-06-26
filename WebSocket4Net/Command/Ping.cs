using System;
using System.Collections.Generic;
using System.Text;

namespace WebSocket4Net.Command
{
    public class Ping : WebSocketCommandBase
    {
        public override void ExecuteCommand(WebSocket session, WebSocketFrame frame)
        {
            session.LastActiveTime = DateTime.Now;
            session.ProtocolProcessor.SendPong(session, frame.Text);
        }

        public override string Name
        {
            get { return OpCode.Ping.ToString(); }
        }
    }
}
