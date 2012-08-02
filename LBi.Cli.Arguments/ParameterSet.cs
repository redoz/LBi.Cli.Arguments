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
using System.IO;
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
            PropertyInfo[] publicProps = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            Parameter[] allParams = new Parameter[publicProps.Length];

            for (int i = 0; i < publicProps.Length; i++)
            {
                var propInfo = publicProps[i];
                object[] attrs = propInfo.GetCustomAttributes(typeof(ParameterAttribute), true);
                if (attrs.Length == 0)
                    continue;

                ParameterAttribute attr = (ParameterAttribute)attrs[0];

                string name = string.IsNullOrWhiteSpace(attr.Name) ? propInfo.Name : attr.Name;

                Func<string> helpMessage;

                if (!string.IsNullOrWhiteSpace(attr.HelpMessage) &&
                    string.IsNullOrEmpty(attr.HelpMessageResourceName) &&
                    attr.HelpMessageResourceType == null)
                {
                    helpMessage = () => attr.HelpMessage;
                }
                else if (string.IsNullOrWhiteSpace(attr.HelpMessage) &&
                         string.IsNullOrEmpty(attr.HelpMessageResourceName) &&
                         attr.HelpMessageResourceType != null)
                {
                    if (attr.HelpMessageResourceType.GetInterface(typeof(IResourceProvider).Name) != null)
                    {
                        IResourceProvider provider = (IResourceProvider) Activator.CreateInstance(attr.HelpMessageResourceType);
                        helpMessage = () => provider.GetValue(attr.HelpMessageResourceName);
                    }
                    else
                        helpMessage = ResourceProvider.CreateStaticPropertyAccessor(attr.HelpMessageResourceType, attr.HelpMessageResourceName);
                } else
                {
                    throw new ParameterDefinitionException(propInfo, 
                        "You must specify either ParameterAttribute.HelpMessage or ParameterAttribute.HelpMessageResourceType and ParameterAttribute.HelpMessageResourceName");
                }

                var validators = Attribute.GetCustomAttributes(propInfo, true).OfType<ValidationAttribute>();

                allParams[i] = new Parameter(propInfo, name,attr.Position, helpMessage, validators);
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
                throw new ParameterSetDefinitionException(dupGroups.SelectMany(g => g), errMsg.ToString());
            }

            // check for missing positions
            for (int i = 0; i < positionalParams.Length; i++)
            {
                if (i != positionalParams[i].Position.Value)
                    throw new ParameterSetDefinitionException(positionalParams, "Missing parameter position: " +
                                                                                i.ToString(CultureInfo.InvariantCulture));
            }

            return new ParameterSet(type, allParams);
        }

        protected Parameter[] Parameters;

        private ParameterSet(Type type, Parameter[] parameters)
        {
            this.UnderlyingType = type;
            this.Parameters = parameters;
        }

        public Type UnderlyingType { get; protected set; }

        public int Count { get { return this.Parameters.Length; } }

        public IEnumerable<Parameter> PositionalParameters
        {
            get {
                return this.Parameters.Where(p => p.Position.HasValue);
            }
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

        public IEnumerator<Parameter> GetEnumerator()
        {
            return this.Parameters.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
