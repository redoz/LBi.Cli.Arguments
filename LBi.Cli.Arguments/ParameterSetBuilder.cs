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
using System.Text;
using System.Threading.Tasks;
using LBi.Cli.Arguments.Binding;
using LBi.Cli.Arguments.Parsing;
using LBi.Cli.Arguments.Parsing.Ast;
using LBi.Cli.Arguments.Resources;

namespace LBi.Cli.Arguments
{
    public class ParameterSetBuilder : IParameterSetBuilder
    {
        protected class BuildContext
        {
            public BuildContext(NodeSequence sequence, ParameterSet paramSet, object instance)
            {
                this.Sequence = sequence;
                this.Instance = instance;
                this.ParameterSet = paramSet;
                this.Errors = new List<ResolveError>();
                this.RemainingParameters = new List<Parameter>(paramSet.OrderBy(p => p.Position.HasValue ? p.Position.Value : int.MinValue));
            }

            public NodeSequence Sequence { get; protected set; }

            public List<Parameter> RemainingParameters { get; protected set; }

            public List<ResolveError> Errors { get; protected set; }

            public ParameterSet ParameterSet { get; protected set; }

            public object Instance { get; protected set; }
        }

        public virtual ParameterSetResult Build(ITypeConverter typeConverter, CultureInfo cultureInfo, NodeSequence sequence, ParameterSet paramSet)
        {
            BuildContext ctx = new BuildContext(sequence,
                                                paramSet, Activator.CreateInstance(paramSet.UnderlyingType));

            // set default values
            foreach (Parameter parameter in paramSet)
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
                                throw new ParameterDefinitionException(parameter.Property,
                                                                       string.Format(
                                                                           Exceptions.FailedToParseDefaultValue,
                                                                           strVal,
                                                                           parameter.Property.PropertyType.FullName));
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
                    // TODO _maybe_ use some dependency injection for the ITypeConverter
                    object value = attr.Value;
                    if (value == null)
                    {
                        if (!parameter.Property.PropertyType.IsValueType)
                        {
                            parameter.Property.SetValue(ctx.Instance, value, null);
                        }
                        else
                        {
                            throw new ParameterDefinitionException(parameter.Property,
                                                                   string.Format(
                                                                       Exceptions.FailedToConvertDefaultValue,
                                                                       "NULL",
                                                                       "",
                                                                       parameter.Property.PropertyType.FullName));
                        }
                    }
                    else
                    {
                        Exception exception;
                        if (typeConverter.TryConvertType(cultureInfo, parameter.Property.PropertyType, ref value, out exception))
                        {
                            parameter.Property.SetValue(ctx.Instance, value, null);
                        }
                        else
                        {
                            throw new ParameterDefinitionException(parameter.Property,
                                                                   string.Format(
                                                                       Exceptions.FailedToConvertDefaultValue,
                                                                       value,
                                                                       value.GetType().FullName,
                                                                       parameter.Property.PropertyType.FullName));
                        }
                    }
                }
            }

            using (IEnumerator<AstNode> enumerator = sequence.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.Type == NodeType.Parameter ||
                        enumerator.Current.Type == NodeType.Switch)
                    {
                        ParameterName parameterName = (ParameterName)enumerator.Current;
                        Parameter[] parameters;
                        if (ctx.ParameterSet.TryGetParameter(parameterName.Name, out parameters) && parameters.Length == 1)
                        {
                            // remove from "remaining parameters" collection
                            if (!ctx.RemainingParameters.Remove(parameters[0]))
                            {
                                // report error, multiple bindings
                                ctx.Errors.Add(
                                    new ResolveError(ErrorType.MultipleBindings,
                                                     parameters,
                                                     new AstNode[] { parameterName },
                                                     String.Format(ErrorMessages.MultipleBindings,
                                                                   parameterName.Name)));

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

                                // handled, continue and skip
                                continue;
                            }

                            // advance to value 
                            enumerator.MoveNext();

                            SetPropertyValue(ctx, parameters[0], enumerator.Current);
                        }
                        else if (parameters != null && parameters.Length > 1)
                        {
                            // report error, ambigious name
                            ctx.Errors.Add(
                                new ResolveError(ErrorType.AmbigiousName,
                                                 parameters,
                                                 new AstNode[] { parameterName },
                                                 String.Format(ErrorMessages.AmbigiousName,
                                                               parameterName.Name,
                                                               string.Join(", ", parameters.Select(p => p.Name)))));
                        }
                        else
                        {
                            // report error, parameter not found
                            ctx.Errors.Add(
                                new ResolveError(ErrorType.ArgumentNameMismatch,
                                                 Enumerable.Empty<Parameter>(),
                                                 new AstNode[] { parameterName },
                                                 string.Format(ErrorMessages.ArgumentNameMismatch,
                                                               parameterName.Name)));
                        }
                    }
                    else
                    {
                        // positional param
                        var positionalParam = ctx.RemainingParameters.FirstOrDefault(p => p.Position.HasValue);

                        if (positionalParam == null)
                        {
                            // report error, there are no positional parameters
                            ctx.Errors.Add(new ResolveError(ErrorType.ArgumentPositionMismatch,
                                                            Enumerable.Empty<Parameter>(),
                                                            new[] { enumerator.Current },
                                                            string.Format(
                                                                ErrorMessages.ArgumentPositionMismatch,
                                                                ctx.Sequence.GetInputString(enumerator.Current.SourceInfo))));
                        }
                        else
                        {
                            SetPropertyValue(ctx, positionalParam, enumerator.Current);
                        }
                    }
                }
            }

            ValidationContext validationContext = new ValidationContext(ctx.Instance, null, null);

            List<ValidationResult> validationResults = new List<ValidationResult>();
            bool valid = Validator.TryValidateObject(ctx.Instance, validationContext, validationResults, true);

            if (!valid)
            {
                foreach (ValidationResult validationResult in validationResults)
                {
                    ctx.Errors.Add(new ResolveError(ErrorType.Validation,
                                                    validationResult.MemberNames.Select(n => paramSet[n]),
                                                    null,
                                                    validationResult.ErrorMessage));
                }
            }

            return new ParameterSetResult(sequence, ctx.ParameterSet, ctx.Instance, ctx.Errors);
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
                            ctx.Errors.Add(
                                new ResolveError(ErrorType.IncompatibleType,
                                                 new[] { parameter },
                                                 new[] { astNode },
                                                 String.Format(ErrorMessages.IncompatibleType,
                                                               ctx.Sequence.GetInputString(
                                                                   typeError.AstNode.SourceInfo),
                                                               parameter.Name)));
                        }

                        InvokeError invokeError = valueError as InvokeError;

                        if (invokeError != null)
                        {
                            if (invokeError.Method != null)
                            {
                                ctx.Errors.Add(
                                    new ResolveError(ErrorType.IncompatibleType,
                                                     new[] { parameter },
                                                     new[] { astNode },
                                                     String.Format(
                                                         ErrorMessages.MethodInvocationFailed,
                                                         ctx.Sequence.GetInputString(
                                                                 invokeError.AstNodes.Select(
                                                                 ast => ast.SourceInfo)),
                                                         parameter.Name,
                                                         invokeError.Method.ReflectedType.Name,
                                                         invokeError.Method.Name,
                                                         invokeError.Exception.GetType().Name,
                                                         invokeError.Exception.Message)));
                            }
                            else
                            {
                                ctx.Errors.Add(
                                    new ResolveError(ErrorType.IncompatibleType,
                                                     new[] { parameter },
                                                     new[] { astNode },
                                                     String.Format(
                                                         ErrorMessages
                                                                  .ObjectInitializationFailed,
                                                         ctx.Sequence.GetInputString(
                                                             invokeError.AstNodes.Select(
                                                                 ast => ast.SourceInfo)),
                                                         parameter.Name,
                                                         invokeError.Constructor.ReflectedType.Name,
                                                         invokeError.Exception.GetType().Name,
                                                         invokeError.Exception.Message)));
                            }
                        }
                    }
                }
            }
        }
    }
}
