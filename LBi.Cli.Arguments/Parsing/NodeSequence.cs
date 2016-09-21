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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LBi.Cli.Arguments.Parsing.Ast;

namespace LBi.Cli.Arguments.Parsing
{
    public class NodeSequence : IEnumerable<AstNode>
    {
        public NodeSequence(string input, IEnumerable<AstNode> sequence)
        {
            this.Input = input;
            this.Sequence = sequence.ToArray();
        }

        public string Input { get; protected set; }

        protected readonly AstNode[] Sequence;

        public AstNode this[int index] => this.Sequence[index];

        public int Count => this.Sequence.Length;

        public string GetInputString(IEnumerable<ISourceInfo> sourceInfos)
        {
            var siArray = sourceInfos.OrderBy(si => si.Position)
                                     .ToArray();

            int position = siArray[0].Position;
            int length = siArray[siArray.Length - 1].Position
                         + siArray[siArray.Length - 1].Length
                         - position;

            return this.Input.Substring(position, length);
        }

        public string GetInputString(ISourceInfo sourceInfo)
        {
            return this.Input.Substring(sourceInfo.Position, sourceInfo.Length);
        }

        #region Implementation of IEnumerable

        public IEnumerator<AstNode> GetEnumerator()
        {
            return this.Sequence.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
}
