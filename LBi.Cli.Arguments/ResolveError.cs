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
using LBi.Cli.Arguments.Parsing.Ast;

namespace LBi.Cli.Arguments
{
    public class ResolveError
    {
        public ResolveError(ErrorType type, IEnumerable<Parameter> parameter, AstNode[] nodes, string message)
        {
            this.Type = type;
            this.Parameter = parameter.ToArray();
            this.Argument = nodes;
            this.Message = message;
        }
        public ErrorType Type { get; protected set; }
        public Parameter[] Parameter { get; protected set; }
        public AstNode[] Argument { get; protected set; }
        public string Message { get; protected set; }
    }
}