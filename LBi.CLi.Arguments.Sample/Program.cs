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
using LBi.Cli.Arguments;
using LBi.Cli.Arguments.Output;
using LBi.Cli.Arguments.Parsing;

namespace LBi.CLi.Arguments.Sample
{

    public abstract class ExecuteCommandBase
    {
        [Parameter(HelpMessage = "Optional parameter dictionary")]
        [DefaultValue("@{}")]
        public IDictionary<string, object> Paremters { get; set; }

        [Parameter(HelpMessage = "If set, no action is taken.")]
        public Switch WhatIf { get; set; }

        public abstract void Execute();
    }

    [ParameterSet("Name", HelpMessage = "Executes command given a name.")]
    public class ExecuteCommandUsingName : ExecuteCommandBase
    {
        [Parameter(HelpMessage = "Name"), Required]
        public string Name { get; set; }

        public override void Execute()
        {
            if (this.WhatIf.IsPresent)
                Console.WriteLine("Would have executed using name: {0}", this.Name);
            else
                Console.WriteLine("Executing using name: {0}", this.Name);
        }
    }

    [ParameterSet("Path", HelpMessage = "Executes command given a path.")]
    public class ExecuteCommandUsingPath : ExecuteCommandBase
    {
        [Parameter(HelpMessage = "The path."), Required]
        public string Path { get; set; }

        public override void Execute()
        {
            if (this.WhatIf.IsPresent)
                Console.WriteLine("Would have executed using path: {0}", this.Path);
            else
                Console.WriteLine("Executing using path: {0}", this.Path);
        }
    }


    // LBi.Cli.Arguments.exe -Name test -WhatIf -Parameters @{"foo" = "bar"}
    // or 
    // LBi.Cli.Arguments.exe -Path "c:\test\foo.txt" -WhatIf -Parameters @{"foo" = "bar"}
    class Program
    {
        static void Main(string[] args)
        {
            /*
             *  simple usage
             */

            // set up argument parser
            ArgumentParser<ExecuteCommandBase> argParser = new ArgumentParser<ExecuteCommandBase>(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));
            ExecuteCommandBase paramSet;
            if (argParser.TryParse(args, out paramSet))
            {
                paramSet.Execute();
            }


            /*
             *  advanced usage
             */

            // create parameter set collection from types
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));

            // parse the command line arguments
            Parser parser = new Parser();
            NodeSequence nodes = parser.Parse(string.Join(" ", args));

            // resolve parameter set against the parsed node set
            ResolveResult result = sets.Resolve(nodes);
            if (result.IsMatch)
            {
                paramSet = (ExecuteCommandBase)result.BestMatch.Object;
                paramSet.Execute();
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
