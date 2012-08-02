using System;
using System.Reflection;

namespace LBi.Cli.Arguments
{
    public class ParameterDefinitionException : Exception
    {
        public ParameterDefinitionException(PropertyInfo parameterProperty, string message)
            : base(WrapMessage(parameterProperty, message))
        {
            this.ParameterProperty = parameterProperty;
        }

        private static string WrapMessage(PropertyInfo parameterProperty, string message)
        {
            return string.Format("Error reported when processing property {0} on type {1}: {2}",
                                 parameterProperty.Name,
                                 parameterProperty.ReflectedType.Name,
                                 message);
        }

        public PropertyInfo ParameterProperty { get; protected set; }

        public ParameterDefinitionException(PropertyInfo parameterProperty, string format, params object[] args) 
            : this(parameterProperty, string.Format(format, args))
        {
        }
    }
}