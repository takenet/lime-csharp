using System;
using System.Text;

namespace Lime.Transport
{
    /// <summary>
    /// Provides a buffer with a method for JSON extraction.
    /// </summary>
    internal sealed class JsonBuffer
    {
        private readonly byte[] _buffer;
        private int _bufferCurPos;
        private int _jsonStartPos;
        private int _jsonCurPos;
        private int _jsonStackedBrackets;
        private bool _jsonStarted;
        private bool _insideQuotes;

        public JsonBuffer(int bufferSize)
        {
            _buffer = new byte[bufferSize];
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

                if (_buffer[i] == '"' &&
                    (i == 0 || _buffer[i - 1] != '\\'))
                {
                    _insideQuotes = !_insideQuotes;
                }

                if (_insideQuotes) continue;                

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

            if (jsonLenght > 1)
            {
                json = new byte[jsonLenght];
                Array.Copy(_buffer, _jsonStartPos, json, 0, jsonLenght);

                // Shifts the buffer to the left
                _bufferCurPos -= (jsonLenght + _jsonStartPos);
                Array.Copy(_buffer, jsonLenght + _jsonStartPos, _buffer, 0, _bufferCurPos);

                _jsonCurPos = 0;
                _jsonStartPos = 0;
                _jsonStarted = false;

                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(_buffer);
        }
    }
}