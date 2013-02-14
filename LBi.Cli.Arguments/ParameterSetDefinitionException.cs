/*
 * Copyright 2012,2013 LBi Netherlands B.V.
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

namespace LBi.Cli.Arguments
{
    public class ParameterSetDefinitionException : Exception
    {
        public Type ParameterSetType { get; protected set; }
        public IEnumerable<Parameter> ParameterProperties { get; protected set; }

        public ParameterSetDefinitionException(Type parameterSetType, IEnumerable<Parameter> parameters, string message)
            : base(WrapMessage(parameterSetType, message))
        {
            this.ParameterSetType = parameterSetType;
            this.ParameterProperties = parameters;
        }

        public ParameterSetDefinitionException(Type parameterSetType, IEnumerable<Parameter> parameters, string format, params object[] args)
            : this(parameterSetType, parameters, string.Format(format, args))
        {
        }

        private static string WrapMessage(Type parameterSetType, string message)
        {
            return string.Format("Error reported when processing parameter set of type {0}: {1}", parameterSetType.Name, message);
        }
    }
}