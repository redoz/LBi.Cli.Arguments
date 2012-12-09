﻿/*
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
using System.Threading.Tasks;
using LBi.Cli.Arguments;
using LBi.Cli.Arguments.Output;
using LBi.Cli.Arguments.Parsing;

namespace LBi.CLi.Arguments.Sample
{
    public abstract class ExecuteCommandBase
    {
        [Parameter(HelpMessage = "Action to take"), Required]
        public string Action { get; set; }

        public abstract void Execute();
    }

    [ParameterSet("Name", HelpMessage = "Executes command given a name.")]
    public class ExecuteCommandUsingName : ExecuteCommandBase
    {
        [Parameter(HelpMessage = "Name"), Required]
        public string Name { get; set; }

        public override void Execute()
        {
            Console.WriteLine("Executing action {0} using name: {1}", this.Action, this.Name);
        }
    }

    [ParameterSet("Path", HelpMessage = "Executes command given a path.")]
    public class ExecuteCommandUsingPath : ExecuteCommandBase
    {
        [Parameter(HelpMessage = "The path."), Required]
        public string Path { get; set; }

        public override void Execute()
        {
            Console.WriteLine("Executing action {0} using path: {1}", this.Action, this.Path);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // create parameter set collection from types
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));

            // parse the command line arguments
            Parser parser = new Parser();
            ArgumentCollection parsedArguments = parser.Parse(string.Join(" ", args));

            // resolve parameter set against the parsed arguments
            ResolveResult result = sets.Resolve(parsedArguments);
            if (result.IsMatch)
            {
                ParameterSetResult matchingSet = result.BestMatch;
                ExecuteCommandBase command = (ExecuteCommandBase)matchingSet.Object;
                command.Execute();
            }
            else
            {
                ErrorWriter errorWriter = new ErrorWriter(Console.Error);
                errorWriter.Write(result.BestMatch);
                HelpWriter helpWriter = new HelpWriter(Console.Out);
                helpWriter.Write(sets, HelpLevel.Parameters);
            }
        }
    }
}