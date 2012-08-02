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

namespace LBi.Cli.Arguments.Parsing.Ast
{
    public class LiteralValue : AstNode
    {
        public LiteralValue(ISourceInfo sourceInfo, LiteralValueType type, string value)
            : base(sourceInfo)
        {
            this.Value = value;
            this.Type = type;
        }

        public LiteralValueType Type { get; protected set; }

        public string Value { get; protected set; }

        public override void Visit(IAstVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
