/*
 * Copyright 2012 LBi Netherlands B.V.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */

using System;
using System.IO;
using System.Text;

namespace LBi.Cli.Arguments.Parsing
{
    public class BasicReader : IDisposable
    {
        private readonly StringBuilder _buffer;
        private readonly StringComparer _comparer;
        private readonly TextReader _reader;
        private int _position;
        private readonly bool _disposeReader;

        public BasicReader(TextReader reader, StringComparer comparer, bool disposeReader = false)
        {
            this._reader = reader;
            this._buffer = new StringBuilder();
            this._comparer = comparer;
            this._position = 0;
            this._disposeReader = disposeReader;
        }

        public bool Eof => this._buffer.Length == 0 && this._reader.Peek() == -1;

        public int Position => this._position;

        #region IDisposable Members

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BasicReader()
        {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (this._disposeReader)
                    this._reader.Dispose();
            }
        }

        #endregion

        public bool StartsWith(string str)
        {
            if (!this.EnsureBufferSize(str.Length))
                return false;

            string start = this._buffer.ToString(0, str.Length);

            return this._comparer.Equals(str, start);
        }

        public int AdvanceWhitespace()
        {
            int ret;
            char p;

            for (ret = 0; this.TryPeek(out p) && char.IsWhiteSpace(p); ret++)
                this.Read();

            return ret;
        }

        private bool EnsureBufferSize(int length)
        {
            while (length > this._buffer.Length)
            {
                char[] buf = new char[length - this._buffer.Length];
                int read = this._reader.Read(buf, 0, buf.Length);

                if (read == 0) // end of stream
                    return false;

                this._buffer.Append(buf, 0, read);
            }

            return true;
        }

        public bool TryPeek(out char ret)
        {
            bool success = false;
            ret = default(char);
            if (this._buffer.Length > 0)
            {
                ret = this._buffer[0];
                success = true;
            }
            else
            {
                int tmp = this._reader.Peek();
                if (tmp >= 0)
                {
                    ret = (char)tmp;
                    success = true;
                }
            }

            return success;
        }

        public bool TryRead(out char ret)
        {
            bool success = false;
            ret = default(char);
            if (this._buffer.Length > 0)
            {
                ret = this._buffer[0];
                this._buffer.Remove(0, 1);
                success = true;
                this._position++;
            }
            else
            {
                int tmp = this._reader.Read();
                if (tmp >= 0)
                {
                    ret = (char)tmp;
                    success = true;
                    this._position++;
                }
            }

            return success;
        }

        public bool TryRead(int len, out string str)
        {
            bool success = this.EnsureBufferSize(len);

            if (success)
            {
                str = this._buffer.ToString(0, len);
                this._position += len;
                this._buffer.Remove(0, len);
            }
            else
                str = null;


            return success;
        }

        public void Skip(int len)
        {
            string tmp;
            if (!this.TryRead(len, out tmp))
                throw new InvalidOperationException("No data left");
        }

        public string ReadUntil(Func<char, bool> predicate)
        {
            StringBuilder paramName = new StringBuilder();
            char next;

            while (this.TryPeek(out next) && !predicate(next))
                paramName.Append(this.Read());

            return paramName.ToString();
        }

        public char Read()
        {
            char ret;
            if (this.TryRead(out ret))
                return ret;

            throw new InvalidOperationException("No more data.");
        }

        public char Peek()
        {
            char ret;
            if (this.TryPeek(out ret))
                return ret;

            throw new InvalidOperationException("No more data.");
        }

        public string PeekUntil(Func<char, bool> predicate)
        {
            for (int i = 0; i < this._buffer.Length; i++)
            {
                if (predicate(this._buffer[i]))
                    return this._buffer.ToString(0, i);
            }

            while (this._reader.Peek() != -1 && !predicate((char)this._reader.Peek()))
                this._buffer.Append((char)this._reader.Read());

            return this._buffer.ToString();
        }
    }
}
