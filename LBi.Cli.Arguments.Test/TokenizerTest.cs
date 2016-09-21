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

using System.Linq;
using LBi.Cli.Arguments.Parsing;
using Xunit;

namespace LBi.CLI.Arguments.Test
{
    public class TokenizerTest
    {
        [Fact]
        public void TokenizeSingleParameterWithSimpleValue()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("-myparam test").ToArray();

            Assert.Equal(tokens.Select(t => t.Type),
                         new[]
                         {
                             TokenType.ParameterName,
                             TokenType.StringValue,
                             TokenType.EndOfString,
                         });
        }

        [Fact]
        public void TokenizeMultipleParameterWith()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("-myparam test -my2ndparam $true -dictp @{} -arrayp @()").ToArray();

            Assert.Equal(tokens.Select(t => t.Type),
                         new[]
                         {
                             TokenType.ParameterName,
                             TokenType.StringValue,
                             TokenType.ParameterName,
                             TokenType.BoolValue,
                             TokenType.ParameterName,
                             TokenType.DictionaryStart,
                             TokenType.DictionaryEnd,
                             TokenType.ParameterName,
                             TokenType.ListStart,
                             TokenType.ListEnd,
                             TokenType.EndOfString
                         });
        }

        [Fact]
        public void TokenizeMultiplePositionalParameters()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("test $true @{} @()").ToArray();
            Assert.Equal(tokens.Select(t => t.Type),
                         new[]
                         {
                             TokenType.StringValue,
                             TokenType.BoolValue,
                             TokenType.DictionaryStart,
                             TokenType.DictionaryEnd,
                             TokenType.ListStart,
                             TokenType.ListEnd,
                             TokenType.EndOfString,
                         });
        }

        [Fact]
        public void TokenizeStringValueWithoutQuotes()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("test").ToArray();
            Assert.Equal(2, tokens.Length);
            Assert.Equal(4, tokens[0].Length);
            Assert.Equal(0, tokens[0].Position);
            Assert.Equal("test", tokens[0].Value);
            Assert.Equal(TokenType.StringValue, tokens[0].Type);
            Assert.Equal(TokenType.EndOfString, tokens[1].Type);
        }

        [Fact]
        public void TokenizeStringValueWithSingleQuotes()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("'te\"`'st'").ToArray();
            Assert.Equal(2, tokens.Length);
            Assert.Equal(9, tokens[0].Length);
            Assert.Equal(0, tokens[0].Position);
            Assert.Equal("te\"'st", tokens[0].Value);
            Assert.Equal(TokenType.StringValue, tokens[0].Type);
            Assert.Equal(TokenType.EndOfString, tokens[1].Type);
        }

        [Fact]
        public void TokenizeStringValueWithDoubleQuotes()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("\"te'`\"st\"").ToArray();
            Assert.Equal(2, tokens.Length);
            Assert.Equal(9, tokens[0].Length);
            Assert.Equal(0, tokens[0].Position);
            Assert.Equal("te'\"st", tokens[0].Value);
            Assert.Equal(TokenType.StringValue, tokens[0].Type);
            Assert.Equal(TokenType.EndOfString, tokens[1].Type);
        }

        [Fact]
        public void TokenizeNullValue()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("$null").ToArray();
            Assert.Equal(2, tokens.Length);
            Assert.Equal(5, tokens[0].Length);
            Assert.Equal(0, tokens[0].Position);
            Assert.Equal("$null", tokens[0].Value);
            Assert.Equal(TokenType.NullValue, tokens[0].Type);
            Assert.Equal(TokenType.EndOfString, tokens[1].Type);
        }

        [Fact]
        public void TokenizeBooleanTrue()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("$true").ToArray();
            Assert.Equal(2, tokens.Length);
            Assert.Equal(5, tokens[0].Length);
            Assert.Equal(0, tokens[0].Position);
            Assert.Equal("$true", tokens[0].Value);
            Assert.Equal(TokenType.BoolValue, tokens[0].Type);
            Assert.Equal(TokenType.EndOfString, tokens[1].Type);
        }

        [Fact]
        public void TokenizeImplicitList()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("1,2,3,4,5").ToArray();
            Assert.Equal(10, tokens.Length);
            for (int i = 0; i < (tokens.Length - 1) / 2; i++)
            {
                Assert.Equal(TokenType.ListValueSeperator, tokens[i * 2 + 1].Type);
            }
            Assert.Equal(TokenType.EndOfString, tokens[tokens.Length - 1].Type);
        }

        [Fact]
        public void TokenizeSwitchParameter()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("-Switch:$false").ToArray();
            Assert.Equal(3, tokens.Length);
            Assert.Equal("Switch", tokens[0].Value);
            Assert.Equal("$false", tokens[1].Value);
            Assert.Equal(TokenType.SwitchParameter, tokens[0].Type);
            Assert.Equal(TokenType.BoolValue, tokens[1].Type);
            Assert.Equal(TokenType.EndOfString, tokens[2].Type);
        }

        [Fact]
        public void TokenizeSwitchParameter_WithList()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("-Switch:@($true,$true)").ToArray();
            Assert.Equal(6, tokens.Length);
            Assert.Equal("Switch", tokens[0].Value);
            Assert.Equal(TokenType.SwitchParameter, tokens[0].Type);
        }

        [Fact]
        public void TokenizeBooleanFalse()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("$false").ToArray();
            Assert.Equal(2, tokens.Length);
            Assert.Equal(6, tokens[0].Length);
            Assert.Equal(0, tokens[0].Position);
            Assert.Equal("$false", tokens[0].Value);
            Assert.Equal(TokenType.BoolValue, tokens[0].Type);
            Assert.Equal(TokenType.EndOfString, tokens[1].Type);
        }

        [Fact]
        public void TokenizeIntValue()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("1234").ToArray();
            Assert.Equal(2, tokens.Length);
            Assert.Equal(4, tokens[0].Length);
            Assert.Equal(0, tokens[0].Position);
            Assert.Equal("1234", tokens[0].Value);
            Assert.Equal(TokenType.NumericValue, tokens[0].Type);
            Assert.Equal(TokenType.EndOfString, tokens[1].Type);
        }

        [Fact]
        public void TokenizeDoubleValue()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("12.34").ToArray();
            Assert.Equal(2, tokens.Length);
            Assert.Equal(5, tokens[0].Length);
            Assert.Equal(0, tokens[0].Position);
            Assert.Equal("12.34", tokens[0].Value);
            Assert.Equal(TokenType.NumericValue, tokens[0].Type);
            Assert.Equal(TokenType.EndOfString, tokens[1].Type);
        }

        [Fact]
        public void TokenizeInvalidDoubleAsString()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("12.3.4").ToArray();
            Assert.Equal(2, tokens.Length);
            Assert.Equal(6, tokens[0].Length);
            Assert.Equal(0, tokens[0].Position);
            Assert.Equal("12.3.4", tokens[0].Value);
            Assert.Equal(TokenType.StringValue, tokens[0].Type);
            Assert.Equal(TokenType.EndOfString, tokens[1].Type);
        }


        [Fact]
        public void TokenizeEmptyList()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("@()").ToArray();
            Assert.Equal(3, tokens.Length);

            Assert.Equal(2, tokens[0].Length);
            Assert.Equal(0, tokens[0].Position);
            Assert.Equal("@(", tokens[0].Value);
            Assert.Equal(TokenType.ListStart, tokens[0].Type);

            Assert.Equal(1, tokens[1].Length);
            Assert.Equal(2, tokens[1].Position);
            Assert.Equal(")", tokens[1].Value);
            Assert.Equal(TokenType.ListEnd, tokens[1].Type);

            Assert.Equal(TokenType.EndOfString, tokens[2].Type);
        }

        [Fact]
        public void TokenizeListWithSingleItem()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("@($true)").ToArray();
            Assert.Equal(4, tokens.Length);

            Assert.Equal(2, tokens[0].Length);
            Assert.Equal(0, tokens[0].Position);
            Assert.Equal("@(", tokens[0].Value);
            Assert.Equal(TokenType.ListStart, tokens[0].Type);

            Assert.Equal(5, tokens[1].Length);
            Assert.Equal(2, tokens[1].Position);
            Assert.Equal("$true", tokens[1].Value);
            Assert.Equal(TokenType.BoolValue, tokens[1].Type);

            Assert.Equal(1, tokens[2].Length);
            Assert.Equal(7, tokens[2].Position);
            Assert.Equal(")", tokens[2].Value);
            Assert.Equal(TokenType.ListEnd, tokens[2].Type);

            Assert.Equal(TokenType.EndOfString, tokens[3].Type);
        }

        [Fact]
        public void TokenizeListWithSingleUnqotedStringItem()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("@(true)").ToArray();
            Assert.Equal(4, tokens.Length);

            Assert.Equal(2, tokens[0].Length);
            Assert.Equal(0, tokens[0].Position);
            Assert.Equal("@(", tokens[0].Value);
            Assert.Equal(TokenType.ListStart, tokens[0].Type);

            Assert.Equal(4, tokens[1].Length);
            Assert.Equal(2, tokens[1].Position);
            Assert.Equal("true", tokens[1].Value);
            Assert.Equal(TokenType.StringValue, tokens[1].Type);

            Assert.Equal(1, tokens[2].Length);
            Assert.Equal(6, tokens[2].Position);
            Assert.Equal(")", tokens[2].Value);
            Assert.Equal(TokenType.ListEnd, tokens[2].Type);

            Assert.Equal(TokenType.EndOfString, tokens[3].Type);
        }

        [Fact]
        public void TokenizeListWithSingleIntItem()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("@(1234)").ToArray();
            Assert.Equal(4, tokens.Length);

            Assert.Equal(2, tokens[0].Length);
            Assert.Equal(0, tokens[0].Position);
            Assert.Equal("@(", tokens[0].Value);
            Assert.Equal(TokenType.ListStart, tokens[0].Type);

            Assert.Equal(4, tokens[1].Length);
            Assert.Equal(2, tokens[1].Position);
            Assert.Equal("1234", tokens[1].Value);
            Assert.Equal(TokenType.NumericValue, tokens[1].Type);

            Assert.Equal(1, tokens[2].Length);
            Assert.Equal(6, tokens[2].Position);
            Assert.Equal(")", tokens[2].Value);
            Assert.Equal(TokenType.ListEnd, tokens[2].Type);

            Assert.Equal(TokenType.EndOfString, tokens[3].Type);
        }

        [Fact]
        public void TokenizeNonEmptyList()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("@($null, $true ,  $false, '1234', 1234 )").ToArray();
            Assert.Equal(tokens.Select(t => t.Type),
                         new[]
                         {
                             TokenType.ListStart,
                             TokenType.NullValue,
                             TokenType.BoolValue,
                             TokenType.BoolValue,
                             TokenType.StringValue,
                             TokenType.NumericValue,
                             TokenType.ListEnd,
                             TokenType.EndOfString,
                         });
        }

        [Fact]
        public void TokenizeNestedList()
        {
            Tokenizer tokenizer = new Tokenizer();

            Token[] tokens = tokenizer.Tokenize("@($null, @() ,  @('ab', @('cd')))").ToArray();

            Assert.Equal(tokens.Select(t => t.Type),
                         new[]
                         {
                             TokenType.ListStart,
                             TokenType.NullValue,
                             TokenType.ListStart,
                             TokenType.ListEnd,
                             TokenType.ListStart,
                             TokenType.StringValue,
                             TokenType.ListStart,
                             TokenType.StringValue,
                             TokenType.ListEnd,
                             TokenType.ListEnd,
                             TokenType.ListEnd,
                             TokenType.EndOfString,
                         });
        }


        [Fact]
        public void TokenizeEmptyDictionary()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("@{}").ToArray();
            Assert.Equal(3, tokens.Length);

            Assert.Equal(2, tokens[0].Length);
            Assert.Equal(0, tokens[0].Position);
            Assert.Equal("@{", tokens[0].Value);
            Assert.Equal(TokenType.DictionaryStart, tokens[0].Type);

            Assert.Equal(1, tokens[1].Length);
            Assert.Equal(2, tokens[1].Position);
            Assert.Equal("}", tokens[1].Value);
            Assert.Equal(TokenType.DictionaryEnd, tokens[1].Type);

            Assert.Equal(TokenType.EndOfString, tokens[2].Type);
        }

        [Fact]
        public void TokenizeNonEmptyDictionary()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("@{abc='foo'}").ToArray();

            Assert.Equal(tokens.Select(t => t.Type),
                         new[]
                         {
                             TokenType.DictionaryStart,
                             TokenType.StringValue,
                             TokenType.StringValue,
                             TokenType.DictionaryEnd,
                             TokenType.EndOfString,
                         });
        }


        [Fact]
        public void TokenizeNestedDictionary()
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize("@{abc=@{a=b};@{a=b} = c}").ToArray();

            Assert.Equal(tokens.Select(t => t.Type),
                         new[]
                         {
                             TokenType.DictionaryStart,
                             TokenType.StringValue,
                             TokenType.DictionaryStart,
                             TokenType.StringValue,
                             TokenType.StringValue,
                             TokenType.DictionaryEnd,
                             TokenType.DictionaryStart,
                             TokenType.StringValue,
                             TokenType.StringValue,
                             TokenType.DictionaryEnd,
                             TokenType.StringValue,
                             TokenType.DictionaryEnd,
                             TokenType.EndOfString,
                         });
        }
    }
}
