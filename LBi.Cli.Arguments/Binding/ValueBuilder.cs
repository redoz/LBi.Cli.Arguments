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
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using LBi.Cli.Arguments.Parsing.Ast;

namespace LBi.Cli.Arguments.Binding
{
    public class ValueBuilder : IAstVisitor, IDisposable
    {
        #region Error handling


        internal event EventHandler<ErrorEventArg> Error
        {
            add
            {
                this._errorHandlers.Push(value);
            }
            remove
            {
                if (this._errorHandlers.Count == 0 ||
                    this._errorHandlers.Peek() != value)
                {
                    throw new InvalidOperationException("Cannot remove handler not on top of the stack.");
                }

                this._errorHandlers.Pop();
            }
        }

        protected void RaiseError(ValueError error)
        {
            if (this._errorHandlers.Count > 0)
            {
                var handler = this._errorHandlers.Peek();
                handler(this, new ErrorEventArg(error));
            }
        }

        #endregion


        private static TypeDescriptionProvider _typeDescriptorProvider;
        private readonly CultureInfo _culture;
        private readonly Stack<Type> _targetType;
        private readonly Stack<EventHandler<ErrorEventArg>> _errorHandlers;
        private ErrorCollector _errorCollector;

        public ValueBuilder()
            : this(CultureInfo.InvariantCulture)
        {
            // register custom BooleanTypeConverter, this might be a bad idea.
            TypeConverterAttribute converterAttribute = new TypeConverterAttribute(typeof(CustomBooleanConverter));
            _typeDescriptorProvider = TypeDescriptor.AddAttributes(typeof(Boolean), converterAttribute);
        }

        public ValueBuilder(CultureInfo cultureInfo)
        {
            this._culture = cultureInfo;
            this._errorCollector = null;
            this._targetType = new Stack<Type>();
            this._errorHandlers = new Stack<EventHandler<ErrorEventArg>>();
            this.ResolveInterfaceType +=
                (sender, args) =>
                {
                    if (args.RealType != null)
                        return;

                    if (typeof(IEnumerable).Equals(args.InterfaceType))
                        args.RealType = typeof(List<object>);
                    else
                    {
                        Type[] genArgs;
                        if (args.InterfaceType.IsOfGenericType(typeof(IEnumerable<>), out genArgs))
                            args.RealType = typeof(List<>).MakeGenericType(genArgs);
                        if (args.InterfaceType.IsOfGenericType(typeof(IList<>), out genArgs))
                            args.RealType = typeof(List<>).MakeGenericType(genArgs);
                        else if (args.InterfaceType.IsOfGenericType(typeof(IDictionary<,>), out genArgs))
                            args.RealType = typeof(Dictionary<,>).MakeGenericType(genArgs);
                        else if (args.InterfaceType.IsOfGenericType(typeof(ILookup<,>), out genArgs))
                            args.RealType = typeof(Lookup<,>).MakeGenericType(genArgs);
                    }
                };
        }

        #region Interface registration/lookup
        public event EventHandler<ResolveTypeArgs> ResolveInterfaceType;

        protected Type OnResolveInterfaceType(Type interfaceType)
        {
            ResolveTypeArgs args = new ResolveTypeArgs(interfaceType);
            EventHandler<ResolveTypeArgs> handler = ResolveInterfaceType;
            if (handler != null)
            {
                foreach (EventHandler<ResolveTypeArgs> eventHandler in handler.GetInvocationList())
                    eventHandler(this, args);
            }

            if (args.RealType == null)
            {
                throw new InterfaceResolutionException(
                    string.Format(
                        Resources.Exceptions.FailedToResolveInterfaceType,
                        interfaceType.FullName),
                    interfaceType);
            }

            return args.RealType;
        }
        #endregion



        public IEnumerable<ValueError> Errors
        {
            get
            {
                if (this._errorCollector == null)
                    throw new InvalidOperationException("Build was never called");
                return this._errorCollector.AsEnumerable();
            }
        }

        public bool Build(Type propertyType, AstNode astNode, out object value)
        {
            if (this._errorHandlers.Count > 0)
                throw new InvalidOperationException("This method is not reentrant.");

            if (this._errorCollector != null)
                this._errorCollector.Dispose();

            this._errorCollector = new ErrorCollector(this);

            this._targetType.Push(propertyType);

            value = astNode.Visit(this);

            this._targetType.Pop();

            return this._errorCollector.Count == 0;
        }

        // TODO have to fix this, the "out Exception" isn't very useful, maybe this should Raise errors?
        protected virtual bool TryConvertType(Type targetType, ref object value, out Exception exception)
        {
            bool success = false;
            exception = null;
            object ret = value;
            if (value == null)
            {
                success = !targetType.IsValueType;
            }
            else if (targetType.IsInstanceOfType(value))
                success = true;
            else
            {
                var targetConverter = TypeDescriptor.GetConverter(targetType);
                var sourceConverter = TypeDescriptor.GetConverter(value);

                if (targetConverter.CanConvertFrom(value.GetType()))
                {
                    try
                    {
                        ret = targetConverter.ConvertFrom(null, this._culture, value);
                        success = true;
                    }
                    catch (NotSupportedException ex)
                    {
                        exception = ex;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }
                else if (sourceConverter.CanConvertTo(targetType))
                {
                    ret = sourceConverter.ConvertTo(null, this._culture, value, targetType);
                    success = true;
                }
                else
                {
                    try
                    {
                        ret = Convert.ChangeType(value, targetType, this._culture);
                        success = true;
                    }
                    catch (InvalidCastException ex)
                    {
                        exception = ex;
                    }
                    catch (FormatException ex)
                    {
                        exception = ex;
                    }
                    catch (OverflowException ex)
                    {
                        exception = ex;
                    }
                    catch (ArgumentNullException ex)
                    {
                        exception = ex;
                    }

                    if (!success)
                    {
                        // attempt round-trip to string
                        if (targetConverter.CanConvertFrom(typeof(string)))
                        {
                            try
                            {
                                string tmp;
                                if (sourceConverter.CanConvertTo(typeof(string)))
                                    tmp = sourceConverter.ConvertToString(null, this._culture, value);
                                else
                                    tmp = value.ToString();

                                ret = targetConverter.ConvertFromString(null, this._culture, tmp);
                                success = true;
                            }
                            catch (Exception ex)
                            {
                                exception = ex;
                            }
                        }
                    }
                }
            }


            if (!success)
            {

                // check for ctor with single param
                IEnumerable<ConstructorInfo> ctors = targetType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                ctors = ctors.Where(ct => ct.GetParameters().Length == 1);

                foreach (var ct in ctors)
                {
                    var ctorParams = ct.GetParameters();

                    if (this.TryConvertType(ctorParams[0].ParameterType, ref ret, out exception))
                    {
                        try
                        {
                            ret = ct.Invoke(new[] {ret});
                            success = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                        }
                    }
                }
                // check for static "Parse" method 

                object[] args = new[] {ret};
                MethodInfo parseMethod = targetType.GetMethod("Parse",
                                                              BindingFlags.Public | BindingFlags.Static,
                                                              null,
                                                              new[] {ret.GetType()},
                                                              null);

                if (parseMethod == null)
                {
                    args[0] = ret.ToString();
                    parseMethod = targetType.GetMethod("Parse",
                                                       BindingFlags.Public | BindingFlags.Static,
                                                       null,
                                                       new[] {typeof(string)},
                                                       null);
                }

                if (parseMethod != null)
                {
                    try
                    {
                        ret = parseMethod.Invoke(null, args);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }
 
            }

            value = ret;
            return success;
        }

        #region Implementation of IAstVisitor

        object IAstVisitor.Visit(LiteralValue literalValue)
        {
            return Visit(literalValue);
        }

        protected virtual object Visit(LiteralValue literalValue)
        {
            object ret;
            switch (literalValue.ValueType)
            {
                case LiteralValueType.Numeric:
                    {
                        sbyte signedByte;
                        byte usignedByte;
                        short signedShort;
                        ushort unsignedShort;
                        int signedInt;
                        uint unsignedInt;
                        long signedLong;
                        ulong unsignedLong;
                        Single single;
                        Double dble;
                        Decimal dec;
                        BigInteger bigInt;

                        if (byte.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out usignedByte))
                            ret = usignedByte;
                        else if (sbyte.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out signedByte))
                            ret = signedByte;
                        else if (short.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out signedShort))
                            ret = signedShort;
                        else if (ushort.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out unsignedShort))
                            ret = unsignedShort;
                        else if (int.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out signedInt))
                            ret = signedInt;
                        else if (uint.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out unsignedInt))
                            ret = unsignedInt;
                        else if (long.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out signedLong))
                            ret = signedLong;
                        else if (ulong.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out unsignedLong))
                            ret = unsignedLong;
                        else if (Single.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out single))
                            ret = single;
                        else if (Double.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out dble))
                            ret = dble;
                        else if (Decimal.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out dec))
                            ret = dec;
                        else if (BigInteger.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out bigInt))
                            ret = bigInt;
                        else
                            ret = literalValue.Value;

                        Exception exception;
                        if (!this.TryConvertType(this._targetType.Peek(), ref ret, out exception))
                        {
                            this.RaiseError(new TypeError(this._targetType.Peek(), ret, literalValue,
                                                          exception));
                        }
                    }

                    break;
                case LiteralValueType.String:
                    ret = literalValue.Value;
                    if (this._targetType.Peek() != typeof (string))
                    {
                        Exception exception;
                        if (!this.TryConvertType(this._targetType.Peek(), ref ret, out exception))
                        {
                            this.RaiseError(new TypeError(this._targetType.Peek(), literalValue.Value, literalValue,
                                                          exception));
                        }
                    }
                    break;
                case LiteralValueType.Null:
                    ret = null;
                    break;
                case LiteralValueType.Boolean:
                    ret = StringComparer.InvariantCultureIgnoreCase.Equals(literalValue.Value, "$true");
                    if (this._targetType.Peek() != typeof (bool))
                    {
                        Exception exception;
                        if (!this.TryConvertType(this._targetType.Peek(), ref ret, out exception))
                        {
                            this.RaiseError(new TypeError(this._targetType.Peek(), ret, literalValue,
                                                          exception));
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return ret;
        }


        object IAstVisitor.Visit(Sequence sequence)
        {
            return Visit(sequence);
        }

        protected virtual object Visit(Sequence sequence)
        {
            Type targetType = this._targetType.Peek();
            object ret;

            if (targetType.IsArray)
            {
                Type elementType = targetType.GetElementType();
                Array newArray = Array.CreateInstance(elementType, sequence.Elements.Length);

                this._targetType.Push(elementType);
                for (int elemNum = 0; elemNum < sequence.Elements.Length; elemNum++)
                {
                    AstNode element = sequence.Elements[elemNum];

                    ValueError error = null;
                    using (ErrorCollector errors = new ErrorCollector(this))
                    {
                        object value = element.Visit(this);
                        if (errors.Count == 0)
                            newArray.SetValue(value, elemNum);

                        if (errors.Count == 1)
                            error = errors.First();
                        else if (errors.Count > 1)
                            error = new AggregateError(errors);
                    }

                    if (error != null)
                        this.RaiseError(error);
                }
                this._targetType.Pop();

                ret = newArray;
            }
            else
            {
                Type realType;
                if (targetType.IsInterface)
                    realType = OnResolveInterfaceType(targetType);
                else
                    realType = targetType;

                ret = Activator.CreateInstance(realType);

                MethodInfo[] addMethods =
                    realType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                            .Where(a => StringComparer.InvariantCultureIgnoreCase.Equals(a.Name, "Add"))
                            .ToArray();

                for (int elemNum = 0; elemNum < sequence.Elements.Length; elemNum++)
                {
                    List<ValueError> elementErrors = new List<ValueError>();
                    bool success = false;
                    for (int addNum = 0; addNum < addMethods.Length; addNum++)
                    {
                        ParameterInfo[] addParams = addMethods[addNum].GetParameters();

                        if (addParams.Length != 1)
                            continue;

                        this._targetType.Push(addParams[0].ParameterType);
                        using (ErrorCollector errors = new ErrorCollector(this))
                        {
                            object value = sequence.Elements[elemNum].Visit(this);

                            if (errors.Count == 0)
                            {
                                try
                                {
                                    addMethods[addNum].Invoke(ret, new[] {value});
                                    success = true;
                                    break;
                                }
                                catch (TargetInvocationException ex)
                                {
                                    this.RaiseError(
                                        new InvokeError(addMethods[addNum],
                                                        new[] {value},
                                                        new[] {sequence.Elements[elemNum]}, ex.InnerException));
                                }
                            }

                            if (errors.Count == 1)
                                elementErrors.Add(errors.First());
                            else if (errors.Count > 1)
                                elementErrors.Add(new AggregateError(errors));
                        }
                        this._targetType.Pop();
                    }

                    if (!success)
                    {
                        if (elementErrors.Count == 1)
                            this.RaiseError(elementErrors[0]);
                        else
                            this.RaiseError(new AggregateError(elementErrors));
                    }
                }
            }

            return ret;
        }

        object IAstVisitor.Visit(ParameterName parameterName)
        {
            return Visit(parameterName);
        }

        protected virtual object Visit(ParameterName switchParameter)
        {
            throw new NotSupportedException();
        }

        object IAstVisitor.Visit(SwitchParameter switchParameter)
        {
            return Visit(switchParameter);
        }

        protected virtual object Visit(SwitchParameter switchParameter)
        {
            return switchParameter.Value.Visit(this);
        }

        object IAstVisitor.Visit(AssociativeArray array)
        {
            return Visit(array);
        }

        protected virtual object Visit(AssociativeArray array)
        {
            object ret;
            Type targetType = this._targetType.Peek();
            if (targetType.IsInterface)
                ret = this.HandleInterfaceBasedAssocArray(array, targetType);
            else if (targetType.IsArray)
                ret = this.HandleArrayBasedAssocArray(array, targetType);
            else
                ret = this.HandleMethodBasedAssocArray(array, targetType);

            return ret;
        }

        #region AssociativeArray Handling

        private object HandleMethodBasedAssocArray(AssociativeArray array, Type targetType)
        {
            object ret;

            try
            {
                ret = Activator.CreateInstance(targetType);
            }
            catch (MissingMethodException)
            {
                throw new NotSupportedException(
                    string.Format(Resources.Exceptions.UnsupportedParameterTypeNoDefaultConstructor,
                                  targetType.FullName));
            }
            catch (MissingMemberException)
            {
                // portable class library
                throw new NotSupportedException(
                    string.Format(Resources.Exceptions.UnsupportedParameterTypeNoDefaultConstructor,
                                  targetType.FullName));
            }


            MethodInfo[] addMethods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                                .Where(m => StringComparer.OrdinalIgnoreCase.Equals(m.Name, "Add"))
                                                .ToArray();

            var addWithTwoArgs = addMethods.Where(m => m.GetParameters().Length == 2).ToArray();
            var addWithOneArgs = addMethods.Where(m => m.GetParameters().Length == 1).ToArray();

            for (int elementNum = 0; elementNum < array.Elements.Length; elementNum++)
            {
                KeyValuePair<AstNode, AstNode> elem = array.Elements[elementNum];
                bool success = false;
                for (int addNum = 0; addNum < addWithTwoArgs.Length; addNum++)
                {
                    ParameterInfo[] addParameters = addWithTwoArgs[addNum].GetParameters();

                    var keyType = addParameters[0].ParameterType;
                    var keyNode = elem.Key;
                    this._targetType.Push(keyType);
                    var keyValue = keyNode.Visit(this);
                    this._targetType.Pop();
                    success = keyType.IsInstanceOfType(keyValue);

                    if (success)
                    {
                        var valueType = addParameters[1].ParameterType;
                        var valueNode = elem.Value;
                        this._targetType.Push(valueType);
                        var valueValue = valueNode.Visit(this);
                        this._targetType.Pop();

                        success = valueType.IsInstanceOfType(valueValue);

                        if (success)
                        {
                            try
                            {
                                addWithTwoArgs[addNum].Invoke(ret, new[] { keyValue, valueValue });
                                break;
                            }
                            catch (TargetInvocationException ex)
                            {
                                this.RaiseError(new InvokeError(addWithTwoArgs[addNum],
                                                                new[] { keyValue, valueValue },
                                                                new[] { keyNode, valueNode },
                                                                ex.InnerException));
                            }
                        }
                    }
                }

                if (!success)
                {
                    for (int addNum = 0; addNum < addWithOneArgs.Length; addNum++)
                    {
                        ParameterInfo[] addParameters = addWithOneArgs[addNum].GetParameters();

                        var addParam = addParameters[0];
                        var addParamtype = addParam.ParameterType;

                        var paramTypeCtors =
                            addParamtype.GetConstructors()
                                        .Where(ct => ct.GetParameters().Length == 2)
                                        .ToArray();

                        for (int ctorNum = 0; ctorNum < paramTypeCtors.Length; ctorNum++)
                        {
                            var ctorParameters = paramTypeCtors[ctorNum].GetParameters();

                            var keyType = ctorParameters[0].ParameterType;
                            var keyNode = elem.Key;
                            this._targetType.Push(keyType);
                            var keyValue = keyNode.Visit(this);
                            this._targetType.Pop();

                            success = keyType.IsInstanceOfType(keyValue);

                            if (success)
                            {
                                var valueType = ctorParameters[1].ParameterType;
                                var valueNode = elem.Value;
                                this._targetType.Push(valueType);
                                var valueValue = valueNode.Visit(this);
                                this._targetType.Pop();

                                success = valueType.IsInstanceOfType(valueValue);

                                if (success)
                                {
                                    success = false;
                                    try
                                    {
                                        object elementObj = paramTypeCtors[ctorNum].Invoke(new[] { keyValue, valueValue });

                                        try
                                        {
                                            addWithOneArgs[addNum].Invoke(ret, new[] { elementObj });
                                            success = true;
                                        }
                                        catch (TargetInvocationException ex)
                                        {
                                            this.RaiseError(new InvokeError(addWithOneArgs[addNum],
                                                                            new[] { elementObj },
                                                                            new[] { keyNode, valueNode },
                                                                            ex.InnerException));
                                        }
                                    }
                                    catch (TargetInvocationException ex)
                                    {
                                        this.RaiseError(new InvokeError(paramTypeCtors[ctorNum],
                                                                        new[] { keyValue, valueValue },
                                                                        new[] { keyNode, valueNode },
                                                                        ex.InnerException));
                                    }

                                    if (success)
                                        break;

                                }
                            }
                        }

                        if (success)
                            break;
                    }
                }
            }

            return ret;
        }

        private object HandleInterfaceBasedAssocArray(AssociativeArray array, Type targetType)
        {
            Type realType = this.OnResolveInterfaceType(targetType);
            return HandleMethodBasedAssocArray(array, realType);
        }

        private object HandleArrayBasedAssocArray(AssociativeArray array, Type arrayType)
        {
            object ret;
            Type elementType = arrayType.GetElementType();

            ConstructorInfo[] ctors = elementType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            ConstructorInfo[] matches = ctors.Where(ct => ct.GetParameters().Length == 2).ToArray();

            if (matches.Length == 1)
            {
                Array newArray = Array.CreateInstance(elementType, array.Elements.Length);
                Action<int, object, object> handleKeyValuePair =
                    (index, key, value) =>
                    {
                        object newValue = matches[0].Invoke(new[] { key, value });
                        newArray.SetValue(newValue, index);
                    };

                // this is slightly wasteful as we already asked for the parameters once, but it's a one-off operation.
                ParameterInfo[] parameters = matches[0].GetParameters();

                for (int elemNum = 0; elemNum < array.Elements.Length; elemNum++)
                {
                    this._targetType.Push(parameters[0].ParameterType);
                    KeyValuePair<AstNode, AstNode> element = array.Elements[elemNum];

                    object keyValue = element.Key.Visit(this);
                    this._targetType.Pop();

                    this._targetType.Push(parameters[1].ParameterType);

                    object valueValue = element.Value.Visit(this);
                    this._targetType.Pop();

                    handleKeyValuePair(elemNum, keyValue, valueValue);
                }

                // set return value
                ret = newArray;
            }
            else if (matches.Length == 0)
            {
                throw new NotSupportedException(
                    string.Format(Resources.Exceptions.UnsupportedAssocParameterArrayTypeNoConstructor,
                                  elementType.FullName));
            }
            else
            {
                throw new NotSupportedException(
                    string.Format(Resources.Exceptions.UnsupportedAssocParameterArrayTypeAmbiguousConstructor,
                                  elementType.FullName));
            }

            return ret;
        }

        #endregion

        #endregion

        #region Implementation of IDisposable

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ValueBuilder()
        {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (this._errorCollector != null)
                    this._errorCollector.Dispose();

                TypeDescriptor.RemoveProvider(_typeDescriptorProvider, typeof(Boolean));
                TypeDescriptor.Refresh(typeof(Boolean));
            }
        }

        #endregion
    }
}