using System;
using System.Buffers;
using System.Text;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Provides a buffer with a method for JSON extraction.
    /// </summary>
    public sealed class JsonBuffer : IDisposable
    {
        private readonly int _bufferSize;
        private readonly int _maxBufferSize;
        private readonly ArrayPool<byte> _arrayPool;

        private byte[] _buffer;
        private int _bufferCurPos;
        private int _jsonStartPos;
        private int _jsonCurPos;
        private int _jsonStackedBrackets;
        private bool _jsonStarted;
        private bool _insideQuotes;
        private bool _isEscaping;
        
        /// <summary>
        /// Creates a new instance of <see cref="JsonBuffer"/> class.
        /// </summary>
        /// <param name="bufferSize">The default buffer size.</param>
        /// <param name="maxBufferSize">The max buffer size for increasing.</param>
        /// <param name="arrayPool">The array pool for getting buffer instances.</param>
        public JsonBuffer(int bufferSize, int maxBufferSize = 0, ArrayPool<byte> arrayPool = null)
        {
            _bufferSize = bufferSize;
            _maxBufferSize = maxBufferSize;
            _arrayPool = arrayPool ?? ArrayPool<byte>.Shared;
            _buffer = _arrayPool.Rent(bufferSize);
        }

        public byte[] Buffer => _buffer;

        public int BufferCurPos
        {
            set { _bufferCurPos = value; }
            get { return _bufferCurPos; }
        }

        /// <summary>
        /// Try to extract a JSON document from the buffer, based on the brackets count.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public bool TryExtractJsonFromBuffer(out byte[] json)
        {
            if (_bufferCurPos > _buffer.Length)
            {
                throw new ArgumentException("Buffer current pos or length value is invalid");
            }

            json = null;
            int jsonLenght = 0;

            for (int i = _jsonCurPos; i < _bufferCurPos; i++)
            {
                _jsonCurPos = i + 1;

                if (_buffer[i] == '"' && !_isEscaping)
                {
                    _insideQuotes = !_insideQuotes;
                }

                if (!_insideQuotes)
                {
                    if (_buffer[i] == '{')
                    {
                        _jsonStackedBrackets++;
                        if (!_jsonStarted)
                        {
                            _jsonStartPos = i;
                            _jsonStarted = true;
                        }
                    }
                    else if (_buffer[i] == '}')
                    {
                        _jsonStackedBrackets--;
                    }

                    if (_jsonStarted &&
                        _jsonStackedBrackets == 0)
                    {
                        jsonLenght = i - _jsonStartPos + 1;
                        break;
                    }
                }
                else
                {
                    if (_isEscaping)
                    {
                        _isEscaping = false;
                    }
                    else if (_buffer[i] == '\\')
                    {
                        _isEscaping = true;
                    }
                }
            }

            if (jsonLenght > 1)
            {
                json = new byte[jsonLenght];
                System.Buffer.BlockCopy(_buffer, _jsonStartPos, json, 0, jsonLenght);

                // Shifts the buffer to the left
                _bufferCurPos -= (jsonLenght + _jsonStartPos);
                System.Buffer.BlockCopy(_buffer, jsonLenght + _jsonStartPos, _buffer, 0, _bufferCurPos);

                Reset();


                return true;
            }

            return false;
        }

        /// <summary>
        /// Increases the receiver buffer, if allowed.
        /// </summary>
        public void IncreaseBuffer()
        {
            if (_maxBufferSize == 0 
                || _buffer.Length + _bufferSize > _maxBufferSize)
            {
                throw new BufferOverflowException("Maximum buffer size reached");
            }

            var currentBuffer = _buffer;
            var increasedBuffer = _arrayPool.Rent(_buffer.Length + _bufferSize);
            System.Buffer.BlockCopy(currentBuffer, 0, increasedBuffer, 0, currentBuffer.Length);
            _buffer = increasedBuffer;
            _arrayPool.Return(currentBuffer, true);
        }

        private void Reset()
        {
            _jsonCurPos = 0;
            _jsonStartPos = 0;
            _jsonStarted = false;
            _insideQuotes = false;
            _isEscaping = false;

            if (_buffer.Length > _bufferSize
                && _bufferCurPos < _bufferSize)
            {
                var currentBuffer = _buffer;
                var decreasedBuffer = _arrayPool.Rent(_bufferSize);
                System.Buffer.BlockCopy(currentBuffer, 0, decreasedBuffer, 0, _bufferSize);
                _buffer = decreasedBuffer;
                _arrayPool.Return(currentBuffer);
            }
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(_buffer, 0, _buffer.Length);
        }

        public void Dispose()
        {
            _arrayPool.Return(_buffer);
        }
    }
}