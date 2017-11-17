using System.Collections.Generic;
using System.Diagnostics;
using WebSocket4Net.Common;
using WebSocket4Net.Protocol.FrameReader;

namespace WebSocket4Net.Protocol
{
    sealed class DraftHybi10DataReader : ReaderBase
    {
        public DraftHybi10DataReader(WebSocket websocket, ArrayView<byte> arrayView) : base(websocket, arrayView)
        {
            _dataFrame = new WebSocketDataFrame(base.ArrayView);
            _frameReader = FrameReader.FrameReader.Root;
        }

        private List<WebSocketDataFrame> _fragmentedDataFrames;
        private WebSocketDataFrame _dataFrame;
        private IFrameReader _frameReader;
        private int _frameIndex;

        public override ArrayView<byte> ArrayView => _dataFrame.ArrayView;

        private bool Process(out int lengthToProcess)
        {
            var processFrame = _frameReader.Process(_frameIndex, _dataFrame);

            lengthToProcess = processFrame.LengthToProcess;
            var reader = processFrame.Reader;

            if (reader != null)
            {
                _frameReader = reader;
                _frameIndex = processFrame.Index;
            }

            return processFrame;
        }

        private void ResetDataFrame()
        {
            _dataFrame = new WebSocketDataFrame(new ArrayView<byte>());
            _frameIndex = 0;
            _frameReader = FrameReader.FrameReader.Root;
        }

        private bool? _skipData;

        public override WebSocketFrameProcessed ProcessWebSocketFrame(ArrayHolder<byte> ah, int offset, int length)
        {
            var arrayChunk = AddChunk(ArrayChunk<byte>.New(_skipData == true ? null : ah, offset, length, ArrayView.Length));
            if (arrayChunk != null && _skipData == null && ArrayView.ChunkCount == 1)
            {
                var actualPayloadLength = new WebSocketDataFrame(ArrayView).ActualPayloadLengthNullable;
                if (actualPayloadLength > 1024 * 1024 * 25)
                {
                    _skipData = true;
                }
            }

            int lengthToProcess;
            var processed = Process(out lengthToProcess);
            if (processed)
            {
                var webSocketFrame = BuildWebSocketFrame();
                if (_skipData != true) webSocketFrame?.Decode();
                ResetDataFrame();

                _skipData = null;
                return WebSocketFrameProcessed.Pass(webSocketFrame, lengthToProcess);
            }
            return WebSocketFrameProcessed.Fail(lengthToProcess);
        }


        private WebSocketFrame BuildWebSocketFrame()
        {
            // Control frames MAY be injected in the middle of
            // a fragmented message.Control frames themselves MUST NOT be
            // fragmented.
            if (_dataFrame.IsControlFrame) return new WebSocketFrame(_dataFrame);

            if (!_dataFrame.FIN) // https://tools.ietf.org/html/rfc6455#section-5.4
            {
                if (_dataFrame.OpCode == OpCode.Fragmented) // fragmented continued...
                {
                    Debug.Assert(_fragmentedDataFrames != null);
                    _fragmentedDataFrames.Add(_dataFrame);
                    return null;
                }

                // initiated
                Debug.Assert(_fragmentedDataFrames == null);
                _fragmentedDataFrames = new List<WebSocketDataFrame> { _dataFrame };
                return null;
            }

            if (_dataFrame.OpCode == OpCode.Fragmented) // terminated
            {
                Debug.Assert(_fragmentedDataFrames != null);
                _fragmentedDataFrames.Add(_dataFrame);
                var frame = new WebSocketFrame(_fragmentedDataFrames);
                _fragmentedDataFrames = null;
                return frame;
            }

            return new WebSocketFrame(_dataFrame);
        }

        //public override void Clear()
        //{
        //    _dataFrame = new WebSocketDataFrame(new ArrayView<byte>());
        //    _frameIndex = 0;
        //    _frameReader = FrameReader.FrameReader.Root;
        //}

    }
}
