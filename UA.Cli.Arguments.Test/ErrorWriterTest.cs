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

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using UA.Cli.Arguments.Binding;
using UA.Cli.Arguments.Output;
using UA.Cli.Arguments.Parsing;
using Xunit;

namespace UA.Cli.Arguments.Test
{
    public class ErrorWriterTest
    {
        [ParameterSet("Set1", HelpMessage = "Test set")]
        public class ParamSet
        {
            [Parameter(HelpMessage = "A bool"), Required]
            public bool ABool { get; set; }
        }

        [Fact]
        public void MissingParameterTest()
        {
            ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ParamSet));
            NodeSequence args = this.Parse("");
            ResolveResult result = sets.Resolve(new ParameterSetBinder(),
                                                DefaultActivator.Instance,
                                                new IntransigentTypeConverter(),
                                                CultureInfo.InvariantCulture,
                                                args);

            Assert.False(result.IsMatch, "Expects no match");
            Assert.Equal(1, result.Count);
            Assert.Equal(1, result.BestMatch.Errors.Length);
            ErrorWriter writer = new ErrorWriter();
            using (StringWriter strWriter = new StringWriter())
            {
                writer.Write(strWriter, result);
                string output = strWriter.ToString();
                Assert.Contains("Missing requried paramter: 'ABool'.", output);
                Assert.Contains("A bool", output);
                Assert.Contains("Required?                True", output);
                Assert.Contains("Position?                named", output);
                Assert.Contains("Default value            None", output);
                Assert.Contains("Parameter sets           Set1", output);
            }
        }

        private NodeSequence Parse(string arg)
        {
            Parser parser = new Parser();
            return parser.Parse(arg);
        }
    }
}

