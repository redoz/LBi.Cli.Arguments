using System;
using System.Collections;
using System.Collections.Generic;

namespace LBi.Cli.Arguments.Binding
{
    internal class ErrorCollector : IEnumerable<ValueError>, IDisposable
    {
        private readonly List<ValueError> _errors;
        private readonly ValueBuilder _valueBuilder;

        public ErrorCollector(ValueBuilder builder)
        {
            this._errors = new List<ValueError>();
            this._valueBuilder = builder;
            this._valueBuilder.Error += this.ValueBuilderErrorHandler;
        }

        private void ValueBuilderErrorHandler(object sender, ErrorEventArg args)
        {
            this._errors.Add(args.Error);
        }

        public int Count => this._errors.Count;

        public void Dispose()
        {
            this._valueBuilder.Error -= this.ValueBuilderErrorHandler;
        }

        public IEnumerator<ValueError> GetEnumerator()
        {
            return this._errors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
