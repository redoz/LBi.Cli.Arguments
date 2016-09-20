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
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using LBi.Cli.Arguments.Globalization;

namespace LBi.Cli.Arguments
{
    public class ParameterSet : IEnumerable<Parameter>
    {
        public static ParameterSet FromType(Type type)
        {
            ParameterSetAttribute setAttr = (ParameterSetAttribute)Attribute.GetCustomAttribute(type, typeof(ParameterSetAttribute), true);

            PropertyInfo[] publicProps = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            List<Parameter> allParams = new List<Parameter>();

            Func<string> helpMessage;
            string name;

            for (int i = 0; i < publicProps.Length; i++)
            {
                var propInfo = publicProps[i];
                ParameterAttribute attr = (ParameterAttribute)Attribute.GetCustomAttribute(propInfo, typeof(ParameterAttribute), true);
                if (attr == null)
                    continue;

                name = string.IsNullOrWhiteSpace(attr.Name) ? propInfo.Name : attr.Name;

                try
                {
                    helpMessage = attr.GetHelpMessageSelector();
                }
                catch (InvalidOperationException ex)
                {
                    throw new ParameterDefinitionException(propInfo,
                                                           ex.Message);
                }

                var validators = Attribute.GetCustomAttributes(propInfo, true).OfType<ValidationAttribute>();

                allParams.Add(new Parameter(propInfo, name, attr.Position, helpMessage, validators));
            }

            // sanity check input parameters
            Parameter[] positionalParams = allParams.Where(p => p.Position.HasValue).OrderBy(p => p.Position.Value).ToArray();
            IEnumerable<IGrouping<int, Parameter>> ppGroups = positionalParams.GroupBy(p => p.Position.Value);
            IGrouping<int, Parameter>[] dupGroups = ppGroups.Where(g => g.Count() > 1).ToArray();

            // check for duplicate positions
            if (dupGroups.Length > 0)
            {
                StringBuilder errMsg = new StringBuilder("Duplicate parameter positions: ");
                for (int i = 0; i < dupGroups.Length; i++)
                {
                    errMsg.Append("Position: ").Append(dupGroups[i].Key).AppendLine();

                    foreach (Parameter dupParam in dupGroups[i])
                        errMsg.AppendLine(dupParam.Name);
                }
                throw new ParameterSetDefinitionException(type, dupGroups.SelectMany(g => g), errMsg.ToString());
            }

            // check for missing positions
            for (int i = 0; i < positionalParams.Length; i++)
            {
                if (i != positionalParams[i].Position.Value)
                    throw new ParameterSetDefinitionException(type,
                                                              positionalParams,
                                                              "Missing parameter position: " +
                                                              i.ToString(CultureInfo.InvariantCulture));
            }
            try
            {
                helpMessage = setAttr.GetHelpMessageSelector();
            }
            catch (Exception ex)
            {
                throw new ParameterSetDefinitionException(type, positionalParams, ex.Message);
            }
            name = string.IsNullOrWhiteSpace(setAttr.Name) ? type.Name : setAttr.Name;

            // validate command name
            if (setAttr.Command != null && setAttr.Command.Any(char.IsWhiteSpace))
                throw new ParameterSetDefinitionException(type,
                                                          Enumerable.Empty<Parameter>(),
                                                          "Command cannot contain whitespace: '{0}'.",
                                                          setAttr.Command);

            return new ParameterSet(type, name, setAttr.Command, allParams.ToArray(), helpMessage);
        }

        protected Parameter[] Parameters;

        private ParameterSet(Type type, string name, string command, Parameter[] parameters, Func<string> helpMessage)
        {
            this.UnderlyingType = type;
            this.Command = command;
            this.Parameters = parameters;
            this.HelpMessage = helpMessage;
            this.Name = name;
        }

        public Func<string> HelpMessage { get; protected set; }

        public string Name { get; protected set; }

        public Type UnderlyingType { get; protected set; }

        public string Command { get; protected set; }

        public T GetAttribute<T>(bool inherit = true) where T : Attribute
        {
            return (T)Attribute.GetCustomAttribute(this.UnderlyingType, typeof(T), inherit);
        }

        public int Count
        {
            get { return this.Parameters.Length; }
        }

        /// <summary>
        ///     Returns an ordered list of positional parameters.
        /// </summary>
        public IEnumerable<Parameter> PositionalParameters
        {
            get { return this.Parameters.Where(p => p.Position.HasValue).OrderBy(p => p.Position.Value); }
        }

        public Parameter this[int index]
        {
            get
            {
                if (index < 0 || index >= this.Parameters.Length)
                    throw new ArgumentOutOfRangeException("index");

                return this.Parameters[index];
            }
        }

        public Parameter this[string name]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentNullException("name");

                Parameter ret = null;
                for (int paramNum = 0; paramNum < this.Parameters.Length; paramNum++)
                {
                    Parameter curParam = this.Parameters[paramNum];
                    string namePrefix = curParam.Name.Substring(0, Math.Min(name.Length, curParam.Name.Length));
                    if (StringComparer.InvariantCultureIgnoreCase.Equals(name, namePrefix))
                        if (ret == null)
                            ret = curParam;
                        else
                            throw new AmbiguousMatchException("More than one match.");
                }
                return ret;
            }
        }

        public IEnumerator<Parameter> GetEnumerator()
        {
            return this.Parameters.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public bool TryGetParameter(string name, out Parameter[] parameters)
        {
            parameters =
                this.Parameters
                    .Where(p => p.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                    .ToArray();

            return parameters.Length == 1;
        }
    }
}
