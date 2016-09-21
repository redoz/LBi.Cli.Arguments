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
using System.Globalization;
using System.Linq;
using LBi.Cli.Arguments.Binding;
using LBi.Cli.Arguments.Parsing;
using LBi.Cli.Arguments.Parsing.Ast;
using LBi.Cli.Arguments.Resources;

namespace LBi.Cli.Arguments
{
    public class ParameterSetBinder : IParameterSetBinder
    {
        protected class BuildContext
        {
            public BuildContext(NodeSequence sequence, ParameterSet paramSet, object instance)
            {
                this.Sequence = sequence;
                this.Instance = instance;
                this.ParameterSet = paramSet;
                this.Errors = new List<BindError>();
                this.RemainingParameters = new List<Parameter>(paramSet.OrderBy(p => p.Position.HasValue ? p.Position.Value : int.MinValue));
            }

            public NodeSequence Sequence { get; protected set; }

            public List<Parameter> RemainingParameters { get; protected set; }

            public List<BindError> Errors { get; protected set; }

            public ParameterSet ParameterSet { get; protected set; }

            public object Instance { get; protected set; }
        }

        public virtual ParameterSetResult Build(ITypeActivator typeActivator,
                                                ITypeConverter typeConverter,
                                                CultureInfo cultureInfo,
                                                NodeSequence sequence,
                                                ParameterSet paramSet)
        {
            BuildContext ctx = new BuildContext(sequence,
                                                paramSet,
                                                typeActivator.CreateInstance(paramSet.UnderlyingType));

            var requiredParameters = new HashSet<Parameter>(paramSet.Where(p => p.GetAttribute<RequiredAttribute>() != null));

            SetDefaultValues(typeConverter, cultureInfo, ctx);

            bool first = true;

            using (IEnumerator<AstNode> enumerator = sequence.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (first && !string.IsNullOrEmpty(ctx.ParameterSet.Command))
                    {
                        LiteralValue literal = enumerator.Current as LiteralValue;
                        if (literal == null || !StringComparer.InvariantCultureIgnoreCase.Equals(literal.Value, ctx.ParameterSet.Command))
                        {
                            // report error: missing/incorrect command
                            ctx.Errors.Add(new BindError(ErrorType.MissingCommand,
                                                         Enumerable.Empty<Parameter>(),
                                                         new AstNode[] { literal },
                                                         string.Format(ErrorMessages.MissingCommand, ctx.ParameterSet.Command)));
                        }
                        if (!enumerator.MoveNext())
                            break;
                    }
                    first = false;

                    if (enumerator.Current.Type == NodeType.Parameter || enumerator.Current.Type == NodeType.Switch)
                    {
                        ParameterName parameterName = (ParameterName)enumerator.Current;
                        Parameter[] parameters;
                        if (ctx.ParameterSet.TryGetParameter(parameterName.Name, out parameters) && parameters.Length == 1)
                        {
                            // remove from "remaining parameters" collection
                            if (!ctx.RemainingParameters.Remove(parameters[0]))
                            {
                                // report error, multiple bindings
                                string message = string.Format(ErrorMessages.MultipleBindings,
                                                               parameterName.Name);

                                ctx.Errors.Add(new BindError(ErrorType.MultipleBindings,
                                                             parameters,
                                                             new AstNode[] { parameterName },
                                                             message));

                                // if the parameter was not of type Switch, skip next node
                                if (parameters[0].Property.PropertyType != typeof(Switch))
                                    enumerator.MoveNext();

                                // handled, continue and skip
                                continue;
                            }

                            // if it's a Switch we can simply set it to "Present"
                            if (parameters[0].Property.PropertyType == typeof(Switch))
                            {
                                if (enumerator.Current.Type == NodeType.Switch)
                                {
                                    SwitchParameter switchParameter = (SwitchParameter)enumerator.Current;
                                    SetPropertyValue(ctx, parameters[0], switchParameter.Value);
                                }
                                else
                                    parameters[0].Property.SetValue(ctx.Instance, Switch.Present, null);

                                requiredParameters.Remove(parameters[0]);
                                // handled, continue and skip
                                continue;
                            }

                            // advance to value 
                            if (!enumerator.MoveNext())
                            {
                                string message = string.Format(ErrorMessages.MissingValue, parameters[0].Name);

                                ctx.Errors.Add(new BindError(ErrorType.MissingValue,
                                                             new[] { parameters[0] },
                                                             new[] { parameterName },
                                                             message));
                                break;
                            }

                            SetPropertyValue(ctx, parameters[0], enumerator.Current);
                            requiredParameters.Remove(parameters[0]);
                        }
                        else if (parameters != null && parameters.Length > 1)
                        {
                            // report error, ambigious name
                            string message = string.Format(ErrorMessages.AmbigiousName,
                                                           parameterName.Name,
                                                           string.Join(", ", parameters.Select(p => p.Name)));

                            ctx.Errors.Add(new BindError(ErrorType.AmbigiousName,
                                                         parameters,
                                                         new AstNode[] { parameterName },
                                                         message));
                        }
                        else
                        {
                            // report error, parameter not found
                            string message = string.Format(ErrorMessages.ArgumentNameMismatch, parameterName.Name);

                            ctx.Errors.Add(new BindError(ErrorType.ArgumentNameMismatch,
                                                         Enumerable.Empty<Parameter>(),
                                                         new AstNode[] { parameterName },
                                                         message));
                        }
                    }
                    else
                    {
                        // positional param
                        var positionalParam = ctx.RemainingParameters.FirstOrDefault(p => p.Position.HasValue);

                        if (positionalParam == null)
                        {
                            // report error, there are no positional parameters
                            string message = string.Format(ErrorMessages.ArgumentPositionMismatch,
                                                           ctx.Sequence.GetInputString(enumerator.Current.SourceInfo));

                            ctx.Errors.Add(new BindError(ErrorType.ArgumentPositionMismatch,
                                                         Enumerable.Empty<Parameter>(),
                                                         new[] { enumerator.Current },
                                                         message));
                        }
                        else
                        {
                            SetPropertyValue(ctx, positionalParam, enumerator.Current);
                            requiredParameters.Remove(positionalParam);
                        }
                    }
                }
            }

            foreach (Parameter missingParameter in requiredParameters)
            {
                string message = string.Format(ErrorMessages.MissingRequiredParameter, missingParameter.Name);

                ctx.Errors.Add(new BindError(ErrorType.MissingRequiredParameter,
                                             new[] { missingParameter },
                                             null,
                                             message));
            }

            return new ParameterSetResult(sequence, ctx.ParameterSet, ctx.Instance, ctx.Errors);
        }

        private static void SetDefaultValues(ITypeConverter typeConverter, CultureInfo cultureInfo, BuildContext ctx)
        {
            // set default values
            foreach (Parameter parameter in ctx.ParameterSet)
            {
                var attr = parameter.GetAttribute<DefaultValueAttribute>();

                if (attr == null)
                    continue;

                string strVal = attr.Value as string;
                if (strVal != null)
                {
                    if (parameter.Property.PropertyType != typeof(string))
                    {
                        Parser parser = new Parser();
                        NodeSequence seq = parser.Parse(strVal);
                        using (ValueBuilder valueBuilder = new ValueBuilder())
                        {
                            object value;
                            if (valueBuilder.Build(parameter.Property.PropertyType, seq[0], out value))
                            {
                                parameter.Property.SetValue(ctx.Instance, value, null);
                            }
                            else
                            {
                                string message = string.Format(Exceptions.FailedToParseDefaultValue,
                                                               strVal,
                                                               parameter.Property.PropertyType.FullName);

                                throw new ParameterDefinitionException(parameter.Property, message);
                            }
                        }
                    }
                    else
                    {
                        parameter.Property.SetValue(ctx.Instance, strVal, null);
                    }
                }
                else
                {
                    object value = attr.Value;
                    if (value == null)
                    {
                        if (!parameter.Property.PropertyType.IsValueType)
                        {
                            parameter.Property.SetValue(ctx.Instance, value, null);
                        }
                        else
                        {
                            string message = string.Format(Exceptions.FailedToConvertDefaultValue,
                                                           "NULL",
                                                           "",
                                                           parameter.Property.PropertyType.FullName);
                            throw new ParameterDefinitionException(parameter.Property, message);
                        }
                    }
                    else
                    {
                        IEnumerable<Exception> exceptions;
                        if (typeConverter.TryConvertType(cultureInfo, parameter.Property.PropertyType, ref value, out exceptions))
                        {
                            parameter.Property.SetValue(ctx.Instance, value, null);
                        }
                        else
                        {
                            string message = string.Format(Exceptions.FailedToConvertDefaultValue,
                                                           value,
                                                           value.GetType().FullName,
                                                           parameter.Property.PropertyType.FullName);

                            throw new ParameterDefinitionException(parameter.Property, message);
                        }
                    }
                }
            }
        }

        private static void SetPropertyValue(BuildContext ctx, Parameter parameter, AstNode astNode)
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                if (builder.Build(parameter.Property.PropertyType, astNode, out value))
                {
                    parameter.Property.SetValue(ctx.Instance, value, null);
                }
                else
                {
                    foreach (ValueError valueError in builder.Errors)
                    {
                        TypeError typeError = valueError as TypeError;
                        if (typeError != null)
                        {
                            string message = string.Format(ErrorMessages.IncompatibleType,
                                                           ctx.Sequence.GetInputString(typeError.AstNode.SourceInfo),
                                                           parameter.Name);

                            ctx.Errors.Add(new BindError(ErrorType.IncompatibleType,
                                                         new[] { parameter },
                                                         new[] { astNode },
                                                         message));
                        }

                        AddError addError = valueError as AddError;

                        if (addError != null)
                        {
                            string message = string.Format(ErrorMessages.MethodInvocationFailed,
                                                           ctx.Sequence.GetInputString(addError.AstNodes.Select(ast => ast.SourceInfo)),
                                                           parameter.Name,
                                                           addError.Method.ReflectedType.Name,
                                                           addError.Method.Name,
                                                           addError.Exception.GetType().Name,
                                                           addError.Exception.Message);

                            ctx.Errors.Add(new BindError(ErrorType.IncompatibleType,
                                                         new[] { parameter },
                                                         new[] { astNode },
                                                         message));
                        }
                        else
                        {
                            ActivationError activationError = valueError as ActivationError;

                            if (activationError != null)
                            {
                                string message = string.Format(ErrorMessages.ObjectInitializationFailed,
                                                               ctx.Sequence.GetInputString(activationError.AstNodes.Select(ast => ast.SourceInfo)),
                                                               parameter.Name,
                                                               activationError.Constructor.ReflectedType.Name,
                                                               activationError.Exception.GetType().Name,
                                                               activationError.Exception.Message);

                                ctx.Errors.Add(new BindError(ErrorType.IncompatibleType,
                                                             new[] { parameter },
                                                             new[] { astNode },
                                                             message));
                            }
                        }
                    }
                }
            }
        }
    }
}
