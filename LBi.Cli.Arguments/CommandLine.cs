/*
 * Copyright 2016 Patrik Husfloen
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
using LBi.Cli.Arguments.Parsing;

namespace LBi.Cli.Arguments
{
    public static class CommandLine

    {
        static CommandLine()
        {
            using (StringReader stringReader = new StringReader(Environment.CommandLine))
            using (BasicReader reader = new BasicReader(stringReader, StringComparer.Ordinal, disposeReader: false))
            {
                // just in case
                reader.AdvanceWhitespace();
                char first;
                if (reader.TryPeek(out first))
                {
                    if (first == '"')
                    {
                        reader.Skip(1);
                        Path = reader.ReadUntil(c => c == '"');
                        reader.Skip(1);
                    }
                    else
                        Path = reader.ReadUntil(char.IsWhiteSpace);

                    // skip whitespace
                    if (!reader.Eof)
                        reader.Skip(1);

                    // dirty hack to read to eof
                    Arguments = reader.ReadUntil(c => false);
                }
            }
        }

        public static string Path { get; }

        public static string Arguments { get; }
    }
}
