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
        private object _value;

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

        public object Value
        {
            get { return this._value; }
        }

        public bool Build(Type propertyType, AstNode astNode)
        {
            this._targetType.Push(propertyType);

            astNode.Visit(this);

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

        void IAstVisitor.Visit(LiteralValue literalValue)
        {
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
                        if (sbyte.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out signedByte))
                            this._value = signedByte;
                        else if (byte.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out usignedByte))
                            this._value = usignedByte;
                        else if (short.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out signedShort))
                            this._value = signedShort;
                        else if (ushort.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out unsignedShort))
                            this._value = unsignedShort;
                        else if (int.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out signedInt))
                            this._value = signedInt;
                        else if (uint.TryParse(literalValue.Value, NumberStyles.Any, this._culture, out unsignedInt))
                            this._value = unsignedInt;
                        else if (long.TryParse(literalValue.Value, NumberStyles.Any, this._culture,
                                               out signedLong))
                            this._value = signedLong;
                        else if (ulong.TryParse(literalValue.Value, NumberStyles.Any, this._culture,
                                                out unsignedLong))
                            this._value = unsignedLong;
                        else if (Single.TryParse(literalValue.Value, NumberStyles.Any, this._culture,
                                                 out single))
                            this._value = single;
                        else if (Double.TryParse(literalValue.Value, NumberStyles.Any, this._culture,
                                                 out dble))
                            this._value = dble;
                        else if (Decimal.TryParse(literalValue.Value, NumberStyles.Any,
                                                  this._culture, out dec))
                            this._value = dec;

                        string errorMessage;
                        if (!this.TryConvertType(this._targetType.Peek(), ref this._value, out errorMessage))
                        {
                            this._errors.Add(new TypeError(this._targetType.Peek(), this.Value, literalValue,
                                                           errorMessage));
                        }
                    }

                    break;
                case LiteralValueType.String:
                    this._value = literalValue.Value;
                    if (this._targetType.Peek() != typeof(string))
                    {
                        string errorMessage;
                        if (!this.TryConvertType(this._targetType.Peek(), ref this._value, out errorMessage))
                        {
                            this._errors.Add(new TypeError(this._targetType.Peek(), literalValue.Value, literalValue,
                                                           errorMessage));
                        }
                    }
                    break;
                case LiteralValueType.Null:
                    this._value = null;
                    break;
                case LiteralValueType.Boolean:
                    this._value = StringComparer.InvariantCultureIgnoreCase.Equals(literalValue.Value, "$true");
                    if (this._targetType.Peek() != typeof(bool))
                    {
                        string errorMessage;
                        if (!this.TryConvertType(this._targetType.Peek(), ref this._value, out errorMessage))
                        {
                            this._errors.Add(new TypeError(this._targetType.Peek(), this.Value, literalValue,
                                                           errorMessage));
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        void IAstVisitor.Visit(Sequence sequence)
        {
            Type targetType = this._targetType.Peek();
            Type elementType;

            Action<int, object> elementHandler;
            object nextValue;

            if (targetType.IsArray)
            {
                elementType = targetType.GetElementType();
                nextValue = Array.CreateInstance(elementType, sequence.Elements.Length);
                Array newArray = (Array) nextValue;
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

                    nextValue = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                    MethodInfo addMethod = nextValue.GetType().GetMethod("Add", new[] {elementType});
                    Debug.Assert(addMethod != null, "addMethod is null. Add some error checking here.");
                    elementHandler = (i, o) => addMethod.Invoke(nextValue, new[] {o});
                }
                else
                {
                    MethodInfo addMethod =
                        targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                            .Where(m => StringComparer.InvariantCultureIgnoreCase.Equals("Add", m.Name))
                            .Single(m => m.GetParameters().Length == 1);
                    elementType = addMethod.GetParameters()[0].ParameterType;
                    nextValue = Activator.CreateInstance(targetType);
                    elementHandler = (i, o) => addMethod.Invoke(nextValue, new[] {o});
                }
            }

            this._targetType.Push(elementType);
            for (int elemNum = 0; elemNum < sequence.Elements.Length; elemNum++)
            {
                AstNode element = sequence.Elements[elemNum];
                element.Visit(this);
                elementHandler(elemNum, this.Value);
            }
            this._targetType.Pop();
            this._value = nextValue;
        }

        void IAstVisitor.Visit(AssociativeArray array)
        {
            Type targetType = this._targetType.Peek();
            if (targetType.IsInterface)
                this.HandleInterfaceBasedAssocArray(array, targetType);
            else if (targetType.IsArray)
                this.HandleArrayBasedAssocArray(array, targetType);
            else
                this.HandleMethodBasedAssocArray(array, targetType);
        }

        #region AssociativeArray Handling

        private void HandleMethodBasedAssocArray(AssociativeArray array, Type targetType)
        {
            MethodInfo addMethod = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(
                    m => StringComparer.OrdinalIgnoreCase.Equals("Add", m.Name) &&
                         m.GetParameters().Length == 2);

            if (addMethod != null)
            {
                ConstructorInfo defaultCtor = targetType.GetConstructor(Type.EmptyTypes);

                if (defaultCtor != null)
                {
                    object newObject = defaultCtor.Invoke(null);

                    var addParams = addMethod.GetParameters();

                    Type keyType = addParams[0].ParameterType;
                    Type valueType = addParams[1].ParameterType;
                    Action<int, object, object> handleKeyValuePair =
                        (index, key, value) =>
                        addMethod.Invoke(newObject, new[] {key, value});

                    this.FillAssocArray(array, keyType, valueType, handleKeyValuePair);

                    this._value = newObject;
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
                throw new NotSupportedException(
                    string.Format(Resources.Exceptions.UnsupportedParameterTypeNoAddMethod,
                                  targetType.FullName));
            }
        }

        private void HandleInterfaceBasedAssocArray(AssociativeArray array, Type targetType)
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

        private void HandleArrayBasedAssocArray(AssociativeArray array, Type arrayType)
        {
            Type elementType = arrayType.GetElementType();

            ConstructorInfo[] ctors = elementType.GetConstructors(BindingFlags.Public);

            ConstructorInfo[] matches = ctors.Where(ct => ct.GetParameters().Length == 2).ToArray();

            if (matches.Length == 1)
            {
                Array newArray = Array.CreateInstance(elementType, array.Elements.Length);
                Action<int, object, object> handleKeyValuePair =
                    (index, key, value) =>
                        {
                            object newValue = matches[0].Invoke(new[] {key, value});
                            newArray.SetValue(newValue, index);
                        };

                // this is slightly wasteful as we already asked for the parameters once, but it's a one-off operation.
                ParameterInfo[] parameters = matches[0].GetParameters();

                this.FillAssocArray(array, parameters[0].ParameterType, parameters[1].ParameterType, handleKeyValuePair);

                // set return value
                this._value = newArray;
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
                element.Key.Visit(this);
                this._targetType.Pop();
                object keyValue = this._value;

                this._targetType.Push(valueType);
                element.Value.Visit(this);
                this._targetType.Pop();
                object valueValue = this._value;

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