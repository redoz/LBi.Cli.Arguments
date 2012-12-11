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

using LBi.Cli.Arguments.Parsing.Ast;

namespace LBi.Cli.Arguments.Parsing
{
    public class Parser
    {
        public virtual NodeSequence Parse(string args)
        {
            Tokenizer tokenizer = new Tokenizer();
            IEnumerable<Token> tokens = tokenizer.Tokenize(args);
            return new NodeSequence(args, this.Parse(tokens));
        }



        protected virtual IEnumerable<AstNode> Parse(IEnumerable<Token> tokens)
        {
            using (var enu = tokens.GetEnumerator())
            {
                if (!enu.MoveNext())
                    throw new ArgumentException("Token stream must at the very least contain the terminator: EndOfStream", "tokens");

                while (enu.Current.Type != TokenType.EndOfString)
                {
                    yield return this.GetAstNode(enu);
                }
            }
        }

        protected virtual AstNode GetAstNode(IEnumerator<Token> enumerator)
        {
            AstNode ret;
            switch (enumerator.Current.Type)
            {
                case TokenType.ParameterName:
                    ret = new ParameterName(enumerator.Current,
                                            enumerator.Current.Value);
                    enumerator.MoveNext();
                    break;
                case TokenType.SwitchParameter:
                    Token token = enumerator.Current;
                    enumerator.MoveNext();
                    ret = new SwitchParameter(token,
                                              token.Value,
                                              this.GetAstNode(enumerator));
                    break;
                case TokenType.NumericValue:
                    ret = new LiteralValue(
                                            enumerator.Current,
                                            LiteralValueType.Numeric,
                                            enumerator.Current.Value);
                    enumerator.MoveNext();
                    break;
                case TokenType.StringValue:
                    ret = new LiteralValue(
                                            enumerator.Current,
                                            LiteralValueType.String,
                                            enumerator.Current.Value);
                    enumerator.MoveNext();
                    break;
                case TokenType.BoolValue:
                    ret = new LiteralValue(
                                            enumerator.Current,
                                            LiteralValueType.Boolean,
                                            enumerator.Current.Value);
                    enumerator.MoveNext();
                    break;
                case TokenType.NullValue:
                    ret = new LiteralValue(
                                            enumerator.Current,
                                            LiteralValueType.Null,
                                            enumerator.Current.Value);
                    enumerator.MoveNext();
                    break;
                case TokenType.ListStart:
                    ret = this.GetSequence(enumerator);
                    break;
                case TokenType.DictionaryStart:
                    ret = this.GetAssocArray(enumerator);
                    break;
                //case TokenType.DictionaryValueSeperator:
                //case TokenType.DictionaryKeySeperator:
                case TokenType.DictionaryEnd:
                //case TokenType.ListValueSeperator:
                case TokenType.ListEnd:
                    throw new Exception("Invalid token sequence.");

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return ret;
        }

        private AstNode GetAssocArray(IEnumerator<Token> enumerator)
        {
            ISourceInfo sourceInfo = enumerator.Current;
            List<KeyValuePair<AstNode, AstNode>> elements = new List<KeyValuePair<AstNode, AstNode>>();

            System.Diagnostics.Debug.Assert(enumerator.Current.Type == TokenType.DictionaryStart);

            enumerator.MoveNext();

            while (enumerator.Current.Type != TokenType.DictionaryEnd)
            {
                AstNode key = this.GetAstNode(enumerator);
                AstNode value = this.GetAstNode(enumerator);
                elements.Add(new KeyValuePair<AstNode, AstNode>(key, value));
            }

            // skip ListEnd token
            enumerator.MoveNext();

            return new AssociativeArray(sourceInfo, elements);
        }

        private AstNode GetSequence(IEnumerator<Token> enumerator)
        {
            ISourceInfo sourceInfo = enumerator.Current;

            List<AstNode> elements = new List<AstNode>();

            enumerator.MoveNext();

            while (enumerator.Current.Type != TokenType.ListEnd)
            {
                elements.Add(this.GetAstNode(enumerator));
            }

            // skip ListEnd token
            enumerator.MoveNext();

            return new Sequence(sourceInfo, elements);
        }
    }
}
