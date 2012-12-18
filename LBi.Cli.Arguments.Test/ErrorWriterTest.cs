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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LBi.Cli.Arguments.Binding;
using LBi.Cli.Arguments.Output;
using LBi.Cli.Arguments.Parsing;
using Xunit;

namespace LBi.Cli.Arguments.Test
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
                                                new IntransigentTypeConverter(),
                                                CultureInfo.InvariantCulture, args);

            Assert.False(result.IsMatch, "Expects no match");
            Assert.Equal(1, result.Count);
            Assert.Equal(1, result.BestMatch.Errors.Length);
            ErrorWriter writer = new ErrorWriter();
            using (StringWriter strWriter = new StringWriter())
            {
                writer.Write(strWriter, result);
                Assert.Equal(
@"Missing requried paramter: 'ABool'.
-ABool <Boolean>

   A bool

   Required?                True
   Position?                named
   Default value            None
   Parameter sets           Set1
", strWriter.ToString());
            }
        }

        private NodeSequence Parse(params string[] arg)
        {
            Parser parser = new Parser();
            return parser.Parse(string.Join(" ", arg));
        }
    }
}
