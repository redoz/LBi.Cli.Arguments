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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using LBi.Cli.Arguments.Binding;
using LBi.Cli.Arguments.Parsing;
using LBi.Cli.Arguments.Parsing.Ast;
using LBi.Cli.Arguments.Resources;

namespace LBi.Cli.Arguments
{
    public class ParameterSetCollection : IEnumerable<ParameterSet>
    {


        protected readonly List<ParameterSet> ParameterSets;

        public ParameterSetCollection()
        {
            this.ParameterSets = new List<ParameterSet>();
        }

        public ParameterSetCollection(IEnumerable<ParameterSet> sets)
        {
            this.ParameterSets = new List<ParameterSet>(sets);
        }

        public static ParameterSetCollection FromTypes(params Type[] types)
        {
            ParameterSetCollection ret = new ParameterSetCollection();
            foreach (Type t in types)
                ret.DiscoverTypes(ret, t);

            return ret;
        }

        protected virtual void DiscoverTypes(ParameterSetCollection paramSets, Type curType)
        {
            if (paramSets.Contains(curType))
                return;

            if (!curType.IsAbstract && Attribute.GetCustomAttribute(curType, typeof(ParameterSetAttribute), true) != null)
                paramSets.Add(ParameterSet.FromType(curType));

            var attrs = Attribute.GetCustomAttributes(curType, typeof(KnownTypeAttribute), true);
            foreach (KnownTypeAttribute knownType in attrs)
            {
                DiscoverTypes(paramSets, knownType.Type);
            }
        }

        public bool Contains(string name)
        {
            return this.ParameterSets.Any(s => StringComparer.InvariantCultureIgnoreCase.Equals(s.Name, name));
        }

        public bool Contains(Type type)
        {
            return this.ParameterSets.Any(s => s.UnderlyingType == type);
        }

        public int Count
        {
            get { return this.ParameterSets.Count; }
        }

        public void Add(ParameterSet set)
        {
            this.ParameterSets.Add(set);
        }

        public void Remove(ParameterSet set)
        {
            this.ParameterSets.Remove(set);
        }


        protected virtual bool TryGetParameter(IEnumerable<Parameter> parameters, string name, out Parameter[] matches)
        {
            matches =
                parameters
                    .Where(p => p.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                    .ToArray();

            return matches.Length == 1;
        }


        public ResolveResult Resolve(IParameterSetBinder binder, ITypeActivator typeActivator, ITypeConverter typeConverter, CultureInfo cultureInfo, NodeSequence sequence)
        {
            List<ParameterSetResult> setResults = new List<ParameterSetResult>();

            for (int setNum = 0; setNum < this.ParameterSets.Count; setNum++)
            {
                setResults.Add(binder.Build(typeActivator,
                                            typeConverter,
                                            cultureInfo,
                                            sequence,
                                            this.ParameterSets[setNum]));
            }

            return new ResolveResult(setResults);
        }

        public IEnumerator<ParameterSet> GetEnumerator()
        {
            return this.ParameterSets.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ParameterSet this[int index]
        {
            get { return this.ParameterSets[index]; }
        }
    }
}
