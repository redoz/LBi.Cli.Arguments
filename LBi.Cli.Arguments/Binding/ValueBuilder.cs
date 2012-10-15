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

        
        public event EventHandler<ErrorEventArg> Error;

        protected void RaiseError(ValueError error)
        {
            EventHandler<ErrorEventArg> handler = Error;
            if (handler != null) handler(this, new ErrorEventArg(error));
        }

        #endregion


        private static TypeDescriptionProvider _typeDescriptorProvider;
        private readonly CultureInfo _culture;
        private readonly List<ValueError> _allErrors;
        private readonly Stack<Type> _targetType;
 
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
            this._allErrors = new List<ValueError>();
            this._targetType = new Stack<Type>();

            this.ResolveInterfaceType +=
                (sender, args) =>
                    {
                        if (args.RealType != null)
                            return;

                        if (typeof (IEnumerable).Equals(args.InterfaceType))
                            args.RealType = typeof (List<object>);
                        else
                        {
                            Type[] genArgs;
                            if (args.InterfaceType.IsOfGenericType(typeof (IEnumerable<>), out genArgs))
                                args.RealType = typeof (List<>).MakeGenericType(genArgs);
                            else if (args.InterfaceType.IsOfGenericType(typeof (IDictionary<,>), out genArgs))
                                args.RealType = typeof (Dictionary<,>).MakeGenericType(genArgs);
                            else if (args.InterfaceType.IsOfGenericType(typeof (ILookup<,>), out genArgs))
                                args.RealType = typeof (Lookup<,>).MakeGenericType(genArgs);
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
            get { return this._allErrors; }
        }

        public bool Build(Type propertyType, AstNode astNode, out object value)
        {
            this._allErrors.Clear();

            this._targetType.Push(propertyType);

            value = astNode.Visit(this);

            return this._allErrors.Count == 0;
        }

        // TODO take another look at how this method deals with error handling at some point
        private bool TryConvertType(Type targetType, ref object value, out string errorMessage)
        {
            bool success = false;
            errorMessage = null;
            object ret = value;
            if (targetType.IsInstanceOfType(value))
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
                        errorMessage = ex.Message;
                    }
                    catch (Exception ex)
                    {
                        errorMessage = ex.Message;
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
                        errorMessage = ex.Message;
                    }
                    catch (FormatException ex)
                    {
                        errorMessage = ex.Message;
                    }
                    catch (OverflowException ex)
                    {
                        errorMessage = ex.Message;
                    }
                    catch (ArgumentNullException ex)
                    {
                        errorMessage = ex.Message;
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
                                errorMessage = ex.Message;
                            }
                        }
                    }
                }
            }

            value = ret;
            return success;
        }

        private bool TryConstructPair(Type targetType, object key, object value, out object pair)
        {
            bool success = false;
            pair = null;
            var ctors = targetType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var ctor in ctors)
            {
                object keyArg = key;
                object valueArg = value;

                var ctorParams = ctor.GetParameters();

                if (ctorParams.Length != 2)
                    continue;

                string errMsg;
                if (this.TryConvertType(ctorParams[0].ParameterType, ref keyArg, out errMsg))
                {
                    if (this.TryConvertType(ctorParams[1].ParameterType, ref valueArg, out errMsg))
                    {
                        pair = ctor.Invoke(new[] {keyArg, valueArg});
                        success = true;
                        break;
                    }
                }
            }

            return success;
        }

        #region Implementation of IAstVisitor

        object IAstVisitor.Visit(LiteralValue literalValue)
        {
            object ret;
            switch (literalValue.Type)
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
                        if (sbyte.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out signedByte))
                            ret = signedByte;
                        else if (byte.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out usignedByte))
                            ret = usignedByte;
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

                        string errorMessage;
                        if (!this.TryConvertType(this._targetType.Peek(), ref ret, out errorMessage))
                        {
                            this.RaiseError(new TypeError(this._targetType.Peek(), ret, literalValue,
                                                           errorMessage));
                        }
                    }

                    break;
                case LiteralValueType.String:
                    ret = literalValue.Value;
                    if (this._targetType.Peek() != typeof(string))
                    {
                        string errorMessage;
                        if (!this.TryConvertType(this._targetType.Peek(), ref ret, out errorMessage))
                        {
                            this.RaiseError(new TypeError(this._targetType.Peek(), literalValue.Value, literalValue,
                                                           errorMessage));
                        }
                    }
                    break;
                case LiteralValueType.Null:
                    ret = null;
                    break;
                case LiteralValueType.Boolean:
                    ret = StringComparer.InvariantCultureIgnoreCase.Equals(literalValue.Value, "$true");
                    if (this._targetType.Peek() != typeof(bool))
                    {
                        string errorMessage;
                        if (!this.TryConvertType(this._targetType.Peek(), ref ret, out errorMessage))
                        {
                            this.RaiseError(new TypeError(this._targetType.Peek(), ret, literalValue,
                                                           errorMessage));
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
                    using (ErrorCollector errors = new ErrorCollector(this))
                    {
                        object value = element.Visit(this);
                        if (errors.Count == 0)
                            newArray.SetValue(value, elemNum);

                        this._allErrors.AddRange(errors);
                    }
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

                MethodInfo[] addMethods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                for (int elemNum = 0; elemNum < sequence.Elements.Length; elemNum++)
                {
                    List<TypeError> elementErrors = new List<TypeError>();
                    for (int addNum = 0; addNum < addMethods.Length; addNum++)
                    {
                        if (!StringComparer.InvariantCultureIgnoreCase.Equals(addMethods[addNum].Name, "Add"))
                            continue;

                        ParameterInfo[] addParams = addMethods[addNum].GetParameters();

                        if (addParams.Length != 1)
                            continue;

                        this._targetType.Push(addParams[0].ParameterType);
                        using (ErrorCollector errors = new ErrorCollector(this))
                        {
                            object value = sequence.Elements[elemNum].Visit(this);

                            if (errors.Count == 0)
                                addMethods[addNum].Invoke(ret, new[] { value });

                            // THIS SUCKS
                            elementErrors.A
                        }
                        this._targetType.Pop();
                    }
                }

                {
                    {
                        AstNode element = sequence.Elements[elemNum];
                        object value = element.Visit(this);

                        elementHandler(elemNum, value);
                    }
                    this._targetType.Pop();

                }
     
                
            }

            return ret;
        }



        object IAstVisitor.Visit(AssociativeArray array)
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


            MethodInfo[] addMethods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public);

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
                            catch (Exception ex)
                            {
                                this.RaiseError(new InvokeError(addWithTwoArgs[addNum],
                                                                 new[] {keyValue, valueValue},
                                                                 new[] {keyNode, valueNode},
                                                                 ex.Message));
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
                                        catch (Exception ex)
                                        {
                                            this.RaiseError(new InvokeError(addWithOneArgs[addNum],
                                                                             new[] {elementObj},
                                                                             new[] {keyNode, valueNode},
                                                                             ex.Message));
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        this.RaiseError(new InvokeError(addWithTwoArgs[addNum],
                                                                         new[] {keyValue, valueValue},
                                                                         new[] {keyNode, valueNode},
                                                                         ex.Message));
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
            if (targetType.IsGenericType)
            {
                Type genericTypeDef = targetType.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(IEnumerable<>))
                {
                    Type genericTypeArg = targetType.GetGenericArguments()[0];

                    if (genericTypeArg.IsGenericType)
                    {
                        if (genericTypeArg == typeof(KeyValuePair<,>))
                        {
                        }
                        else if (genericTypeArg == typeof(Tuple<,>))
                        {
                        }
                        else
                        {
                            throw new NotSupportedException(
                                string.Format(Resources.Exceptions.UnsupportedParameterType,
                                              targetType.FullName));
                        }
                    }
                    else
                    {
                        throw new NotSupportedException(
                            string.Format(Resources.Exceptions.UnsupportedParameterType,
                                          targetType.FullName));
                    }
                }
                else if (genericTypeDef == typeof(ILookup<,>))
                {
                    // TODO impl
                    throw new NotImplementedException();
                }
                else if (genericTypeDef == typeof(IDictionary<,>))
                {
                    // TODO impl
                    throw new NotImplementedException();
                }
                else
                {
                    throw new NotSupportedException(
                        string.Format(Resources.Exceptions.UnsupportedParameterType,
                                      targetType.FullName));
                }
                return null;
            }
            else if (targetType == typeof(IDictionary))
            {
                // TODO impl
                throw new NotImplementedException();
            }
            else if (targetType == typeof(IList))
            {
                // TODO impl
                throw new NotImplementedException();
            }
            else
            {
                throw new NotSupportedException(
                    string.Format(Resources.Exceptions.UnsupportedParameterType,
                                  targetType.FullName));
            }
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

                this.FillAssocArray(array, parameters[0].ParameterType, parameters[1].ParameterType, handleKeyValuePair);

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

        private void FillAssocArray(AssociativeArray array,
                                    Type keyType,
                                    Type valueType,
                                    Action<int, object, object> handleKeyValuePair)
        {
            for (int elemNum = 0; elemNum < array.Elements.Length; elemNum++)
            {
                this._targetType.Push(keyType);
                KeyValuePair<AstNode, AstNode> element = array.Elements[elemNum];

                object keyValue = element.Key.Visit(this);
                this._targetType.Pop();

                this._targetType.Push(valueType);

                object valueValue = element.Value.Visit(this);
                this._targetType.Pop();

                handleKeyValuePair(elemNum, keyValue, valueValue);
            }
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
                TypeDescriptor.RemoveProvider(_typeDescriptorProvider, typeof(Boolean));
                TypeDescriptor.Refresh(typeof(Boolean));
            }
        }

        #endregion
    }
}