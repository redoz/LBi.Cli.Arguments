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
using System.Linq;
using LBi.Cli.Arguments.Binding;
using LBi.Cli.Arguments.Parsing;
using LBi.Cli.Arguments.Parsing.Ast;
using Xunit;
using Xunit.Extensions;

namespace LBi.CLI.Arguments.Test
{
    // ReSharper disable InconsistentNaming
    public class ValueBuilderTest
    {
        private class SourceInfo : ISourceInfo
        {
            public static readonly ISourceInfo Empty = new SourceInfo();

            private SourceInfo()
            {
                this.Position = 0;
                this.Length = 0;
            }

            public int Position { get; private set; }
            public int Length { get; private set; }
        }

        [Fact]
        public void String_Nop()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(string),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "test"),
                                          out value));
                Assert.Equal("test", (string)value);
                Assert.Empty(builder.Errors);
            }
        }


        [Fact]
        public void String_ToByte()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(byte),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "255"),
                                          out value));
                Assert.Equal((byte)255, (byte)value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void String_ToArrayOfString()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(string[]),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "255"),
                                          out value));
                Assert.IsType<string[]>(value);
                Assert.Equal(new[] { "255" }, value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void String_ToArrayOfObject()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(object[]),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "255"),
                                          out value));
                Assert.IsType<object[]>(value);
                Assert.Equal(new object[] { "255" }, value);
                Assert.Empty(builder.Errors);
            }
        }


        [Fact]
        public void String_To_Enum()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(ConsoleColor),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "Red"),
                                          out value));
                Assert.Equal(ConsoleColor.Red, (ConsoleColor)value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void String_To_PathInfo()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(System.IO.DirectoryInfo),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "c:\\doesnt exist\\"),
                                          out value));
                Assert.IsType<System.IO.DirectoryInfo>(value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void StringAlpha_ToByte()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.False(builder.Build(typeof(byte),
                                           new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "abc"),
                                           out value));
                Assert.NotEmpty(builder.Errors);
            }
        }

        [Fact]
        public void StringHex_ToByte()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(byte),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "0xA"),
                                          out value));

                Assert.Empty(builder.Errors);
                Assert.Equal((byte)value, 0xA);
            }
        }


        [Fact]
        public void String_ToDecimal()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(decimal),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "2.55"),
                                          out value));
                Assert.Equal((decimal)2.55, (decimal)value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void String_ToDateTime()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(DateTime),
                                          new LiteralValue(SourceInfo.Empty,
                                                           LiteralValueType.String,
                                                           "2001-02-03 04:05:06"),
                                          out value));
                Assert.Equal(new DateTime(2001, 02, 03, 04, 05, 06), (DateTime)value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void Number_ToSByte()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(sbyte),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.Numeric, "50"),
                                          out value));
                Assert.Equal((sbyte)50, (sbyte)value);
                Assert.Empty(builder.Errors);
            }
        }


        [Fact]
        public void Number_ToByte()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(byte),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.Numeric, "50"),
                                          out value));
                Assert.Equal((byte)50, (byte)value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void Number_ToSingle()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(float),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.Numeric, "2"),
                                          out value));
                Assert.Equal(2f, (float)value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void Number_ToDouble()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(double),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.Numeric, "2.55"),
                                          out value));
                Assert.Equal(2.55, (double)value, 2);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void Number_ToDecimal()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(decimal),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.Numeric, "2.55"),
                                          out value));
                Assert.Equal((decimal)2.55, (decimal)value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void String_Any_ToBoolean()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(bool),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "False"),
                                          out value));
                Assert.True((bool)value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void String_Empty_ToBoolean()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(bool),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.String, ""),
                                          out value));
                Assert.False((bool)value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void Booean_True_ToInt()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(int),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.Boolean, "$true"),
                                          out value));
                Assert.Equal(1, (int)value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void Booean_False_ToInt()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(int),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.Boolean, "$false"),
                                          out value));
                Assert.Equal(0, (int)value);
                Assert.Empty(builder.Errors);
            }
        }


        [Fact]
        public void Sequence_Empty_To_IEnumerable_of_Object()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(IEnumerable<object>),
                                          new Sequence(SourceInfo.Empty, Enumerable.Empty<AstNode>()),
                                          out value));
                Assert.Empty((IEnumerable)value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void Sequence_To_IEnumerable_of_Boolean()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(IEnumerable<bool>),
                                          new Sequence(SourceInfo.Empty,
                                                       new[]
                                                       {
                                                           new LiteralValue(SourceInfo.Empty,
                                                                            LiteralValueType.Boolean,
                                                                            "$true"),
                                                           new LiteralValue(SourceInfo.Empty,
                                                                            LiteralValueType.Boolean,
                                                                            "$false"),
                                                           new LiteralValue(SourceInfo.Empty,
                                                                            LiteralValueType.Numeric,
                                                                            "1"),
                                                           new LiteralValue(SourceInfo.Empty,
                                                                            LiteralValueType.Numeric,
                                                                            "0"),
                                                           new LiteralValue(SourceInfo.Empty,
                                                                            LiteralValueType.String,
                                                                            "Any"),
                                                           new LiteralValue(SourceInfo.Empty,
                                                                            LiteralValueType.String,
                                                                            "")
                                                       }),
                                          out value));

                Assert.Equal(new[] { true, false, true, false, true, false }, ((IEnumerable<bool>)value).ToArray());

                Assert.Empty(builder.Errors);
            }
        }


        [Fact]
        public void Sequence_To_Array_of_Boolean()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(bool[]),
                                          new Sequence(SourceInfo.Empty,
                                                       new[]
                                                       {
                                                           new LiteralValue(SourceInfo.Empty,
                                                                            LiteralValueType.Boolean,
                                                                            "$true"),
                                                           new LiteralValue(SourceInfo.Empty,
                                                                            LiteralValueType.Boolean,
                                                                            "$false"),
                                                           new LiteralValue(SourceInfo.Empty,
                                                                            LiteralValueType.Numeric,
                                                                            "1"),
                                                           new LiteralValue(SourceInfo.Empty,
                                                                            LiteralValueType.Numeric,
                                                                            "0"),
                                                           new LiteralValue(SourceInfo.Empty,
                                                                            LiteralValueType.String,
                                                                            "Any"),
                                                           new LiteralValue(SourceInfo.Empty,
                                                                            LiteralValueType.String,
                                                                            "")
                                                       }),
                                          out value));

                Assert.True(new[] { true, false, true, false, true, false }.SequenceEqual((IEnumerable<bool>)value));

                Assert.Empty(builder.Errors);
            }
        }


        [Fact]
        public void Sequence_To_Array_of_Decimal()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof(decimal[]),
                                          new Sequence(SourceInfo.Empty,
                                                       new[]
                                                       {
                                                           new LiteralValue(SourceInfo.Empty,
                                                                            LiteralValueType.Numeric,
                                                                            "1"),
                                                           new LiteralValue(SourceInfo.Empty,
                                                                            LiteralValueType.String,
                                                                            "2")
                                                       }),
                                          out value));

                Assert.Equal(new[] { 1m, 2m }, (decimal[])value);

                Assert.Empty(builder.Errors);
            }
        }


        [Theory]
        [InlineData(typeof(IDictionary<string, string>), "Key", "Value")]
        [InlineData(typeof(IDictionary<string, int>), "Key", "Value")]
        [InlineData(typeof(IDictionary<int, string>), "Key", "Value")]
        [InlineData(typeof(IDictionary<int, int>), "Key", "Value")]

        [InlineData(typeof(Dictionary<string, string>), "Key", "Value")]
        [InlineData(typeof(Dictionary<string, int>), "Key", "Value")]
        [InlineData(typeof(Dictionary<int, string>), "Key", "Value")]
        [InlineData(typeof(Dictionary<int, int>), "Key", "Value")]

        [InlineData(typeof(KeyValuePair<string, string>[]), "Key", "Value")]
        [InlineData(typeof(KeyValuePair<string, int>[]), "Key", "Value")]
        [InlineData(typeof(KeyValuePair<int, string>[]), "Key", "Value")]
        [InlineData(typeof(KeyValuePair<int, int>[]), "Key", "Value")]

        [InlineData(typeof(List<KeyValuePair<string, string>>), "Key", "Value")]
        [InlineData(typeof(List<KeyValuePair<string, int>>), "Key", "Value")]
        [InlineData(typeof(List<KeyValuePair<int, string>>), "Key", "Value")]
        [InlineData(typeof(List<KeyValuePair<int, int>>), "Key", "Value")]

        [InlineData(typeof(IList<KeyValuePair<string, string>>), "Key", "Value")]
        [InlineData(typeof(IList<KeyValuePair<string, int>>), "Key", "Value")]
        [InlineData(typeof(IList<KeyValuePair<int, string>>), "Key", "Value")]
        [InlineData(typeof(IList<KeyValuePair<int, int>>), "Key", "Value")]

        [InlineData(typeof(Tuple<string, string>[]), "Item1", "Item2")]
        [InlineData(typeof(Tuple<string, int>[]), "Item1", "Item2")]
        [InlineData(typeof(Tuple<int, string>[]), "Item1", "Item2")]
        [InlineData(typeof(Tuple<int, int>[]), "Item1", "Item2")]

        [InlineData(typeof(List<Tuple<string, string>>), "Item1", "Item2")]
        [InlineData(typeof(List<Tuple<string, int>>), "Item1", "Item2")]
        [InlineData(typeof(List<Tuple<int, string>>), "Item1", "Item2")]
        [InlineData(typeof(List<Tuple<int, int>>), "Item1", "Item2")]

        [InlineData(typeof(IList<Tuple<string, string>>), "Item1", "Item2")]
        [InlineData(typeof(IList<Tuple<string, int>>), "Item1", "Item2")]
        [InlineData(typeof(IList<Tuple<int, string>>), "Item1", "Item2")]
        [InlineData(typeof(IList<Tuple<int, int>>), "Item1", "Item2")]
        public void AssocArray(Type targetType, string keyName, string valueName)
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(targetType,
                                          new AssociativeArray(
                                                               SourceInfo.Empty,
                                                               new[]
                                                               {
                                                                   new KeyValuePair<AstNode, AstNode>(
                                                                                                      new LiteralValue(SourceInfo.Empty,
                                                                                                                       LiteralValueType.Numeric,
                                                                                                                       "1"),
                                                                                                      new LiteralValue(SourceInfo.Empty,
                                                                                                                       LiteralValueType.String,
                                                                                                                       "2")),
                                                                   new KeyValuePair<AstNode, AstNode>(
                                                                                                      new LiteralValue(SourceInfo.Empty,
                                                                                                                       LiteralValueType.Numeric,
                                                                                                                       "3"),
                                                                                                      new LiteralValue(SourceInfo.Empty,
                                                                                                                       LiteralValueType.String,
                                                                                                                       "4"))
                                                               }),
                                          out value));

                Assert.IsAssignableFrom(targetType, value);

                Assert.IsAssignableFrom<IEnumerable>(value);

                Assert.Equal(2, ((IEnumerable)value).Cast<object>().Count());

                int i = 0;
                foreach (object kvp in (IEnumerable)value)
                {
                    object keyValue = kvp.GetType().GetProperty(keyName).GetValue(kvp, null);
                    object valueValue = kvp.GetType().GetProperty(valueName).GetValue(kvp, null);

                    switch (i)
                    {
                        case 0:
                            Assert.Equal("1", keyValue.ToString());
                            Assert.Equal("2", valueValue.ToString());
                            break;
                        case 1:
                            Assert.Equal("3", keyValue.ToString());
                            Assert.Equal("4", valueValue.ToString());
                            break;
                    }

                    i++;
                }

                Assert.Empty(builder.Errors);
            }
        }

        [InlineData(typeof(ILookup<string, string>))]
        [InlineData(typeof(ILookup<string, int>))]
        [InlineData(typeof(ILookup<int, string>))]
        [InlineData(typeof(ILookup<int, int>))]
        [Theory]
        public void AssocArray_To_ILookup(Type targetType)
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(targetType,
                                          new AssociativeArray(
                                                               SourceInfo.Empty,
                                                               new[]
                                                               {
                                                                   new KeyValuePair<AstNode, AstNode>(
                                                                                                      new LiteralValue(SourceInfo.Empty,
                                                                                                                       LiteralValueType.Numeric,
                                                                                                                       "1"),
                                                                                                      new LiteralValue(SourceInfo.Empty,
                                                                                                                       LiteralValueType.String,
                                                                                                                       "2")),
                                                                   new KeyValuePair<AstNode, AstNode>(
                                                                                                      new LiteralValue(SourceInfo.Empty,
                                                                                                                       LiteralValueType.Numeric,
                                                                                                                       "1"),
                                                                                                      new LiteralValue(SourceInfo.Empty,
                                                                                                                       LiteralValueType.String,
                                                                                                                       "4"))
                                                               }),
                                          out value));

                Assert.IsAssignableFrom(targetType, value);

                Assert.IsAssignableFrom<IEnumerable>(value);

                Assert.Equal(1, ((IEnumerable)value).Cast<object>().Count());

                object group = ((IEnumerable)value).Cast<object>().Single();

                Assert.Equal("1", group.GetType().GetProperty("Key").GetValue(group, null).ToString());

                object[] values = ((IEnumerable)group).Cast<object>().ToArray();

                Assert.Equal(2, values.Length);

                Assert.Equal("2", values[0].ToString());

                Assert.Equal("4", values[1].ToString());

                Assert.Empty(builder.Errors);
            }
        }
    }

    // ReSharper restore InconsistentNaming
}
