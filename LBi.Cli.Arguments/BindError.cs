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
using LBi.Cli.Arguments.Parsing.Ast;

namespace LBi.Cli.Arguments
{
    /// <summary>
    ///     Represents a failure to bind a property, created by <see cref="IParameterSetBinder" />
    /// </summary>
    public class BindError
    {
        /// <summary>
        ///     Initilized a new instance of <see cref="BindError" />.
        /// </summary>
        /// <param name="type">The type of error.</param>
        /// <param name="parameter">Which <see cref="Parameter" /> does this relate to.</param>
        /// <param name="nodes">Which <see cref="AstNode" /> does this relate to.</param>
        /// <param name="message">Error message that will be shown to the end-user.</param>
        public BindError(ErrorType type, IEnumerable<Parameter> parameter, AstNode[] nodes, string message)
        {
            this.Type = type;
            this.Parameter = parameter.ToArray();
            this.Argument = nodes;
            this.Message = message;
        }

        /// <summary>
        ///     The type of error.
        /// </summary>
        public ErrorType Type { get; protected set; }

        /// <summary>
        ///     Related paramters.
        /// </summary>
        public Parameter[] Parameter { get; protected set; }

        /// <summary>
        ///     Related Arguments.
        /// </summary>
        public AstNode[] Argument { get; protected set; }

        /// <summary>
        ///     Error message.
        /// </summary>
        public string Message { get; protected set; }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.Type, this.Message);
        }
    }
}
