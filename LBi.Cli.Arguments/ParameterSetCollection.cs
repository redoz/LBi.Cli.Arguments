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
using System.Reflection;
using System.Runtime.Serialization;
using LBi.Cli.Arguments.Binding;
using LBi.Cli.Arguments.Parsing;
using LBi.Cli.Arguments.Parsing.Ast;

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
                DiscoverTypes(ret, t);

            return ret;
        }

        private static void DiscoverTypes(ParameterSetCollection paramSets, Type curType)
        {
            if (!paramSets.Contains(curType))
                return;

            if (!curType.IsAbstract)
                paramSets.Add(ParameterSet.FromType(curType));

            var attrs = Attribute.GetCustomAttributes(curType, typeof(KnownTypeAttribute), true);
            foreach (KnownTypeAttribute knownType in attrs)
            {
                DiscoverTypes(paramSets, knownType.Type);
            }
        }

        public bool Contains(string name)
        {
            return this._sets.Any(s => StringComparer.InvariantCultureIgnoreCase.Equals(s.Name, name));
        }

        public bool Contains(Type type)
        {
            return this._sets.Any(s => s.UnderlyingType == type);
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

        public ResolveResult Resolve(NodeSequence sequence)
        {
            List<ParameterSetResult> setResults = new List<ParameterSetResult>();

            for (int setNum = 0; setNum < this._sets.Count; setNum++)
            {
                setResults.Add(BuildParameterSet(sequence, this._sets[setNum]));
            }

            return new ResolveResult(setResults);
        }

        private static ParameterSetResult BuildParameterSet(NodeSequence sequence, ParameterSet paramSet)
        {
            object setInstance = Activator.CreateInstance(paramSet.UnderlyingType);
            List<ResolveError> paramSetErrors = new List<ResolveError>();

            using (IEnumerator<AstNode> enumerator = sequence.GetEnumerator())
            {

                if (enumerator.Current.Type == NodeType.Parameter)
                {
                    ParameterName parameterName = (ParameterName)enumerator.Current;
                    Parameter[] parameters;
                    if (paramSet.TryGetParameter(parameterName.Name, out parameters) && parameters.Length == 1)
                    {
                        //using (ValueBuilder builder = new ValueBuilder())
                        //{
                        //    Parameter curParam = parameters[0];
                        //    PropertyInfo paramProp = curParam.Property;
                        //    object value;
                        //    if (builder.Build(paramProp.PropertyType, namedArguments[argNum].Value, out value))
                        //    {
                        //        paramProp.SetValue(instances[setNum], value, null);
                        //    }
                        //    else
                        //    {
                        //        foreach (ValueError valueError in builder.Errors)
                        //        {
                        //            TypeError typeError = valueError as TypeError;
                        //            if (typeError != null)
                        //            {
                        //                paramSetErrors.Add(
                        //                    new ResolveError(ErrorType.IncompatibleType,
                        //                                     matchingParams,
                        //                                     namedArguments[argNum],
                        //                                     String.Format(Resources.ErrorMessages.IncompatibleType,
                        //                                                   args.GetInputString(
                        //                                                       typeError.AstNode.SourceInfo),
                        //                                                   matchingParams[0].Name)));
                        //            }

                        //            InvokeError invokeError = valueError as InvokeError;

                        //            if (invokeError != null)
                        //            {
                        //                if (invokeError.Method != null)
                        //                {
                        //                    paramSetErrors.Add(
                        //                        new ResolveError(ErrorType.IncompatibleType,
                        //                                         matchingParams,
                        //                                         namedArguments[argNum],
                        //                                         String.Format(Resources.ErrorMessages.MethodInvocationFailed,
                        //                                                       args.GetInputString(invokeError.AstNodes.Select(ast => ast.SourceInfo)),
                        //                                                       matchingParams[0].Name,
                        //                                                       invokeError.Method.ReflectedType.Name,
                        //                                                       invokeError.Method.Name,
                        //                                                       invokeError.Exception.GetType().Name,
                        //                                                       invokeError.Exception.Message)));
                        //                }
                        //                else
                        //                {
                        //                    paramSetErrors.Add(
                        //                        new ResolveError(ErrorType.IncompatibleType,
                        //                                         matchingParams,
                        //                                         namedArguments[argNum],
                        //                                         String.Format(Resources.ErrorMessages.ObjectInitializationFailed,
                        //                                                       args.GetInputString(invokeError.AstNodes.Select(ast => ast.SourceInfo)),
                        //                                                       matchingParams[0].Name,
                        //                                                       invokeError.Constructor.ReflectedType.Name,
                        //                                                       invokeError.Exception.GetType().Name,
                        //                                                       invokeError.Exception.Message)));
                        //                }
                        //            }
                        //        }
                        //    }
                        //}
                    }
                    else if (parameters != null && parameters.Length > 1)
                    {
                        // report error, ambigious name
                        paramSetErrors.Add(
                            new ResolveError(ErrorType.AmbigiousName,
                                             parameters,
                                             new[] { parameterName },
                                             String.Format(Resources.ErrorMessages.AmbigiousName,
                                                           parameterName.Name,
                                                           string.Join(", ", parameters.Select(p => p.Name)))));
                    }
                    else
                    {
                        // report error, parameter not found
                        paramSetErrors.Add(
                            new ResolveError(ErrorType.ArgumentNameMismatch,
                                             Enumerable.Empty<Parameter>(),
                                             new[] { parameterName },
                                             string.Format(Resources.ErrorMessages.ArgumentNameMismatch,
                                                           parameterName.Name)));
                    }
                }
                else
                {
                    // positional param
                }

            }

            return new ParameterSetResult(sequence, paramSet, setInstance, paramSetErrors);
        }

        //public ResolveResult Resolve(NodeSequence args)
        //{
        //    //ParsedArgument[] positionalArgs = args.Sequence.Where(a => a.Name == null).ToArray();
        //    //ParsedArgument[] namedArguments = args.Sequence.Where(a => a.Name != null).ToArray();

        //    //Dictionary<ParameterSet, List<ResolveError>> allErrors = new Dictionary<ParameterSet, List<ResolveError>>();
        //    object[] instances = new object[this._sets.Count];

        //    List<ParameterSetResult> setResults = new List<ParameterSetResult>();
        //    for (int setNum = 0; setNum < this._sets.Count; setNum++)
        //    {
        //        ParameterSet paramSet = this._sets[setNum];

        //        instances[setNum] = Activator.CreateInstance(paramSet.UnderlyingType);

        //        List<Parameter> potentialParams = new List<Parameter>(paramSet);

        //        var positionalParams = paramSet.PositionalParameters.ToArray();

        //        List<ResolveError> paramSetErrors = new List<ResolveError>();
        //        //allErrors.Add(paramSet, paramSetErrors);

        //        // check named arguments
        //        for (int argNum = 0; argNum < namedArguments.Length; argNum++)
        //        {
        //            Parameter[] matchingParams = paramSet.Where(
        //                p => p.Name.StartsWith(namedArguments[argNum].Name,
        //                                       StringComparison.
        //                                           InvariantCultureIgnoreCase)).ToArray();

        //            if (matchingParams.Length == 0)
        //            {
        //                paramSetErrors.Add(
        //                    new ResolveError(ErrorType.ArgumentNameMismatch,
        //                                     Enumerable.Empty<Parameter>(),
        //                                     namedArguments[argNum],
        //                                     string.Format(Resources.ErrorMessages.ArgumentNameMismatch,
        //                                                   namedArguments[argNum].Name)));
        //            }
        //            else if (matchingParams.Length > 1)
        //            {
        //                paramSetErrors.Add(
        //                    new ResolveError(ErrorType.AmbigiousName,
        //                                     matchingParams,
        //                                     namedArguments[argNum],
        //                                     String.Format(Resources.ErrorMessages.AmbigiousName,
        //                                                   namedArguments[argNum].Name,
        //                                                   string.Join(", ", matchingParams.Select(p => p.Name)))));
        //            }
        //            else
        //            {
        //                using (ValueBuilder builder = new ValueBuilder())
        //                {
        //                    PropertyInfo paramProp = matchingParams[0].Property;
        //                    object value;
        //                    if (builder.Build(paramProp.PropertyType, namedArguments[argNum].Value, out value))
        //                    {
        //                        paramProp.SetValue(instances[setNum], value, null);
        //                    }
        //                    else
        //                    {
        //                        foreach (ValueError valueError in builder.Errors)
        //                        {
        //                            TypeError typeError = valueError as TypeError;
        //                            if (typeError != null)
        //                            {
        //                                paramSetErrors.Add(
        //                                    new ResolveError(ErrorType.IncompatibleType,
        //                                                     matchingParams,
        //                                                     namedArguments[argNum],
        //                                                     String.Format(Resources.ErrorMessages.IncompatibleType,
        //                                                                   args.GetInputString(
        //                                                                       typeError.AstNode.SourceInfo),
        //                                                                   matchingParams[0].Name)));
        //                            }

        //                            InvokeError invokeError = valueError as InvokeError;

        //                            if (invokeError != null)
        //                            {
        //                                if (invokeError.Method != null)
        //                                {
        //                                    paramSetErrors.Add(
        //                                        new ResolveError(ErrorType.IncompatibleType,
        //                                                         matchingParams,
        //                                                         namedArguments[argNum],
        //                                                         String.Format(Resources.ErrorMessages.MethodInvocationFailed,
        //                                                                       args.GetInputString(invokeError.AstNodes.Select(ast => ast.SourceInfo)),
        //                                                                       matchingParams[0].Name,
        //                                                                       invokeError.Method.ReflectedType.Name,
        //                                                                       invokeError.Method.Name,
        //                                                                       invokeError.Exception.GetType().Name,
        //                                                                       invokeError.Exception.Message)));
        //                                }
        //                                else
        //                                {
        //                                    paramSetErrors.Add(
        //                                        new ResolveError(ErrorType.IncompatibleType,
        //                                                         matchingParams,
        //                                                         namedArguments[argNum],
        //                                                         String.Format(Resources.ErrorMessages.ObjectInitializationFailed,
        //                                                                       args.GetInputString(invokeError.AstNodes.Select(ast => ast.SourceInfo)),
        //                                                                       matchingParams[0].Name,
        //                                                                       invokeError.Constructor.ReflectedType.Name,
        //                                                                       invokeError.Exception.GetType().Name,
        //                                                                       invokeError.Exception.Message)));
        //                                }
        //                            }
        //                        }
        //                    }
        //                }

        //                potentialParams.Remove(matchingParams[0]);
        //            }
        //        }

        //        // check positional arguments against positional parameters
        //        for (int argNum = 0; argNum < positionalArgs.Length && argNum < positionalParams.Length; argNum++)
        //        {
        //            using (ValueBuilder builder = new ValueBuilder())
        //            {
        //                PropertyInfo paramProp = positionalParams[argNum].Property;
        //                object value;
        //                if (builder.Build(paramProp.PropertyType, namedArguments[argNum].Value, out value))
        //                {
        //                    paramProp.SetValue(instances[setNum], value, null);
        //                }
        //                else
        //                {
        //                    foreach (TypeError typeError in builder.Errors)
        //                    {
        //                        paramSetErrors.Add(
        //                            new ResolveError(ErrorType.IncompatibleType,
        //                                             new[] {positionalParams[argNum]},
        //                                             namedArguments[argNum],
        //                                             String.Format(Resources.ErrorMessages.IncompatibleType,
        //                                                           args.GetInputString(typeError.AstNode.SourceInfo),
        //                                                           positionalParams[argNum].Name)));

        //                    }
        //                }
        //            }
        //        }

        //        // check if there are more positional arguments than parameters
        //        for (int argNum = positionalParams.Length; argNum < positionalArgs.Length; argNum++)
        //        {
        //            paramSetErrors.Add(new ResolveError(ErrorType.ArgumentPositionMismatch,
        //                                                Enumerable.Empty<Parameter>(),
        //                                                positionalArgs[argNum],
        //                                                string.Format(Resources.ErrorMessages.ArgumentPositionMismatch)));
        //        }

        //        ValidationContext validationContext = new ValidationContext(instances[setNum], null, null);

        //        List<ValidationResult> validationResults = new List<ValidationResult>();
        //        bool valid = Validator.TryValidateObject(instances[setNum], validationContext, validationResults, true);

        //        if (!valid)
        //        {
        //            foreach (ValidationResult validationResult in validationResults)
        //            {
        //                paramSetErrors.Add(new ResolveError(ErrorType.Validation,
        //                                                        validationResult.MemberNames.Select(n => paramSet[n]),
        //                                                        null,
        //                                                        validationResult.ErrorMessage));
        //            }
        //        }

        //        setResults.Add(new ParameterSetResult(args, paramSet, instances[setNum], paramSetErrors));
        //    }


        //    return new ResolveResult(setResults);
        //}
    }
}
