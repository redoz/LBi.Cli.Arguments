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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using LBi.Cli.Arguments.Parsing;

namespace LBi.Cli.Arguments
{
    public class ParameterSetCollection
    {
        private List<ParameterSet> _sets;

        public ParameterSetCollection()
        {
            this._sets = new List<ParameterSet>();
        }

        public ParameterSetCollection(IEnumerable<ParameterSet> sets) 
        {
            this._sets = new List<ParameterSet>(sets);
        }

        public static ParameterSetCollection FromTypes(params Type[] types)
        {
            ParameterSetCollection ret = new ParameterSetCollection();
            foreach (Type t in types)
                ret.Add(ParameterSet.FromType(t));

            return ret;
        }

        public int Count
        {
            get { return this._sets.Count; }
        }

        public void Add(ParameterSet set)
        {
            this._sets.Add(set);
        }

        public void Remove(ParameterSet set)
        {
            this._sets.Remove(set);
        }


        public bool TryResolve(IEnumerable<ParsedArgument> args, out ParameterSet set, out IEnumerable<ResolveError> errors)
        {
            ParsedArgument[] arguments =  args.ToArray();
            ParsedArgument[] positionalArgs = arguments.TakeWhile(a => a.ParameterName == null).ToArray();
            ParsedArgument[] namedArguments = arguments.Skip(positionalArgs.Length).ToArray();

            Dictionary<ParameterSet, Dictionary<Parameter, List<ResolveError>>> allErrors = new Dictionary<ParameterSet, Dictionary<Parameter, List<ResolveError>>>();

            if (positionalArgs.Length > 0)
            {
                foreach (ParameterSet paramSet in this._sets)
                {
                    var positionalParams = paramSet.PositionalParameters.ToArray();

                    Dictionary<Parameter, List<ResolveError>> paramSetErrors = new Dictionary<Parameter, List<ResolveError>>();
                    allErrors.Add(paramSet, paramSetErrors);
                    
                    for (int argNum = 0; argNum < positionalArgs.Length && argNum < positionalParams.Length; argNum++)
                    {
                        if (CanCoerece(positionalArgs[argNum], positionalParams[argNum]))
                        {
                            
                        } else
                        {
                            List<ResolveError> paramErrors;
                            if (!paramSetErrors.TryGetValue(positionalParams[argNum], out paramErrors))
                                paramSetErrors.Add(positionalParams[argNum], paramErrors = new List<ResolveError>());

                            paramErrors.Add(new ResolveError {Type = ErrorType.IncompatibleType});
                        }
                    }

                }
            }

            // TODO FIX THIS
            set = null;
            errors = null;
            return false;
        }


        protected virtual bool CanCoerece(ParsedArgument sourceArg, Parameter targetParam)
        {
            return false;
            //bool success = true;
            //var converter = TypeDescriptor.GetConverter(targetType);
            //// this try/catch isn't very nice, but it will do for now
            //// TODO add cache for input+targetType => success
            //try
            //{
            //    converter.ConvertFromInvariantString(input);
            //} catch (NotSupportedException)
            //{
            //    success = false;
            //}
        }
    }

    public class ResolveError
    {
        public ErrorType Type { get; set; }
    }

    public enum ErrorType
    {
        IncompatibleType,
        MissingRequiredParameter,
        ArgumentNameMismatch,
        ArgumentPositionMismatch
    }
}
