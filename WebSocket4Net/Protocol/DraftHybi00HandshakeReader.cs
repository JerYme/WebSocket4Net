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
        private byte[] _challenges = new byte[16];


        public DraftHybi00HandshakeReader(WebSocket websocket)
            : base(websocket)
        {
        }

        public override WebSocketFrame BuildHandShakeFrame(byte[] readBuffer, int offset, int length, out int lengthToProcess, out bool success)
        {
            //haven't receive handshake header
            if (_receivedChallengeLength < 0)
            {
                var handShakeFrame = base.BuildHandShakeFrame(readBuffer, offset, length, out lengthToProcess, out success);
                if (handShakeFrame == null) return null;

                //Bad request
                if (BadRequestCode.Equals(handShakeFrame.Key)) return handShakeFrame;

                _receivedChallengeLength = 0;
                _handshakeCommand = handShakeFrame;

                var challengeOffset = offset + length - lengthToProcess;

                if (lengthToProcess < _expectedChallengeLength)
                {
                    if (lengthToProcess > 0)
                    {
                        Buffer.BlockCopy(readBuffer, challengeOffset, _challenges, 0, lengthToProcess);
                        _receivedChallengeLength = lengthToProcess;
                        lengthToProcess = 0;
                    }

                    return null;
                }
                Buffer.BlockCopy(readBuffer, challengeOffset, _challenges, 0, _expectedChallengeLength);
                success = true;
                _handshakeCommand.Data = _challenges;
                lengthToProcess -= _expectedChallengeLength;
                return _handshakeCommand;
            }

            int receivedTotal = _receivedChallengeLength + length;
            if (receivedTotal < _expectedChallengeLength)
            {
                Buffer.BlockCopy(readBuffer, offset, _challenges, _receivedChallengeLength, length);
                lengthToProcess = 0;
                _receivedChallengeLength = receivedTotal;
                success = false;
                return null;
            }
            var parsedLen = _expectedChallengeLength - _receivedChallengeLength;
            Buffer.BlockCopy(readBuffer, offset, _challenges, _receivedChallengeLength, parsedLen);
            lengthToProcess = length - parsedLen;
            success = true;
            _handshakeCommand.Data = _challenges;
            return _handshakeCommand;

        }
    }
}
