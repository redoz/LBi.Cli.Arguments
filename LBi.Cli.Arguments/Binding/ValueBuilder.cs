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
        private static TypeDescriptionProvider _typeDescriptorProvider;
        private readonly CultureInfo _culture;
        private readonly List<TypeError> _errors;
        private readonly Stack<Type> _targetType;

        public ValueBuilder()
            : this(CultureInfo.InvariantCulture)
        {
            // register custom BooleanTypeConverter
            TypeConverterAttribute converterAttribute = new TypeConverterAttribute(typeof(CustomBooleanConverter));
            _typeDescriptorProvider = TypeDescriptor.AddAttributes(typeof(Boolean), converterAttribute);
        }

        public ValueBuilder(CultureInfo cultureInfo)
        {
            this._culture = cultureInfo;
            this._errors = new List<TypeError>();
            this._targetType = new Stack<Type>();
        }

        public IEnumerable<TypeError> Errors
        {
            get { return this._errors; }
        }

        public bool Build(Type propertyType, AstNode astNode, out object value)
        {
            this._targetType.Push(propertyType);

            value = astNode.Visit(this);

            return this._errors.Count == 0;
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
                        else if (long.TryParse(literalValue.Value, NumberStyles.Any, this._culture,
                                               out signedLong))
                            ret = signedLong;
                        else if (ulong.TryParse(literalValue.Value, NumberStyles.Any, this._culture,
                                                out unsignedLong))
                            ret = unsignedLong;
                        else if (Single.TryParse(literalValue.Value, NumberStyles.Any, this._culture,
                                                 out single))
                            ret = single;
                        else if (Double.TryParse(literalValue.Value, NumberStyles.Any, this._culture,
                                                 out dble))
                            ret = dble;
                        else if (Decimal.TryParse(literalValue.Value, NumberStyles.Any,
                                                  this._culture, out dec))
                            ret = dec;
                        else if (BigInteger.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out bigInt))
                            ret = bigInt;
                        else
                            ret = literalValue.Value;

                        string errorMessage;
                        if (!this.TryConvertType(this._targetType.Peek(), ref ret, out errorMessage))
                        {
                            this._errors.Add(new TypeError(this._targetType.Peek(), ret, literalValue,
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
                            this._errors.Add(new TypeError(this._targetType.Peek(), literalValue.Value, literalValue,
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
                            this._errors.Add(new TypeError(this._targetType.Peek(), ret, literalValue,
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
            Type elementType;

            Action<int, object> elementHandler;
            object ret;

            if (targetType.IsArray)
            {
                elementType = targetType.GetElementType();
                ret = Array.CreateInstance(elementType, sequence.Elements.Length);
                Array newArray = (Array)ret;
                elementHandler = (i, o) => newArray.SetValue(o, i);
            }
            else
            {
                if (targetType.IsInterface)
                {
                    Type[] genArgs;

                    if (targetType.IsInterface && targetType == typeof(IEnumerable))
                    {
                        elementType = typeof(object);
                    }
                    else if (targetType.IsOfGenericType(typeof(IEnumerable<>), out genArgs))
                    {
                        elementType = genArgs[0];
                    }
                    else
                    {
                        // TODO fix this
                        throw new Exception("Unsupported");
                    }

                    ret = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                    MethodInfo addMethod = ret.GetType().GetMethod("Add", new[] { elementType });
                    Debug.Assert(addMethod != null, "addMethod is null. Add some error checking here.");
                    elementHandler = (i, o) => addMethod.Invoke(ret, new[] { o });
                }
                else
                {
                    MethodInfo addMethod =
                        targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                            .Where(m => StringComparer.InvariantCultureIgnoreCase.Equals("Add", m.Name))
                            .Single(m => m.GetParameters().Length == 1);
                    elementType = addMethod.GetParameters()[0].ParameterType;
                    ret = Activator.CreateInstance(targetType);
                    elementHandler = (i, o) => addMethod.Invoke(ret, new[] { o });
                }
            }

            this._targetType.Push(elementType);
            for (int elemNum = 0; elemNum < sequence.Elements.Length; elemNum++)
            {
                AstNode element = sequence.Elements[elemNum];
                object value = element.Visit(this);
                elementHandler(elemNum, value);
            }
            this._targetType.Pop();
            return ret;
        }

        object IAstVisitor.Visit(AssociativeArray array)
        {
            Type targetType = this._targetType.Peek();
            if (targetType.IsInterface)
                return this.HandleInterfaceBasedAssocArray(array, targetType);
            else if (targetType.IsArray)
                return this.HandleArrayBasedAssocArray(array, targetType);
            else
                return this.HandleMethodBasedAssocArray(array, targetType);
        }

        #region AssociativeArray Handling

        private object HandleMethodBasedAssocArray(AssociativeArray array, Type targetType)
        {
            object ret;
            MethodInfo addMethod = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(
                    m => StringComparer.OrdinalIgnoreCase.Equals("Add", m.Name) &&
                         m.GetParameters().Length == 2);

            if (addMethod != null)
            {
                ConstructorInfo defaultCtor = targetType.GetConstructor(Type.EmptyTypes);

                if (defaultCtor != null)
                {
                    ret = defaultCtor.Invoke(null);

                    var addParams = addMethod.GetParameters();

                    Type keyType = addParams[0].ParameterType;
                    Type valueType = addParams[1].ParameterType;
                    Action<int, object, object> handleKeyValuePair =
                        (index, key, value) =>
                        addMethod.Invoke(ret, new[] { key, value });

                    this.FillAssocArray(array, keyType, valueType, handleKeyValuePair);
                }
                else
                {
                    throw new NotSupportedException(
                        string.Format(Resources.Exceptions.UnsupportedParameterTypeNoDefaultConstructor,
                                      targetType.FullName));
                }
            }
            else
            {
                addMethod = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(
                        m => StringComparer.OrdinalIgnoreCase.Equals("Add", m.Name) &&
                             m.GetParameters().Length == 1);

                if (addMethod != null)
                {
                    var addParams = addMethod.GetParameters();

                    var parameterType = addParams[0].ParameterType;

                    var paramTypeCtors = parameterType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

                    var matches = paramTypeCtors.Where(ct => ct.GetParameters().Length == 2).ToArray();

                    if (matches.Length == 1)
                    {
                        // tODO fix this
                    }
                    else if (matches.Length == 0)
                    {
                        // and this
                    }
                    else
                    {
                        // and this   
                    }

                    throw new NotSupportedException(
                        string.Format(Resources.Exceptions.UnsupportedParameterTypeNoAddMethod,
                                      targetType.FullName));
                } else
                {
                    // TODO Fix this
                    ret = null;
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