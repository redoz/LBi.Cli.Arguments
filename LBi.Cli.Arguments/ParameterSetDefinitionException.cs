using System;
using System.Collections.Generic;

namespace LBi.Cli.Arguments
{
    public class ParameterSetDefinitionException : Exception
    {
        public ParameterSetDefinitionException(IEnumerable<Parameter> parameters, string message)
            : base(message)
        {
            this.ParameterProperties = parameters;
        }

        public IEnumerable<Parameter> ParameterProperties { get; protected set; }

        public ParameterSetDefinitionException(IEnumerable<Parameter> parameters, string format, params object[] args)
            : this(parameters, string.Format(format, args))
        {
        }
    }
}