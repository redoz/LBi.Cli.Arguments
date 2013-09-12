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
using LBi.Cli.Arguments;
using LBi.Cli.Arguments.Binding;
using LBi.Cli.Arguments.Output;
using LBi.Cli.Arguments.Parsing;

namespace LBi.CLi.Arguments.Sample
{

    public abstract class ExecuteCommandBase
    {
        [Parameter(HelpMessage = "Optional parameter dictionary")]
        [DefaultValue("@{}")]
        [ExampleValue("With parameters", "@{min = 5; max = 15}")]
        public IDictionary<string, object> Parameters { get; set; }

        [ExampleValue("With Parameters", "")]
        [Parameter(HelpMessage = "If set, no action is taken.")]
        public Switch WhatIf { get; set; }

        public abstract void Execute();
    }



    [ParameterSet("Name", HelpMessage = "Executes command given a name.")]
    [Example("With parameters", HelpMessage = "This is an example of how to call a command with a parameter dictionary.")]
    public class ExecuteCommandUsingName : ExecuteCommandBase
    {
        [Parameter(HelpMessage = "Name"), Required]
        [ExampleValue("With parameters", "System")]
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
    [Example("With Path", HelpMessage = "This is an example of how to call a command with a path")]
    public class ExecuteCommandUsingPath : ExecuteCommandBase
    {
        [Parameter(HelpMessage = "The path."), Required]
        [ExampleValue("With Path", @"c:\temp\out.txt")]
        public string Path { get; set; }

        public override void Execute()
        {
            if (this.WhatIf.IsPresent)
                Console.WriteLine("Would have executed using path: {0}", this.Path);
            else
                Console.WriteLine("Executing using path: {0}", this.Path);
        }
    }

    // Examples
    // LBi.Cli.Arguments.Sample.exe -Name test -WhatIf -Parameters @{"foo" = "bar"}
    // LBi.Cli.Arguments.Sample.exe -Path "c:\test\foo.txt" -WhatIf -Parameters @{"foo" = "bar"}
    // LBi.Cli.Arguments.Sample.exe -Help -Detailed
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

            Console.ReadLine();

            /*
            *  advanced usage
            */

            // create parameter set collection from types
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));

            // parse the command line arguments
            Parser parser = new Parser();
            NodeSequence nodes = parser.Parse(string.Join(" ", args));

            // resolve parameter set against the parsed node set
            ResolveResult result = sets.Resolve(new ParameterSetBinder(),
                                                new DefaultActivator(),
                                                new IntransigentTypeConverter(),
                                                CultureInfo.InvariantCulture,
                                                nodes);
            if (result.IsMatch)
            {
                paramSet = (ExecuteCommandBase)result.BestMatch.Object;
                paramSet.Execute();
            }
            else
            {
                ErrorWriter errorWriter = new ErrorWriter();
                errorWriter.Write(new ConsoleWriter(Console.Error), result);

                HelpWriter helpWriter = new HelpWriter();
                helpWriter.Write(new ConsoleWriter(Console.Out), sets, HelpLevel.Parameters);
            }
        }
    }
}
