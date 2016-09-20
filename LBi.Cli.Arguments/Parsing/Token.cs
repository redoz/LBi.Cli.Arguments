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

namespace LBi.Cli.Arguments.Parsing
{
    public class Token : ISourceInfo
    {
        public Token(TokenType type, string value, int position, int length)
        {
            this.Type = type;
            this.Value = value;
            this.Position = position;
            this.Length = length;
        }

        public string Value { get; protected set; }

        public int Position { get; protected set; }

        public int Length { get; protected set; }

        public TokenType Type { get; protected set; }

        public override string ToString()
        {
            return string.Format(
                                 "{{Type: {0}, Pos: {1}, Len: {2}, Value: '{3}'}}",
                                 this.Type,
                                 this.Position,
                                 this.Length,
                                 this.Value);
        }
    }
}
