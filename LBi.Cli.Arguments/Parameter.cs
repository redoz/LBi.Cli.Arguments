using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using LBi.Cli.Arguments.Globalization;

namespace LBi.Cli.Arguments
{
    public class Parameter
    {
        public Parameter(PropertyInfo property, string name, int? position, Func<string> helpMessageProvider, IEnumerable<ValidationAttribute> validators)
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
    }
}