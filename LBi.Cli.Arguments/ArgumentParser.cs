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
using System.ComponentModel.DataAnnotations;
using System.IO;
using LBi.Cli.Arguments.Output;
using LBi.Cli.Arguments.Parsing;

namespace LBi.Cli.Arguments
{
    public class ArgumentParser<TParamSetBase>
    {
        // TODO put this in resource file
        [ParameterSet("Help", HelpMessage = "Show help")]
        protected class HelpCommand
        {
            [Parameter, Required]
            public SwitchParameter Help { get; set; }

            [Parameter]
            public SwitchParameter Full { get; set; }

            [Parameter]
            public SwitchParameter Detailed { get; set; }

            [Parameter]
            public SwitchParameter Parameters { get; set; }

            [Parameter]
            public SwitchParameter Examples { get; set; }

            public HelpLevel ToHelpLevel()
            {
                HelpLevel ret = 0;
                if (this.Full)
                    ret = HelpLevel.Full;

                if (this.Examples)
                    ret |= HelpLevel.Examples;

                if (this.Detailed)
                    ret |= HelpLevel.Detailed;

                if (this.Parameters)
                    ret |= HelpLevel.Parameters;

                return ret;
            }
        }

        public TextWriter Out { get; set; }

        public TextWriter Error { get; set; }

        protected readonly ParameterSetCollection ParameterSets;

        public ArgumentParser(params Type[] types)
        {
            Array.Resize(ref types, types.Length + 1);
            types[types.Length - 1] = typeof(HelpCommand);
            // create parameter set collection from types
            this.ParameterSets = ParameterSetCollection.FromTypes(types);
            this.Out = Console.Out;
            this.Error = Console.Error;
        }

        public virtual bool TryParse(string[] args, out TParamSetBase paramSet)
        {
            bool success;
            // parse the command line arguments
            Parser parser = new Parser();
            NodeSequence nodes = parser.Parse(string.Join(" ", args));

            // resolve parameter set against the parsed node set
            ResolveResult result = this.ParameterSets.Resolve(nodes);
            if (result.IsMatch && !(result.BestMatch.Object is HelpCommand))
            {
                paramSet = (TParamSetBase)result.BestMatch.Object;
                success = true;
            }
            else if (result.IsMatch && result.BestMatch.Object is HelpCommand)
            {
                HelpWriter helpWriter = new HelpWriter(this.Out);
                helpWriter.Write(this.ParameterSets, ((HelpCommand)result.BestMatch.Object).ToHelpLevel());
                success = false;
                paramSet = default(TParamSetBase);
            }
            else
            {
                paramSet = default(TParamSetBase);
                success = false;
                ErrorWriter errorWriter = new ErrorWriter(this.Error);
                errorWriter.Write(result.BestMatch);
                HelpWriter helpWriter = new HelpWriter(this.Out);
                helpWriter.Write(this.ParameterSets, HelpLevel.Parameters);
            }

            return success;
        }

    }
}
