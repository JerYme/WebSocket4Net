using System;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ClientEngine;

namespace WebSocket4Net.Common
{
    public delegate void CommandDelegate<in TClientSession, in TFrame>(TClientSession session, TFrame commandInfo);

    class DelegateCommand<TClientSession, TFrame> : ICommand<TClientSession, TFrame>
        where TClientSession : IClientSession
        where TFrame : IWebSocketFrame
    {
        private readonly CommandDelegate<TClientSession, TFrame> _execution;

        public DelegateCommand(string name, CommandDelegate<TClientSession, TFrame> execution)
        {
            Name = name;
            _execution = execution;
        }

        public void ExecuteCommand(TClientSession session, TFrame commandInfo)
        {
            _execution(session, commandInfo);
        }

        public string Name { get; }
    }
}
