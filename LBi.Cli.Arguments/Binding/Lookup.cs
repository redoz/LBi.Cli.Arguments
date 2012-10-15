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

namespace LBi.Cli.Arguments.Binding
{
    internal class Lookup<TKey, TValue> : ILookup<TKey, TValue>
    {
        private readonly Dictionary<TKey, List<TValue>> _lookup;

        public Lookup()
        {
            this._lookup = new Dictionary<TKey, List<TValue>>();
        }

        public void Add(TKey key, TValue value)
        {
            List<TValue> list;
            if (!this._lookup.TryGetValue(key, out list))
                this._lookup.Add(key, list = new List<TValue>());

            list.Add(value);
        }

        public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator()
        {
            var items =
                this._lookup.SelectMany(kvp => kvp.Value.Select(value => new KeyValuePair<TKey, TValue>(kvp.Key, value)))
                    .GroupBy(pair => pair.Key, pair => pair.Value);
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(TKey key)
        {
            return this._lookup.ContainsKey(key);
        }

        public int Count { get; private set; }

        public IEnumerable<TValue> this[TKey key]
        {
            get { return this._lookup[key].AsEnumerable(); }
        }
    }
}