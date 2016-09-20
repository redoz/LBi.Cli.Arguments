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

namespace LBi.Cli.Arguments.Output
{
    public class ConsoleWriter : TextWriter
    {
        private readonly StringBuilder _buffer;
        private readonly TextWriter _writer;
        private readonly bool _hasConsole;

        public ConsoleWriter(TextWriter writer)
        {
            this._writer = writer;
            this._buffer = new StringBuilder();

            try
            {
                this._hasConsole = Console.BufferWidth > 0;
            }
            catch (IOException)
            {
                this._hasConsole = false;
            }
        }

        public override void Write(char value)
        {
            if (!this._hasConsole)
            {
                this._writer.Write(value);
            }
            else if (char.IsWhiteSpace(value))
            {
                if (Console.BufferWidth > Console.CursorLeft + this._buffer.Length)
                {
                    this._writer.Write(this._buffer.ToString());
                    this._writer.Write(value);
                    this._buffer.Clear();
                }
                else if (Console.BufferWidth == Console.CursorLeft + this._buffer.Length)
                {
                    this._writer.Write(this._buffer.ToString());
                    this._writer.WriteLine();
                    this._buffer.Clear();
                }
                else
                {
                    this._writer.WriteLine();
                    this._writer.Write(this._buffer.ToString());
                    this._writer.Write(value);
                    this._buffer.Clear();
                }
            }
            else
            {
                if (Console.BufferWidth == this._buffer.Length)
                {
                    this._writer.WriteLine();
                    this._writer.WriteLine(this._buffer.ToString());
                    this._buffer.Clear();
                    this._buffer.Append(value);
                }
                else
                {
                    this._buffer.Append(value);
                }
            }
        }

        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }
}
