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

namespace UA.Cli.Arguments.Parsing.Ast
{
    public class Sequence : AstNode
    {
        public Sequence(ISourceInfo sourceInfo, IEnumerable<AstNode> elements) : base(NodeType.Sequence, sourceInfo)
        {
            this.Elements = elements.ToArray();
        }

        public AstNode[] Elements { get; protected set; }

        public override object Visit(IAstVisitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return "[" + string.Join(", ", this.Elements.Select(a => a.ToString())) + "]";
        }
    }
}

