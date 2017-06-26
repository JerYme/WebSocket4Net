using System.Collections.Generic;
using System.Diagnostics;
using WebSocket4Net.Common;
using WebSocket4Net.Protocol.FrameReader;

namespace WebSocket4Net.Protocol
{
    sealed class DraftHybi10DataReader : DataReaderBase
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

        public int SegmentsLength => _dataFrame.ArrayView.Length;

        public override ArrayView<byte> ArrayView => _dataFrame.ArrayView;

        public override bool Process(out int lengthToProcess)
        {
            var processFrame = _frameReader.Process(_frameIndex, _dataFrame);

            lengthToProcess = processFrame.LengthToProcess;
            var reader = processFrame.Reader;

            if (reader != null)
            {
                _frameReader = reader;
                _frameIndex = processFrame.FrameIndex;
            }

            if (!processFrame && reader == null)
            {

            }

            return processFrame;
            //if (nextFrameReader == null) return success;

            //_frameIndex = _dataFrame.Segments.SegmentsLength - lengthToProcess;
            //if (!success && lengthToProcess > 0) _dataFrame.Segments.TrimEnd(lengthToProcess);
            //return success;
            //if (lengthToProcess > 0) _dataFrame.Segments.TrimEnd(lengthToProcess);
        }

        public override void ResetWebSocketFrame()
        {
            _dataFrame = new WebSocketDataFrame(new ArrayView<byte>());
            _frameIndex = 0;
            _frameReader = FrameReader.FrameReader.Root;
        }

        public override WebSocketFrame BuildWebSocketFrame(int lengthToProcess)
        {
            // Control frames MAY be injected in the middle of
            // a fragmented message.Control frames themselves MUST NOT be
            // fragmented.
            if (_dataFrame.IsControlFrame) return new WebSocketFrame(lengthToProcess, _dataFrame);

            if (!_dataFrame.FIN) // https://tools.ietf.org/html/rfc6455#section-5.4
            {
                if (_dataFrame.OpCode == OpCode.Fragmented) // fragmented continued...
                {
                    Debug.Assert(_fragmentedDataFrames != null);
                    _fragmentedDataFrames.Add(_dataFrame);
                    return null;
                }
                //if (_fragmentedDataFrames != null)
                //{
                //    _fragmentedDataFrames.Add(_dataFrame);
                //    _dataFrame = new WebSocketDataFrame(new ArraySegmentList<byte>());
                //    return null;
                //}

                // initiated
                Debug.Assert(_fragmentedDataFrames == null);
                _fragmentedDataFrames = new List<WebSocketDataFrame> { _dataFrame };
                return null;
            }

            if (_dataFrame.OpCode == OpCode.Fragmented) // terminated
            {
                Debug.Assert(_fragmentedDataFrames != null);
                _fragmentedDataFrames.Add(_dataFrame);
                var frame = new WebSocketFrame(lengthToProcess, _fragmentedDataFrames);
                _fragmentedDataFrames = null;
                return frame;
            }

            return new WebSocketFrame(lengthToProcess, _dataFrame);
        }

        public override void Clear()
        {
            _dataFrame = new WebSocketDataFrame(new ArrayView<byte>());
            _frameIndex = 0;
            _frameReader = FrameReader.FrameReader.Root;
        }

    }
}
