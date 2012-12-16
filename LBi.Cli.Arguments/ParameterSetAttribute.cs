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
using LBi.Cli.Arguments.Globalization;

namespace LBi.Cli.Arguments
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ParameterSetAttribute : Attribute, IHelpMessage
    {
        public ParameterSetAttribute(string parameterSet)
        {
            this.Name = parameterSet;
        }

        public string Name { get; protected set; }
        
        public string Command { get; set; }

        public string HelpMessage { get; set; }
        public string HelpMessageResourceName { get; set; }
        public Type HelpMessageResourceType { get; set; }
    }
}
