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
using System.ComponentModel.DataAnnotations;
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


        public ILookup<ParameterSet, ResolveError> Resolve(ArgumentCollection args)
        {
            ParsedArgument[] positionalArgs = args.Arguments.Where(a => a.Name == null).ToArray();
            ParsedArgument[] namedArguments = args.Arguments.Where(a => a.Name != null).ToArray();

            Dictionary<ParameterSet, List<ResolveError>> allErrors = new Dictionary<ParameterSet, List<ResolveError>>();
            object[] instances = new object[this._sets.Count];


            for (int setNum = 0; setNum < this._sets.Count; setNum++)
            {
                ParameterSet paramSet = this._sets[setNum];

                instances[setNum] = Activator.CreateInstance(paramSet.UnderlyingType);

                List<Parameter> potentialParams = new List<Parameter>(paramSet);

                var positionalParams = paramSet.PositionalParameters.ToArray();

                List<ResolveError> paramSetErrors = new List<ResolveError>();
                allErrors.Add(paramSet, paramSetErrors);

                // check named arguments
                for (int argNum = 0; argNum < namedArguments.Length; argNum++)
                {
                    Parameter[] matchingParams = paramSet.Where(
                        p => p.Name.StartsWith(namedArguments[argNum].Name,
                                               StringComparison.
                                                   InvariantCultureIgnoreCase)).ToArray();

                    if (matchingParams.Length == 0)
                    {
                        paramSetErrors.Add(
                            new ResolveError(ErrorType.ArgumentNameMismatch,
                                             null,
                                             namedArguments[argNum],
                                             string.Format(Resources.ErrorMessages.ArgumentNameMismatch,
                                                           namedArguments[argNum].Name)));
                    }
                    else if (matchingParams.Length > 1)
                    {
                        paramSetErrors.Add(
                            new ResolveError(ErrorType.AmbigiousName,
                                             matchingParams,
                                             namedArguments[argNum],
                                             String.Format(Resources.ErrorMessages.AmbigiousName,
                                                           namedArguments[argNum].Name,
                                                           string.Join(", ", matchingParams.Select(p => p.Name)))));
                    }
                    else
                    {
                        // TODO assign value
                        if (!this.TryAssignValue(instances[setNum], matchingParams[0], namedArguments[argNum]))
                        {
                            paramSetErrors.Add(
                                new ResolveError(ErrorType.IncompatibleType,
                                                 matchingParams,
                                                 namedArguments[argNum],
                                                 String.Format(Resources.ErrorMessages.IncompatibleType,
                                                               args.GetArgumentString(namedArguments[argNum]),
                                                               matchingParams[0].Name)));
                        }
                        
                        potentialParams.Remove(matchingParams[0]);
                    }
                }

                // check positional arguments against positional parameters
                for (int argNum = 0; argNum < positionalArgs.Length && argNum < positionalParams.Length; argNum++)
                {
                    if (!CanCoerece(positionalArgs[argNum], positionalParams[argNum]))
                    {
                        paramSetErrors.Add(new ResolveError(ErrorType.IncompatibleType,
                                                            positionalParams[argNum],
                                                            positionalArgs[argNum],
                                                            String.Format(Resources.ErrorMessages.IncompatibleType,
                                                                          positionalParams[argNum].Name)));
                    }
                    else
                    {
                        // remove from potential matches
                        potentialParams.Remove(positionalParams[argNum]);
                    }
                }

                // check if there are more positional arguments than parameters
                for (int argNum = positionalParams.Length; argNum < positionalArgs.Length; argNum++)
                {
                    paramSetErrors.Add(new ResolveError(ErrorType.ArgumentPositionMismatch,
                                                        null,
                                                        positionalArgs[argNum],
                                                        string.Format(Resources.ErrorMessages.ArgumentPositionMismatch)));
                }

                ValidationContext validationContext = new ValidationContext(instances[setNum], null, null);

                List<ValidationResult> validationResults = new List<ValidationResult>();
                bool valid = Validator.TryValidateObject(instances[setNum], validationContext, validationResults, true);

                if (!valid)
                {
                    foreach (ValidationResult validationResult in validationResults)
                    {
                        paramSetErrors.Add(new ResolveError(ErrorType.Validation,
                                                                validationResult.MemberNames.Select(n => paramSet[n]),
                                                                null,
                                                                validationResult.ErrorMessage));
                    }
                }

                for (int paramNum = 0; paramNum < paramSet.Count; paramNum++)
                {
                    foreach (var validator in paramSet[paramNum].Validators)
                    {
                        object paramValue = paramSet[paramNum].Property.GetValue(instances[setNum], null);
                        var res = validator.GetValidationResult(paramValue,
                                                      validationContext);

                        if (!validator.IsValid(null))
                        {
                            paramSetErrors.Add(new ResolveError(ErrorType.Validation,
                                                                paramSet[paramNum],
                                                                null,
                                                                res.ErrorMessage));
                        }
                    }
                }
            }


            return
                allErrors.SelectMany(
                    kvp => kvp.Value.Select(v => new KeyValuePair<ParameterSet, ResolveError>(kvp.Key, v)))
                    .ToLookup(kvp => kvp.Key, kvp => kvp.Value);

        }

        private bool TryAssignValue(object instance, Parameter parameter, ParsedArgument argument)
        {
            ValueBuilder builder = new ValueBuilder();
            object value = builder.Build(parameter.Property.PropertyType, argument.Value);
            parameter.Property.SetValue(instance, value, null);
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
}
