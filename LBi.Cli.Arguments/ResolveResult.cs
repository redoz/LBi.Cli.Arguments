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

namespace LBi.Cli.Arguments
{
    public class ResolveResult : IEnumerable<ParameterSetResult>
    {
        private readonly ParameterSetResult[] _results;

        public ResolveResult(IEnumerable<ParameterSetResult> results)
        {
            this._results = results.ToArray();
        }

        public ParameterSetResult this[int index]
        {
            get { return this._results[index]; }
        }

        public int Count
        {
            get { return this._results.Length; }
        }

        public bool IsMatch
        {
            get { return this._results.Count(psr => psr.Errors.Length == 0) == 1; }
        }

        public ParameterSetResult BestMatch
        {
            get
            {
                return this._results.OrderBy(r => r.Errors.Length)
                           .FirstOrDefault();
            }
        }

        #region Implementation of IEnumerable

        public IEnumerator<ParameterSetResult> GetEnumerator()
        {
            return ((IEnumerable<ParameterSetResult>)this._results).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
}
