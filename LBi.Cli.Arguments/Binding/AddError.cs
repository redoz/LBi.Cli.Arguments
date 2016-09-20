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
using LBi.Cli.Arguments.Parsing.Ast;

namespace LBi.Cli.Arguments.Binding
{
    public class AddError : ValueError
    {
        public MethodInfo Method { get; protected set; }
        public object[] Parameters { get; protected set; }
        public AstNode[] AstNodes { get; protected set; }
        public Exception Exception { get; protected set; }

        public AddError(MethodInfo method, object[] parameters, AstNode[] astNodes, Exception exception)
        {
            this.Exception = exception;
            this.Method = method;
            this.Parameters = parameters;
            this.AstNodes = astNodes;
        }
    }
}
