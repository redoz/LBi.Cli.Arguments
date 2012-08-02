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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LBi.Cli.Arguments.Parsing
{
    public class Tokenizer
    {
        internal const string ParameterIndicator = "-";
        internal const string ListStart = "@(";
        internal const string ListValueSeperator = ",";
        internal const string ListEnd = ")";
        internal const string DictionaryStart = "@{";
        internal const string DictionaryValueSeperator = "=";
        internal const string DictionaryKeySeperator = ";";
        internal const string DictionaryEnd = "}";

        public virtual IEnumerable<Token> Tokenize(params string[] args)
        {
            TokenWriter tokenWriter = new TokenWriter();
            Task.Factory.StartNew(() =>
                                      {
                                          try
                                          {
                                              using (StringReader reader = new StringReader(string.Join(" ", args)))
                                              using (BasicReader basicReader = new BasicReader(reader, StringComparer.Ordinal))
                                              {
                                                  this.Parse(tokenWriter, basicReader);
                                                  tokenWriter.Close();
                                              }
                                          }
                                          catch (Exception ex)
                                          {
                                              tokenWriter.Abort(ex);
                                          }
                                      });
            return tokenWriter.GetConsumingEnumerable();
        }

        protected virtual void Parse(TokenWriter tokenWriter, BasicReader reader)
        {
            while (!reader.Eof)
            {
                reader.AdvanceWhitespace();

                if (reader.StartsWith(ParameterIndicator))
                    this.ParseParameter(tokenWriter, reader);
                else
                    this.ParseValue(tokenWriter, reader);
            }
        }

        protected virtual void ParseValue(TokenWriter tokenWriter, BasicReader reader)
        {
            int startPos = reader.Position;

            if (reader.StartsWith("'") || reader.StartsWith("\""))
            {
                this.ParseQuotedString(tokenWriter, reader);
            }
            else if (reader.StartsWith("$"))
            {
                TokenType type;
                char[] endChar = new char[] {' ', '\t', '\r', '\n', ',', '}', ')', '=', ';' };

                string val = reader.ReadUntil(endChar.Contains);
                switch (val)
                {
                    case "$null":
                        type = TokenType.NullValue;
                        break;
                    case "$true":
                    case "$false":
                        type = TokenType.BoolValue;
                        break;
                    default:
                        throw new InvalidDataException("Not a literal token: " + val);
                }

                tokenWriter.Add(new Token(type, val, startPos, reader.Position - startPos));

            }
            else if (reader.StartsWith(DictionaryStart))
            {
                this.ParseDictionary(tokenWriter, reader);
            }
            else if (reader.StartsWith(ListStart))
            {
                this.ParseList(tokenWriter, reader);
            }
            else
            {
                char[] endChar = new char[] { ' ', '\t', '\r', '\n', ',', '}', ')', '=', ';' };
                string val = reader.PeekUntil(endChar.Contains);

                bool decimalSep = val.Any(c => c == '.');

                int skipSign = val[0] == '+' || val[0] == '-' ? 1 : 0;

                if (val.Skip(skipSign).Count(char.IsDigit) + (decimalSep ? 1 : 0) == val.Length - skipSign)
                {
                    reader.Skip(val.Length);
                    
                    tokenWriter.Add(new Token(TokenType.NumericValue, val, startPos, reader.Position - startPos));

                }
                else
                    this.ParseUnquotedString(tokenWriter, reader);
            }


        }

        protected virtual void ParseList(TokenWriter tokenWriter, BasicReader reader)
        {
            if (!reader.StartsWith(ListStart))
                throw new InvalidOperationException(String.Format("Cannot parse paramer, expected {0}.", ParameterIndicator));


            tokenWriter.Add(new Token(TokenType.ListStart, ListStart, reader.Position, ListStart.Length));
            
            // skip it
            reader.Skip(ListStart.Length);

            // trim ws
            reader.AdvanceWhitespace();

            while (!reader.StartsWith(ListEnd) && !reader.Eof)
            {
                // parse list value
                this.ParseValue(tokenWriter, reader);

                // trim ws
                reader.AdvanceWhitespace();

                // read seperator
                if (reader.StartsWith(ListValueSeperator))
                {
                    // don't output these anymore
                    ////tokenWriter.Add(new Token
                    ////                    {
                    ////                        Position = reader.Position,
                    ////                        Length = ListValueSeperator.Length,
                    ////                        Type = TokenType.ListValueSeperator,
                    ////                        Value = ListValueSeperator
                    ////                    });

                    reader.Skip(ListValueSeperator.Length);
                }
                else if (!reader.StartsWith(ListEnd))
                {
                    throw new Exception("Expected value seperator or end of list.");
                }

                // trim ws
                reader.AdvanceWhitespace();
            }



            if (reader.StartsWith(ListEnd))
            {
                tokenWriter.Add(new Token(TokenType.ListEnd, ListEnd, reader.Position, ListEnd.Length));
                    
                reader.Skip(ListEnd.Length);
            }
            else
            {
                throw new Exception("Expected end of list");
            }
        }

        protected virtual void ParseDictionary(TokenWriter tokenWriter, BasicReader reader)
        {
            if (!reader.StartsWith(DictionaryStart))
                throw new InvalidOperationException(String.Format("Cannot parse paramer, expected {0}.", ParameterIndicator));

            tokenWriter.Add(new Token(TokenType.DictionaryStart, DictionaryStart, reader.Position, DictionaryStart.Length));                    

            // skip
            reader.Skip(DictionaryStart.Length);

            // trim ws
            reader.AdvanceWhitespace();

            while (!reader.StartsWith(DictionaryEnd) && !reader.Eof)
            {
                // parse dict-key
                this.ParseValue(tokenWriter, reader);

                // trim ws
                reader.AdvanceWhitespace();

                // read seperator
                if (reader.StartsWith(DictionaryValueSeperator))
                {
                    // we no longer emit these tokens
                    ////tokenWriter.Add(new Token
                    ////                    {
                    ////                        Position = reader.Position,
                    ////                        Length = DictionaryValueSeperator.Length,
                    ////                        Type = TokenType.DictionaryValueSeperator,
                    ////                        Value = DictionaryValueSeperator
                    ////                    });

                    reader.Skip(DictionaryValueSeperator.Length);
                }
                else
                {
                    throw new Exception("Expected key seperator");
                }

                // trim ws
                reader.AdvanceWhitespace();

                // parse dict-value
                this.ParseValue(tokenWriter, reader);

                // trim ws
                reader.AdvanceWhitespace();

                // read seperator
                if (reader.StartsWith(DictionaryKeySeperator))
                {
                    // we no longer emit these tokens
                    ////tokenWriter.Add(new Token
                    ////                    {
                    ////                        Position = reader.Position,
                    ////                        Length = DictionaryKeySeperator.Length,
                    ////                        Type = TokenType.DictionaryKeySeperator,
                    ////                        Value = DictionaryKeySeperator
                    ////                    });

                    reader.Skip(DictionaryKeySeperator.Length);
                }
                else
                    break;
            }

            if (!reader.Eof && reader.StartsWith(DictionaryEnd))
            {
                tokenWriter.Add(new Token(TokenType.DictionaryEnd, DictionaryEnd, reader.Position, DictionaryEnd.Length));
                                    
                reader.Skip(DictionaryEnd.Length);
            }
            else
            {
                throw new Exception("Expected end of dictionary");
            }
        }

        protected virtual void ParseParameter(TokenWriter tokenWriter, BasicReader reader)
        {
            if (!reader.StartsWith(ParameterIndicator))
                throw new InvalidOperationException(String.Format("Cannot parse paramer, expected {0}.", ParameterIndicator));

            reader.Skip(1);

            int startPos = reader.Position;

            string val = reader.ReadUntil(char.IsWhiteSpace);

            tokenWriter.Add(new Token(TokenType.ParameterName, val, startPos, reader.Position - startPos));
        }

        protected virtual void ParseQuotedString(TokenWriter tokenWriter, BasicReader reader)
        {
            // record position for later
            int pos = reader.Position;

            // skip leading quote
            char quoteChar = reader.Read();

            StringBuilder val = new StringBuilder();

            // escape char + quote char
            char[] stopChars = new[] { '`', quoteChar };

            do
            {
                string tmp = reader.ReadUntil(stopChars.Contains);
                val.Append(tmp);

                char chr;
                if (reader.TryPeek(out chr))
                {
                    if (chr == '`')
                    {
                        // escape char, so skip one
                        reader.Read();

                        // then process the next
                        val.Append(reader.Read());
                    }
                    else if (chr == quoteChar)
                    {
                        break; // exit
                    }
                }
                else
                {
                    throw new Exception("Premature end of stream");
                }
            }
            while (!reader.Eof);

            // skip trailing quote
            reader.Skip(1);

            tokenWriter.Add(new Token(TokenType.StringValue, val.ToString(), pos, reader.Position - pos));
        }

        protected virtual void ParseUnquotedString(TokenWriter tokenWriter, BasicReader reader)
        {
            // record position for later
            int pos = reader.Position;

            StringBuilder val = new StringBuilder();

            // escape char + stop chars
            char[] stopChars = new[] { '`', ' ', '\t', '\r', '\n', '}', ')', '=' };

            do
            {
                string tmp = reader.ReadUntil(stopChars.Contains);
                val.Append(tmp);

                char chr;
                if (reader.TryPeek(out chr))
                {
                    if (chr == '`')
                    {
                        // escape char, so skip one
                        reader.Read();

                        // then process the next
                        val.Append(reader.Read());
                    }
                    else
                    {
                        break; // exit
                    }
                }
            }
            while (!reader.Eof);

            tokenWriter.Add(new Token(TokenType.StringValue, val.ToString(), pos, reader.Position - pos));
        }
    }
}
