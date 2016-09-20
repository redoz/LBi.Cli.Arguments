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
using System.IO;
using System.Linq;
using System.Reflection;

namespace LBi.Cli.Arguments.Output
{
    // TODO refactor and clean this up, it's a bit of a kludge atm
    public class HelpWriter : IHelpWriter
    {
        public HelpWriter()
        {
        }

        protected virtual void WriteParameter(TextWriter writer, IEnumerable<ParameterSet> sets, Parameter parameter)
        {
            writer.Write('-');
            writer.Write(parameter.Name);
            writer.Write(' ');
            if (parameter.Type == typeof(Switch))
            {
                writer.WriteLine("[<" + typeof(Switch).Name + ">]");
            }
            else
            {
                writer.Write('<');
                writer.Write(parameter.Type.Name);
                writer.Write('>');
                writer.WriteLine();
            }

            writer.WriteLine();

            writer.Write("   ");
            writer.WriteLine(parameter.HelpMessage());

            writer.WriteLine();

            writer.WriteLine("   {0,-25}{1}", "Required?", parameter.GetAttribute<RequiredAttribute>() != null);
            writer.WriteLine("   {0,-25}{1}", "Position?", parameter.Position.HasValue ? parameter.Position.Value.ToString() : "named");
            string defValue;
            var defValueAttr = parameter.GetAttribute<DefaultValueAttribute>();

            if (defValueAttr == null)
                defValue = "None";
            else
                defValue = defValueAttr.Value.ToString();

            writer.WriteLine("   {0,-25}{1}", "Default value", defValue);


            writer.WriteLine("   {0,-25}{1}",
                             "Parameter sets",
                             string.Join(", ", sets.Where(s => s.Contains(parameter)).Select(s => s.Name)));
        }

        protected virtual void WriteSyntax(TextWriter writer, IEnumerable<ParameterSet> parameterSets, HelpLevel level)
        {
            const string indent = "   ";
            writer.WriteLine("SYNTAX");
            var entryAsm = Assembly.GetEntryAssembly();
            string fileName = "";
            if (entryAsm != null)
                fileName = Path.GetFileName(Assembly.GetEntryAssembly().Location);

            this.WriteSyntax(writer,
                             parameterSets,
                             indent,
                             fileName,
                             true,
                             p => true,
                             p =>
                             {
                                 if (p.Property.PropertyType == typeof(Switch))
                                     return "";

                                 if (p.Property.PropertyType == typeof(Switch))
                                     return "";

                                 if (level.HasFlag(HelpLevel.Detailed))
                                     return string.Format("<{0}>", p.Property.PropertyType.FullName);

                                 return string.Format("<{0}>", this.GetHumanReadableTypeName(p.Property.PropertyType));
                             });
        }

        protected virtual void WriteSyntax(TextWriter writer,
                                           IEnumerable<ParameterSet> parameterSets,
                                           string indent,
                                           string fileName,
                                           bool includeOptionalMarkers,
                                           Func<Parameter, bool> shouldWrite,
                                           Func<Parameter, string> valueWriter)
        {
            foreach (ParameterSet set in parameterSets)
            {
                writer.Write(indent);
                writer.Write(fileName);

                if (!string.IsNullOrEmpty(set.Command))
                {
                    writer.Write(' ');
                    writer.Write(set.Command);
                }

                foreach (Parameter parameter in set.PositionalParameters)
                {
                    if (!shouldWrite(parameter))
                        continue;

                    bool optional = parameter.GetAttribute<RequiredAttribute>() == null;

                    writer.Write(" ");

                    if (optional && includeOptionalMarkers)
                    {
                        writer.Write("[");
                    }
                    if (includeOptionalMarkers)
                        writer.Write("[");

                    writer.Write('-');
                    writer.Write(parameter.Name);
                    if (includeOptionalMarkers)
                        writer.Write("]");
                    var value = valueWriter(parameter);
                    if (value.Length > 0
                        && !(parameter.Property.PropertyType == typeof(Switch)
                             && StringComparer.InvariantCultureIgnoreCase.Equals("$true", value)))
                    {
                        if (parameter.Property.PropertyType == typeof(Switch))
                            writer.Write(':');
                        else
                            writer.Write(' ');
                        writer.Write(value);
                    }
                    if (optional && includeOptionalMarkers)
                    {
                        writer.Write("]");
                    }
                }

                var namedParameters = set.Except(set.PositionalParameters).ToArray();
                var requriedParams = namedParameters.Where(p => p.GetAttribute<RequiredAttribute>() != null)
                                                    .OrderBy(p => p.Name, StringComparer.InvariantCultureIgnoreCase)
                                                    .ToArray();
                var optionalParams =
                    namedParameters.Except(requriedParams)
                                   .OrderBy(p => p.Name, StringComparer.InvariantCultureIgnoreCase)
                                   .ToArray();
                foreach (Parameter parameter in requriedParams.Concat(optionalParams))
                {
                    if (!shouldWrite(parameter))
                        continue;

                    bool optional = parameter.GetAttribute<RequiredAttribute>() == null;

                    writer.Write(" ");

                    if (optional && includeOptionalMarkers)
                    {
                        writer.Write("[");
                    }
                    writer.Write('-');
                    writer.Write(parameter.Name);

                    var value = valueWriter(parameter);
                    if (value.Length > 0
                        && !(parameter.Property.PropertyType == typeof(Switch)
                             && StringComparer.InvariantCultureIgnoreCase.Equals("$true", value)))
                    {
                        if (parameter.Property.PropertyType == typeof(Switch))
                            writer.Write(':');
                        else
                            writer.Write(' ');
                        writer.Write(value);
                    }

                    if (optional && includeOptionalMarkers)
                    {
                        writer.Write("]");
                    }
                }

                writer.WriteLine();
                writer.WriteLine();
                writer.WriteLine(set.HelpMessage());
                writer.WriteLine();
            }
        }


        public virtual void Write(TextWriter writer, ParameterSet set, HelpLevel level)
        {
            var tmp = new ParameterSetCollection(new[] { set });
            this.Write(writer, tmp, level);
        }

        public virtual void Write(TextWriter writer, ParameterSetCollection sets, HelpLevel level)
        {
            if (level.HasFlag(HelpLevel.Syntax))
            {
                this.WriteSyntax(writer, sets, level);
            }

            if (level.HasFlag(HelpLevel.Examples))
            {
                this.WriteExample(writer, sets, level);
            }

            if (level.HasFlag(HelpLevel.Parameters))
            {
                this.WriteParameters(writer, sets, level);
            }
        }

        private void WriteParameters(TextWriter writer, ParameterSetCollection sets, HelpLevel level)
        {
            // TODO grouping by "name" alone is not good enough
            var parameters = sets.SelectMany(set => set).GroupBy(p => p.Name)
                                 .OrderBy(g => g.Key, StringComparer.InvariantCultureIgnoreCase)
                                 .Select(g => g.First());

            foreach (var parameter in parameters)
            {
                writer.WriteLine();
                this.WriteParameter(writer, sets, parameter);
            }
        }

        protected virtual void WriteExample(TextWriter writer, ParameterSetCollection parameterSets, HelpLevel level)
        {
            const string indent = "   ";
            writer.WriteLine("EXAMPLES");

            var entryAsm = Assembly.GetEntryAssembly();
            string fileName = "";
            if (entryAsm != null)
                fileName = Path.GetFileName(Assembly.GetEntryAssembly().Location);

            foreach (var set in parameterSets)
            {
                var examples =
                    set.SelectMany(p => p.GetAttributes<ExampleValueAttribute>()
                                         .Select(ev => new Tuple<Parameter, ExampleValueAttribute>(p, ev)))
                       .GroupBy(t => t.Item2.Set,
                                StringComparer.InvariantCultureIgnoreCase);

                examples = examples.Where(g => g.Any(t => t.Item1.Property.DeclaringType == set.UnderlyingType));

                foreach (IGrouping<string, Tuple<Parameter, ExampleValueAttribute>> group in examples)
                {
                    var attrDict = group.ToDictionary(t => t.Item1, t => t.Item2);
                    this.WriteSyntax(writer,
                                     new[] { set },
                                     indent,
                                     fileName,
                                     false,
                                     p =>
                                     {
                                         ExampleValueAttribute attr;
                                         if (attrDict.TryGetValue(p, out attr))
                                             return true;
                                         return p.GetAttribute<RequiredAttribute>() != null;
                                     },
                                     p =>
                                     {
                                         ExampleValueAttribute attr;
                                         if (attrDict.TryGetValue(p, out attr))
                                             return attr.Value;

                                         if (p.Property.PropertyType == typeof(Switch))
                                             return "";

                                         if (level.HasFlag(HelpLevel.Detailed))
                                             return string.Format("<{0}>", p.Property.PropertyType.FullName);

                                         return string.Format("<{0}>", this.GetHumanReadableTypeName(p.Property.PropertyType));
                                     });
                }
            }
        }

        private string GetHumanReadableTypeName(Type type)
        {
            bool isArray;
            if (isArray = type.IsArray)
                type = type.GetElementType();

            if (type.IsGenericType)
                return type.Name.Substring(0, type.Name.IndexOf('`')) + (isArray ? "[]" : "");
            return type.Name + (isArray ? "[]" : "");
        }
    }
}
