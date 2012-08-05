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
                return false;
            }

            return true;
        }


        #region Implementation of IAstVisitor

        void IAstVisitor.Visit(LiteralValue literalValue)
        {
            switch (literalValue.Type)
            {
                case LiteralValueType.Numeric:
                    // TODO fixthis
                    
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