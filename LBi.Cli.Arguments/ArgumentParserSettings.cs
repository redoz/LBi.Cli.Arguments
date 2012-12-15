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
using System.Globalization;
using System.IO;
using LBi.Cli.Arguments.Binding;
using LBi.Cli.Arguments.Output;

namespace LBi.Cli.Arguments
{
    public class ArgumentParserSettings
    {
        public static ArgumentParserSettings Default { get; private set; }

        static ArgumentParserSettings()
        {
            Default = new ArgumentParserSettings
                          {
                              Out = new ConsoleWriter(Console.Out),
                              Error = new ConsoleWriter(Console.Error),
                              HelpWriter = new HelpWriter(),
                              ErrorWriter = new ErrorWriter(),
                              TypeConverter = new IntransigentTypeConverter(),
                              ParameterSetBuilder = new ParameterSetBuilder(),
                              Culture = CultureInfo.InvariantCulture,
                          };
        }

        public TextWriter Out { get; set; }
        public TextWriter Error { get; set; }
        public IHelpWriter HelpWriter { get; set; }
        public IErrorWriter ErrorWriter { get; set; }
        public CultureInfo Culture { get; set; }
        public ITypeConverter TypeConverter { get; set; }
        public IParameterSetBuilder ParameterSetBuilder{ get; set; }
    }
}