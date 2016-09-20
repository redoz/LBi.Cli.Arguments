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
using System.Reflection;

namespace LBi.Cli.Arguments
{
    public class ParameterDefinitionException : Exception
    {
        public ParameterDefinitionException(PropertyInfo parameterProperty, string message)
            : base(WrapMessage(parameterProperty, message))
        {
            this.ParameterProperty = parameterProperty;
        }

        public ParameterDefinitionException(PropertyInfo parameterProperty, string format, params object[] args)
            : this(parameterProperty, string.Format(format, args))
        {
        }

        public ParameterDefinitionException(Exception innerException, PropertyInfo parameterProperty, string message)
            : base(WrapMessage(parameterProperty, message), innerException)
        {
            this.ParameterProperty = parameterProperty;
        }

        public ParameterDefinitionException(Exception innerException, PropertyInfo parameterProperty, string format, params object[] args)
            : this(innerException, parameterProperty, string.Format(format, args))
        {
        }

        private static string WrapMessage(PropertyInfo parameterProperty, string message)
        {
            return string.Format("Error reported when processing property {0} on type {1}: {2}",
                                 parameterProperty.Name,
                                 parameterProperty.ReflectedType.Name,
                                 message);
        }

        public PropertyInfo ParameterProperty { get; protected set; }
    }
}
