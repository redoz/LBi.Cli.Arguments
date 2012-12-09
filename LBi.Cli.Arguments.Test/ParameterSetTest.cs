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

        [ParameterSet("Parameters", HelpMessage = "Executes command with parameters.")]
        public class ExecuteCommandWithParameters : ExecuteCommandBase
        {
            [Parameter(HelpMessage = "Parameters"), Required]
            public IDictionary<string, object> Parameters { get; set; }  
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
            ArgumentCollection args = this.Parse("-Action Execute -Name 'a b c'");
            ResolveResult result = sets.Resolve(args);
            
            var selectedSet = result.Single(r => r.Errors.Length == 0);
            ExecuteCommandUsingName cmd = selectedSet.Object as ExecuteCommandUsingName;
            Assert.NotNull(cmd);
            Assert.Equal("a b c", cmd.Name);
            Assert.Equal("Execute", cmd.Action);

            var failedSet = result.Single(r => r.Errors.Length > 0);
            ExecuteCommandUsingPath pathCmd = failedSet.Object as ExecuteCommandUsingPath;
            Assert.NotNull(pathCmd);
            Assert.Equal("Execute", pathCmd.Action);
        }

        [Fact]
        public void ResolveParameterSet_IntToString()
        {
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));
            ArgumentCollection args = this.Parse("-Action Execute -Name 50");
            ResolveResult result = sets.Resolve(args);

            var selectedSet = result.Single(r => r.Errors.Length == 0);
            ExecuteCommandUsingName cmd = selectedSet.Object as ExecuteCommandUsingName;
            Assert.NotNull(cmd);
            Assert.Equal("50", cmd.Name);
            Assert.Equal("Execute", cmd.Action);
        }

        [Fact]
        public void ResolveParameterSet_BoolToString()
        {
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));
            ArgumentCollection args = this.Parse("-Action Execute -Name $true");
            ResolveResult result = sets.Resolve(args);

            var selectedSet = result.Single(r => r.Errors.Length == 0);
            ExecuteCommandUsingName cmd = selectedSet.Object as ExecuteCommandUsingName;
            Assert.NotNull(cmd);
            Assert.Equal("True", cmd.Name);
            Assert.Equal("Execute", cmd.Action);
        }

        [Fact]
        public void ResolveParameterSet_WithParameters()
        {
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandWithParameters));
            ArgumentCollection args = this.Parse("-Action Execute -Parameters @{foo = 'bar'; bar = 4}");
            ResolveResult result = sets.Resolve(args);

            var selectedSet = result.Single(r => r.Errors.Length == 0);
            ExecuteCommandWithParameters cmd = selectedSet.Object as ExecuteCommandWithParameters;
            Assert.NotNull(cmd);
            Assert.Equal("Execute", cmd.Action);
            Assert.Equal("bar", cmd.Parameters["foo"]);
            Assert.Equal((byte)4, cmd.Parameters["bar"]);
        }


        private ArgumentCollection Parse(params string[] arg)
        {
            Parser parser = new Parser();
            return parser.Parse(string.Join(" ", arg));
        }
    }
}
