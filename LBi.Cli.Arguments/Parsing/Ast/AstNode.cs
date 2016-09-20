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

namespace LBi.Cli.Arguments.Parsing.Ast
{
    public abstract class AstNode
    {
        protected AstNode(NodeType nodeType, ISourceInfo sourceInfo)
        {
            if (sourceInfo == null)
                throw new ArgumentNullException("sourceInfo");

            this.Type = nodeType;

            // this is cloned so that we don't end up holding on to the ISourceInfo instance
            this.SourceInfo = new SourceInfoImpl(sourceInfo);
        }

        public NodeType Type { get; protected set; }

        public ISourceInfo SourceInfo { get; protected set; }

        public abstract object Visit(IAstVisitor visitor);

        #region Nested type: SourceInfoImpl

        protected class SourceInfoImpl : ISourceInfo
        {
            public SourceInfoImpl(ISourceInfo cloneFrom)
            {
                if (cloneFrom == null)
                    throw new ArgumentNullException("cloneFrom");

                this.Position = cloneFrom.Position;
                this.Length = cloneFrom.Length;
            }

            #region ISourceInfo Members

            public int Position { get; private set; }

            public int Length { get; private set; }

            #endregion
        }

        #endregion
    }
}
