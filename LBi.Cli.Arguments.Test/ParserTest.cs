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
            NodeSequence parsedArgs = this.Parse("-arg val -arg2 @($null)");
            Assert.Equal(4, parsedArgs.Count);
            Assert.IsType<ParameterName>(parsedArgs[0]);
            Assert.IsType<LiteralValue>(parsedArgs[1]);
            Assert.IsType<ParameterName>(parsedArgs[2]);
            Assert.IsType<Sequence>(parsedArgs[3]);

            
            Assert.Equal(1, ((Sequence)parsedArgs[3]).Elements.Length);
            Assert.IsType<LiteralValue>(((Sequence)parsedArgs[3]).Elements[0]);
            Assert.Equal(LiteralValueType.Null, ((LiteralValue)((Sequence)parsedArgs[3]).Elements[0]).ValueType);
        }

        [Fact]
        public void ParsePositionalParameters()
        {
            NodeSequence parsedArgs = this.Parse("val @($null)");
            Assert.Equal(2, parsedArgs.Count);
            Assert.IsType<LiteralValue>(parsedArgs[0]);
            Assert.IsType<Sequence>(parsedArgs[1]);
            Assert.Equal(1, ((Sequence)parsedArgs[1]).Elements.Length);
            Assert.IsType<LiteralValue>(((Sequence)parsedArgs[1]).Elements[0]);
            Assert.Equal(LiteralValueType.Null, ((LiteralValue)((Sequence)parsedArgs[1]).Elements[0]).ValueType);
        }

        [Fact]
        public void ParsePositionalAndNamedParameters()
        {
            NodeSequence parsedArgs = this.Parse("val -arg @($null)");
            Assert.Equal(3, parsedArgs.Count);
            Assert.IsType<LiteralValue>(parsedArgs[0]);
            Assert.IsType<ParameterName>(parsedArgs[1]);
            Assert.IsType<Sequence>(parsedArgs[2]);
            Assert.Equal(1, ((Sequence)parsedArgs[2]).Elements.Length);
            Assert.IsType<LiteralValue>(((Sequence)parsedArgs[2]).Elements[0]);
            Assert.Equal(LiteralValueType.Null, ((LiteralValue)((Sequence)parsedArgs[2]).Elements[0]).ValueType);
        }

        [Fact]
        public void ParseImplicitList()
        {
            NodeSequence parsedArgs = this.Parse("1,2,3,4,5 1,2,3,4,5");
            Assert.Equal(2, parsedArgs.Count);
            Assert.IsType<Sequence>(parsedArgs[0]);
            Assert.Equal(5, ((Sequence)parsedArgs[0]).Elements.Length);
            Assert.Equal(5, ((Sequence)parsedArgs[1]).Elements.Length);
        }

        [Fact]
        public void ParseImplicitList2()
        {
            NodeSequence parsedArgs = this.Parse("1,2,3,4,5 @(1,2,3,4,5)");
            Assert.Equal(2, parsedArgs.Count);
            Assert.IsType<Sequence>(parsedArgs[0]);
            Assert.Equal(5, ((Sequence)parsedArgs[0]).Elements.Length);
            Assert.Equal(5, ((Sequence)parsedArgs[1]).Elements.Length);
        }


        private NodeSequence Parse(params string[] arg)
        {
            //Tokenizer tok = new Tokenizer()
            //Token[] tokens = tok.Tokenize(arg).ToArray();
            Parser parser = new Parser();
            return parser.Parse(string.Join(" ", arg));
        }
    }
}
