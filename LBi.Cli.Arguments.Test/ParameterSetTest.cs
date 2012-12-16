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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using LBi.Cli.Arguments;
using LBi.Cli.Arguments.Binding;
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

            [Parameter(HelpMessage = "Verbose output")]
            public Switch Verbose { get; set; }
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
            [DefaultValue("@{}")]
            [Parameter(HelpMessage = "Parameters"), Required]
            public IDictionary<string, object> Parameters { get; set; }

            [Parameter(HelpMessage = "Parameters")]
            public Switch Test { get; set; }
        }

        [ParameterSet("WithCommand", Command = "Test", HelpMessage = "Executes command with parameters.")]
        public class ParameterSetWithCommand : ExecuteCommandBase
        {
            [DefaultValue("@{}")]
            [Parameter(HelpMessage = "Parameters")]
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
            Assert.Equal(2, sets.Count);
        }

        [Fact]
        public void ResolveParameterSet()
        {
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));
            NodeSequence args = this.Parse("-Action Execute -Name 'a b c'");
            ResolveResult result = sets.Resolve(new ParameterSetBuilder(),
                                                new IntransigentTypeConverter(),
                                                CultureInfo.InvariantCulture, args);
            
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
        public void ResolveParameterSetWithCommand()
        {
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath), typeof(ParameterSetWithCommand));
            NodeSequence args = this.Parse("Test -Action Execute");
            ResolveResult result = sets.Resolve(new ParameterSetBuilder(),
                                                new IntransigentTypeConverter(),
                                                CultureInfo.InvariantCulture, args);
            
            var selectedSet = result.Single(r => r.Errors.Length == 0);
            ParameterSetWithCommand cmd = selectedSet.Object as ParameterSetWithCommand;
            Assert.NotNull(cmd);
        }

        [Fact]
        public void ResolveParameterSet_IntToString()
        {
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));
            NodeSequence args = this.Parse("-Action Execute -Name 50");
            ResolveResult result = sets.Resolve(new ParameterSetBuilder(),new IntransigentTypeConverter(),CultureInfo.InvariantCulture, args);
    
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
            NodeSequence args = this.Parse("-Action Execute -Name $true");
            ResolveResult result = sets.Resolve(new ParameterSetBuilder(),new IntransigentTypeConverter(),CultureInfo.InvariantCulture, args);

            var selectedSet = result.Single(r => r.Errors.Length == 0);
            ExecuteCommandUsingName cmd = selectedSet.Object as ExecuteCommandUsingName;
            Assert.NotNull(cmd);
            Assert.Equal("True", cmd.Name);
            Assert.Equal("Execute", cmd.Action);
        }

        [Fact]
        public void ResolveParameterSet_WithSwitch()
        {
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));
            NodeSequence args = this.Parse("-Action Execute -Name $true -Verbose");
            ResolveResult result = sets.Resolve(new ParameterSetBuilder(),new IntransigentTypeConverter(),CultureInfo.InvariantCulture, args);

            var selectedSet = result.Single(r => r.Errors.Length == 0);
            ExecuteCommandUsingName cmd = selectedSet.Object as ExecuteCommandUsingName;
            Assert.NotNull(cmd);
            Assert.Equal("True", cmd.Name);
            Assert.Equal("Execute", cmd.Action);
            Assert.True(cmd.Verbose.IsPresent);
        }

        [Fact]
        public void ResolveParameterSet_WithSwitchNegated()
        {
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));
            NodeSequence args = this.Parse("-Action Execute -Name $true -Verbose:$false");
            ResolveResult result = sets.Resolve(new ParameterSetBuilder(),new IntransigentTypeConverter(),CultureInfo.InvariantCulture, args);

            var selectedSet = result.Single(r => r.Errors.Length == 0);
            ExecuteCommandUsingName cmd = selectedSet.Object as ExecuteCommandUsingName;
            Assert.NotNull(cmd);
            Assert.Equal("True", cmd.Name);
            Assert.Equal("Execute", cmd.Action);
            Assert.False(cmd.Verbose.IsPresent);
        }

        [Fact]
        public void ResolveParameterSet_WithPath()
        {
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));
            NodeSequence args = this.Parse(@"-Action Execute -Path c:\temp\foo -Verbose");
            ResolveResult result = sets.Resolve(new ParameterSetBuilder(),new IntransigentTypeConverter(),CultureInfo.InvariantCulture, args);

            var selectedSet = result.Single(r => r.Errors.Length == 0);
            ExecuteCommandUsingPath cmd = selectedSet.Object as ExecuteCommandUsingPath;
            Assert.NotNull(cmd);
            Assert.Equal(@"c:\temp\foo", cmd.Path);
            Assert.Equal("Execute", cmd.Action);
            Assert.True(cmd.Verbose.IsPresent);
        }


        [Fact]
        public void ResolveParameterSet_WithSwitchExplicit()
        {
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));
            NodeSequence args = this.Parse("-Action Execute -Name $true -Verbose:$true");
            ResolveResult result = sets.Resolve(new ParameterSetBuilder(),new IntransigentTypeConverter(),CultureInfo.InvariantCulture, args);

            var selectedSet = result.Single(r => r.Errors.Length == 0);
            ExecuteCommandUsingName cmd = selectedSet.Object as ExecuteCommandUsingName;
            Assert.NotNull(cmd);
            Assert.Equal("True", cmd.Name);
            Assert.Equal("Execute", cmd.Action);
            Assert.True(cmd.Verbose.IsPresent);
        }

        [Fact]
        public void ResolveParameterSet_WithParameters()
        {
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandWithParameters));
            NodeSequence args = this.Parse("-Action Execute -Parameters @{foo = 'bar'; bar = 4}");
            ResolveResult result = sets.Resolve(new ParameterSetBuilder(),new IntransigentTypeConverter(),CultureInfo.InvariantCulture, args);

            var selectedSet = result.Single(r => r.Errors.Length == 0);
            ExecuteCommandWithParameters cmd = selectedSet.Object as ExecuteCommandWithParameters;
            Assert.NotNull(cmd);
            Assert.Equal("Execute", cmd.Action);
            Assert.Equal("bar", cmd.Parameters["foo"]);
            Assert.Equal((byte)4, cmd.Parameters["bar"]);
        }

        [Fact]
        public void ResolveParameterSet_WithParameters_DefaultValue()
        {
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandWithParameters));
            NodeSequence args = this.Parse("-Action Execute -Test");
            ResolveResult result = sets.Resolve(new ParameterSetBuilder(),new IntransigentTypeConverter(),CultureInfo.InvariantCulture, args);

            var selectedSet = result.Single(r => r.Errors.Length == 0);
            ExecuteCommandWithParameters cmd = selectedSet.Object as ExecuteCommandWithParameters;
            Assert.NotNull(cmd);
            Assert.Equal("Execute", cmd.Action);
            Assert.NotNull(cmd.Parameters);
        }


        private NodeSequence Parse(params string[] arg)
        {
            Parser parser = new Parser();
            return parser.Parse(string.Join(" ", arg));
        }
    }
}
