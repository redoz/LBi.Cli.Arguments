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

using System.Collections.Generic;
using System.Linq;
using LBi.Cli.Arguments.Parsing;

namespace LBi.Cli.Arguments
{
    public class ParameterSetResult
    {
        public ParameterSetResult(NodeSequence arguments, ParameterSet parameterSet, object setInstance, IEnumerable<BindError> errors)
        {
            this.Arguments = arguments;
            this.ParameterSet = parameterSet;
            this.Object = setInstance;
            this.Errors = errors.ToArray();
        }

        public BindError[] Errors { get; protected set; }

        public object Object { get; protected set; }

        public ParameterSet ParameterSet { get; protected set; }

        public NodeSequence Arguments { get; protected set; }
    }
}
