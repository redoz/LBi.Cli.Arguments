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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace LBi.Cli.Arguments
{
    public class Parameter
    {
        public Parameter(PropertyInfo property,
                         string name,
                         int? position,
                         Func<string> helpMessageProvider,
                         IEnumerable<ValidationAttribute> validators)
        {
            this.Property = property;
            this.Name = name;
            this.HelpMessage = helpMessageProvider;
            this.Validators = validators.ToArray();
            this.Position = position;
        }

        public Func<string> HelpMessage { get; protected set; }
        public string Name { get; protected set; }
        public int? Position { get; protected set; }
        public ValidationAttribute[] Validators { get; protected set; }
        public PropertyInfo Property { get; protected set; }

        public Type Type => this.Property.PropertyType;

        public T GetAttribute<T>(bool inherit = true) where T : Attribute
        {
            return (T)Attribute.GetCustomAttribute(this.Property, typeof(T), inherit);
        }

        public T[] GetAttributes<T>(bool inherit = true) where T : Attribute
        {
            return (T[])Attribute.GetCustomAttributes(this.Property, typeof(T), inherit);
        }
    }
}
