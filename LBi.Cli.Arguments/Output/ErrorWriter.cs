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

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace LBi.Cli.Arguments.Output
{
    public class ErrorWriter : IErrorWriter
    {
        public ErrorWriter()
        {
        }

        protected virtual void WriteParameter(TextWriter writer, IEnumerable<ParameterSetResult> result, Parameter parameter)
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

            var sets = result.Select(r => r.ParameterSet)
                             .Where(s => s.Contains(parameter));

            writer.WriteLine("   {0,-25}{1}", "Parameter sets", string.Join(", ", sets.Select(s => s.Name)));
        }

        public virtual void Write(TextWriter writer, ResolveResult result)
        {
            var error = result.BestMatch.Errors[0];
            writer.WriteLine(error.Message);

            if (error.Parameter != null)
            {
                foreach (var parameter in error.Parameter)
                {
                    this.WriteParameter(writer, result, parameter);
                }
            }
        }
    }
}