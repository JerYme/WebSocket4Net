using System;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ClientEngine;
using WebSocket4Net.Common;

namespace WebSocket4Net.Protocol
{
    class DraftHybi00HandshakeReader : HandshakeReader
    {
        //-1 indicate response header has not been received
        private int _receivedChallengeLength = -1;
        private int _expectedChallengeLength = 16;

        private WebSocketFrame _handshakeCommand;
        private readonly byte[] _challenges = new byte[16];


        public DraftHybi00HandshakeReader(WebSocket websocket)
            : base(websocket)
        {
        }

        public override WebSocketFrameProcessed ProcessWebSocketFrame(ArrayHolder<byte> ah, int offset, int length)
        {
            //haven't receive handshake header
            var readBuffer = ah.Array;
            if (_receivedChallengeLength < 0)
            {
                var handShakeFrame = base.ProcessWebSocketFrame(ah, offset, length);
                if (!handShakeFrame || handShakeFrame.Frame == null) return handShakeFrame;

                //Bad request
                if (BadRequestCode.Equals(handShakeFrame.Frame.Key)) return handShakeFrame;

                _receivedChallengeLength = 0;
                _handshakeCommand = handShakeFrame;
                var lengthToProcess = handShakeFrame.LengthToProcess;

                var challengeOffset = offset + length - lengthToProcess;
                if (lengthToProcess < _expectedChallengeLength)
                {
                    if (lengthToProcess > 0)
                    {
                        Buffer.BlockCopy(readBuffer, challengeOffset, _challenges, 0, lengthToProcess);
                        _receivedChallengeLength = lengthToProcess;
                    }
                    return WebSocketFrameProcessed.Fail();
                }
                Buffer.BlockCopy(readBuffer, challengeOffset, _challenges, 0, _expectedChallengeLength);
                _handshakeCommand.Data = _challenges;
                lengthToProcess -= _expectedChallengeLength;
                return WebSocketFrameProcessed.Pass(_handshakeCommand, lengthToProcess);
            }

            {
                int receivedTotal = _receivedChallengeLength + length;
                if (receivedTotal < _expectedChallengeLength)
                {
                    Buffer.BlockCopy(readBuffer, offset, _challenges, _receivedChallengeLength, length);
                    _receivedChallengeLength = receivedTotal;
                    return WebSocketFrameProcessed.Fail();
                }
                var parsedLen = _expectedChallengeLength - _receivedChallengeLength;
                Buffer.BlockCopy(readBuffer, offset, _challenges, _receivedChallengeLength, parsedLen);
                var lengthToProcess = length - parsedLen;
                _handshakeCommand.Data = _challenges;
                return WebSocketFrameProcessed.Pass(_handshakeCommand, lengthToProcess);
            }
        }
    }
}
