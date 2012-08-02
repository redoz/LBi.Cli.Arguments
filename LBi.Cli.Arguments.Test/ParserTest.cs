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

using System.Collections.Generic;
using System.Linq;
using LBi.Cli.Arguments;
using LBi.Cli.Arguments.Parsing;
using LBi.Cli.Arguments.Parsing.Ast;
using Xunit;

namespace LBi.CLI.Arguments.Test
{
    public class ParserTest
    {
        [Fact]
        public void ParseNamedParameters()
        {
            ParsedArgument[] parsedArgs = this.Parse("-arg val -arg2 @($null)").ToArray();
            Assert.Equal(2, parsedArgs.Length);
            Assert.IsType<LiteralValue>(parsedArgs[0].Value);
            Assert.IsType<Sequence>(parsedArgs[1].Value);
            Assert.Equal(1, ((Sequence)parsedArgs[1].Value).Elements.Length);
            Assert.IsType<LiteralValue>(((Sequence)parsedArgs[1].Value).Elements[0]);
            Assert.Equal(LiteralValueType.Null, ((LiteralValue)((Sequence)parsedArgs[1].Value).Elements[0]).Type);
        }

        [Fact]
        public void ParsePositionalParameters()
        {
            ParsedArgument[] parsedArgs = this.Parse("val @($null)").ToArray();
            Assert.Equal(2, parsedArgs.Length);
            Assert.IsType<LiteralValue>(parsedArgs[0].Value);
            Assert.IsType<Sequence>(parsedArgs[1].Value);
            Assert.Equal(1, ((Sequence)parsedArgs[1].Value).Elements.Length);
            Assert.IsType<LiteralValue>(((Sequence)parsedArgs[1].Value).Elements[0]);
            Assert.Equal(LiteralValueType.Null, ((LiteralValue)((Sequence)parsedArgs[1].Value).Elements[0]).Type);
        }

        [Fact]
        public void ParsePositionalAndNamedParameters()
        {
            ParsedArgument[] parsedArgs = this.Parse("val -arg @($null)").ToArray();
            Assert.Equal(2, parsedArgs.Length);
            Assert.IsType<LiteralValue>(parsedArgs[0].Value);
            Assert.IsType<Sequence>(parsedArgs[1].Value);
            Assert.Equal(1, ((Sequence)parsedArgs[1].Value).Elements.Length);
            Assert.IsType<LiteralValue>(((Sequence)parsedArgs[1].Value).Elements[0]);
            Assert.Equal(LiteralValueType.Null, ((LiteralValue)((Sequence)parsedArgs[1].Value).Elements[0]).Type);
        }


        private IEnumerable<ParsedArgument> Parse(params string[] arg)
        {
            Tokenizer tok = new Tokenizer();
            Token[] tokens = tok.Tokenize(arg).ToArray();
            Parser parser = new Parser();
            return parser.Parse(tokens);
        }
    }
}
