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
using System.ComponentModel;
using LBi.Cli.Arguments.Parsing.Ast;

namespace LBi.Cli.Arguments
{
    public class ValueBuilder : IAstVisitor
    {
        protected Stack<Type> TargetType;
        private List<TypeError> _errors;
        protected object _value;

        public ValueBuilder()
        {
            this._errors = new List<TypeError>();
            this.TargetType = new Stack<Type>();
        }

        public bool Build(Type propertyType, AstNode astNode)
        {
            this.TargetType.Push(propertyType);

            astNode.Visit(this);


            return this._errors.Count == 0;
        }

        public IEnumerable<TypeError> Errors { get { return this._errors; } }

        public object Value
        {
            get { return _value; }
        }

        private bool TryConvertType(Type targetType, object input, out object output)
        {
            bool ret = true;
            if (targetType.IsInstanceOfType(input))
            {
                output = input;
            }
            else
            {
                var targetConverter = TypeDescriptor.GetConverter(targetType);
                var sourceConverter = TypeDescriptor.GetConverter(input);
                if (targetConverter.CanConvertFrom(input.GetType()))
                {
                    output = targetConverter.ConvertFrom(input);
                }
                else if (sourceConverter.CanConvertTo(targetType))
                {
                    output = sourceConverter.ConvertTo(input, targetType);
                }
                else if (targetConverter.CanConvertFrom(typeof(string)))
                {
                    output = targetConverter.ConvertFromInvariantString(input.ToString());
                }
                else
                {
                    output = null;
                    ret = false;
                }
            }

            return ret;
        }


        #region Implementation of IAstVisitor

        void IAstVisitor.Visit(LiteralValue literalValue)
        {
            switch (literalValue.Type)
            {
                case LiteralValueType.Numeric:
                    // this looks pretty evil
                    sbyte signedByte;
                    if (sbyte.TryParse(literalValue.Value, out signedByte))
                        this._value = signedByte;
                    else
                    {
                        byte usignedByte;
                        if (byte.TryParse(literalValue.Value, out usignedByte))
                            this._value = usignedByte;
                        else
                        {
                            short signedShort;
                            if (short.TryParse(literalValue.Value, out signedShort))
                                this._value = signedShort;
                            else
                            {
                                ushort unsignedShort;
                                if (ushort.TryParse(literalValue.Value, out unsignedShort))
                                    this._value = unsignedShort;
                                else
                                {
                                    int signedInt;
                                    if (int.TryParse(literalValue.Value, out signedInt))
                                        this._value = signedInt;
                                    else
                                    {
                                        uint unsignedInt;
                                        if (uint.TryParse(literalValue.Value, out unsignedInt))
                                            this._value = unsignedInt;
                                        else
                                        {
                                            long signedLong;
                                            if (long.TryParse(literalValue.Value, out signedLong))
                                                this._value = signedLong;
                                            else
                                            {
                                                ulong unsignedLong;
                                                if (ulong.TryParse(literalValue.Value, out unsignedLong))
                                                    this._value = unsignedLong;
                                                else
                                                {
                                                    Single single;
                                                    if (Single.TryParse(literalValue.Value, out single))
                                                        this._value = single;
                                                    else
                                                    {
                                                        Double dble;
                                                        if (Double.TryParse(literalValue.Value, out dble))
                                                            this._value = dble;
                                                        else
                                                        {
                                                            Decimal dec;
                                                            if (Decimal.TryParse(literalValue.Value, out dec))
                                                                this._value = dec;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (!this.TryConvertType(this.TargetType.Peek(), this.Value, out this._value))
                    {
                        this._errors.Add(new TypeError(this.TargetType.Peek(), this.Value, literalValue));
                    }

                    break;
                case LiteralValueType.String:
                    this._value = literalValue.Value;
                    if (this.TargetType.Peek() != typeof(string))
                    {
                        if (!this.TryConvertType(this.TargetType.Peek(), this.Value, out this._value))
                        {
                            this._errors.Add(new TypeError(this.TargetType.Peek(), this.Value, literalValue));
                        }
                    }
                    break;
                case LiteralValueType.Null:
                    this._value = null;
                    break;
                case LiteralValueType.Boolean:
                    this._value = StringComparer.InvariantCultureIgnoreCase.Equals(literalValue.Value, "$true");
                    if (this.TargetType.Peek() != typeof(bool))
                    {
                        if (!this.TryConvertType(this.TargetType.Peek(), this.Value, out this._value))
                        {
                            this._errors.Add(new TypeError(this.TargetType.Peek(), this.Value, literalValue));
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        void IAstVisitor.Visit(Sequence sequence)
        {
            List<object> values = new List<object>();
            foreach (AstNode element in sequence.Elements)
            {
                element.Visit(this);
                values.Add(this.Value);
            }
            this._value = values;
        }

        void IAstVisitor.Visit(AssociativeArray array)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}