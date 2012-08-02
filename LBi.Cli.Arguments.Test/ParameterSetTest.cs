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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using LBi.Cli.Arguments;
using LBi.Cli.Arguments.Parsing;
using Xunit;

namespace LBi.CLI.Arguments.Test
{
    public class ParameterSetTest
    {
        public abstract class ExecuteCommandBase
        {
            [Parameter(HelpMessage = "Action to take"), Required]
            public string Action { get; set; }
        }

        [ParameterSet("Name", HelpMessage = "Executes command given a name.")]
        public class ExecuteCommandUsingName : ExecuteCommandBase
        {
            [Parameter(HelpMessage = "Name"), Required]
            public string Name { get; set; }
        }

        [ParameterSet("Path", HelpMessage = "Executes command given a path.")]
        public class ExecuteCommandUsingPath : ExecuteCommandBase
        {
            [Parameter(HelpMessage = "The path."), Required]
            public string Path { get; set; }
        }

        [Fact]
        public void BuildParameterSet()
        {
            ParameterSet set = ParameterSet.FromType(typeof(ExecuteCommandUsingName));
            Assert.NotNull(set);
        }

        [Fact]
        public void BuildParameterSetCollection()
        {
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));
            Assert.NotNull(sets);

        }

        [Fact]
        public void ResolveParameterSet()
        {
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));
            IEnumerable<ParsedArgument> args = this.Parse("-Action Execute -Name 'a b c'");
            IEnumerable<ResolveError> errors;
            ParameterSet set;
            Assert.True(sets.TryResolve(args, out set, out errors));
            Assert.Empty(errors);
            Assert.NotNull(set);
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
