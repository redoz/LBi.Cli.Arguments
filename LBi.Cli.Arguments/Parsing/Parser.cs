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
using System.Diagnostics;
using LBi.Cli.Arguments.Parsing.Ast;

namespace LBi.Cli.Arguments.Parsing
{
    public class Parser
    {
        public virtual ArgumentCollection Parse(string args)
        {
            Tokenizer tokenizer = new Tokenizer();
            IEnumerable<Token> tokens = tokenizer.Tokenize(args);
            return new ArgumentCollection(args, this.Parse(tokens));
        }



        protected virtual IEnumerable<ParsedArgument> Parse(IEnumerable<Token> tokens)
        {
            using (var enu = tokens.GetEnumerator())
            {
                int argPos = 0;
                if (!enu.MoveNext())
                    yield break;

                while (enu.Current.Type != TokenType.EndOfString)
                {
                    if (enu.Current.Type == TokenType.ParameterName)
                    {
                        // with real name
                        string paramName = enu.Current.Value;

                        if (!enu.MoveNext())
                            throw new Exception("Unexpected end of token stream.");

                        yield return new ParsedArgument(paramName,
                                                        argPos,
                                                        this.GetAstNode(enu));


                    }
                    else
                    {
                        // positional only 
                        // TODO Firstparam shoud be null?
                        yield return new ParsedArgument(enu.Current.Value,
                                                        argPos,
                                                        this.GetAstNode(enu));
                    }

                    argPos++;
                }
            }
        }

        protected virtual AstNode GetAstNode(IEnumerator<Token> enumerator)
        {
            AstNode ret;
            switch (enumerator.Current.Type)
            {
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

            Debug.Assert(enumerator.Current.Type == TokenType.DictionaryStart);

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
