using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LBi.Cli.Arguments;
using LBi.Cli.Arguments.Binding;
using LBi.Cli.Arguments.Parsing;
using LBi.Cli.Arguments.Parsing.Ast;
using Xunit;

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
                Assert.True(builder.Build(typeof (string),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "test"),
                                          out value));
                Assert.Equal("test", (string) value);
                Assert.Empty(builder.Errors);
            }
        }


        [Fact]
        public void String_ToByte()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (byte),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "255"), 
                                          out value));
                Assert.Equal((byte) 255, (byte) value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void StringAlpha_ToByte()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.False(builder.Build(typeof (byte),
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
                Assert.True(builder.Build(typeof (byte),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "0xA"),
                                          out value));
                foreach (TypeError error in builder.Errors)
                {
                    Debug.WriteLine(error.Message);
                }
                Assert.Empty(builder.Errors);
                Assert.Equal((byte) value, 0xA);
            }
        }


        [Fact]
        public void String_ToDecimal()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (decimal),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "2.55"), 
                                          out value));
                Assert.Equal((decimal) 2.55, (decimal) value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void String_ToDateTime()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (DateTime),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.String,
                                                           "2001-02-03 04:05:06"), 
                                          out value));
                Assert.Equal(new DateTime(2001, 02, 03, 04, 05, 06), (DateTime) value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void Number_ToSByte()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (sbyte),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.Numeric, "50"), 
                                          out value));
                Assert.Equal((sbyte) 50, (sbyte) value);
                Assert.Empty(builder.Errors);
            }
        }


        [Fact]
        public void Number_ToByte()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (byte),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.Numeric, "50"), 
                                          out value));
                Assert.Equal((byte) 50, (byte) value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void Number_ToSingle()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (Single),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.Numeric, "2"), 
                                          out value));
                Assert.Equal(2f, (Single) value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void Number_ToDouble()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (double),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.Numeric, "2.55"),
                                          out value));
                Assert.Equal(2.55, (double) value, 2);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void Number_ToDecimal()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (decimal),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.Numeric, "2.55"),
                                          out value));
                Assert.Equal((decimal) 2.55, (decimal) value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void String_Any_ToBoolean()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (bool),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "False"),
                                          out value));
                Assert.True((bool) value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void String_Empty_ToBoolean()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (bool), new LiteralValue(SourceInfo.Empty, LiteralValueType.String, ""),
                                          out value));
                Assert.False((bool) value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void Booean_True_ToInt()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (int),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.Boolean, "$true"),
                                          out value));
                Assert.Equal(1, (int) value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void Booean_False_ToInt()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (int),
                                          new LiteralValue(SourceInfo.Empty, LiteralValueType.Boolean, "$false"),
                                          out value));
                Assert.Equal(0, (int) value);
                Assert.Empty(builder.Errors);
            }
        }


        [Fact]
        public void Sequence_Empty_To_IEnumerable_of_Object()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (IEnumerable<object>),
                                          new Sequence(SourceInfo.Empty, Enumerable.Empty<AstNode>()), out value));
                Assert.Empty((IEnumerable) value);
                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void Sequence_To_IEnumerable_of_Boolean()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (IEnumerable<bool>),
                                          new Sequence(SourceInfo.Empty,
                                                       new[]
                                                           {
                                                               new LiteralValue(SourceInfo.Empty,
                                                                                LiteralValueType.Boolean, "$true"),
                                                               new LiteralValue(SourceInfo.Empty,
                                                                                LiteralValueType.Boolean, "$false"),
                                                               new LiteralValue(SourceInfo.Empty,
                                                                                LiteralValueType.Numeric, "1"),
                                                               new LiteralValue(SourceInfo.Empty,
                                                                                LiteralValueType.Numeric, "0"),
                                                               new LiteralValue(SourceInfo.Empty,
                                                                                LiteralValueType.String, "Any"),
                                                               new LiteralValue(SourceInfo.Empty,
                                                                                LiteralValueType.String, "")
                                                           }), 
                                          out value));

                Assert.Equal(new[] {true, false, true, false, true, false}, ((IEnumerable<bool>) value).ToArray());

                Assert.Empty(builder.Errors);
            }
        }


        [Fact]
        public void Sequence_To_Array_of_Boolean()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (bool[]),
                                          new Sequence(SourceInfo.Empty,
                                                       new[]
                                                           {
                                                               new LiteralValue(SourceInfo.Empty,
                                                                                LiteralValueType.Boolean, "$true"),
                                                               new LiteralValue(SourceInfo.Empty,
                                                                                LiteralValueType.Boolean, "$false"),
                                                               new LiteralValue(SourceInfo.Empty,
                                                                                LiteralValueType.Numeric, "1"),
                                                               new LiteralValue(SourceInfo.Empty,
                                                                                LiteralValueType.Numeric, "0"),
                                                               new LiteralValue(SourceInfo.Empty,
                                                                                LiteralValueType.String, "Any"),
                                                               new LiteralValue(SourceInfo.Empty,
                                                                                LiteralValueType.String, "")
                                                           }),
                                          out value));

                Assert.Equal(new[] {true, false, true, false, true, false}, (bool[]) value);

                Assert.Empty(builder.Errors);
            }
        }


        [Fact]
        public void Sequence_To_Array_of_Decimal()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (decimal[]),
                                          new Sequence(SourceInfo.Empty,
                                                       new[]
                                                           {
                                                               new LiteralValue(SourceInfo.Empty,
                                                                                LiteralValueType.Numeric, "1"),
                                                               new LiteralValue(SourceInfo.Empty,
                                                                                LiteralValueType.String, "2")
                                                           }),
                                          out value));

                Assert.Equal(new[] {1m, 2m}, (decimal[]) value);

                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void AssocArray_To_Array_of_KeyValuePair_of_String_String()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (KeyValuePair<string, string>[]),
                                          new AssociativeArray(SourceInfo.Empty,
                                                               new[]
                                                                   {
                                                                       new KeyValuePair<AstNode, AstNode>(
                                                                           new LiteralValue(SourceInfo.Empty,
                                                                                            LiteralValueType.Numeric,
                                                                                            "1"),
                                                                           new LiteralValue(SourceInfo.Empty,
                                                                                            LiteralValueType.String, "2"))
                                                                   }),
                                          out value));

                Assert.Equal(new[] {new KeyValuePair<string, string>("1", "2")},
                             (KeyValuePair<string, string>[]) value);

                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void AssocArray_To_Array_of_Tuple_of_String_String()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (Tuple<string, string>[]),
                                          new AssociativeArray(SourceInfo.Empty,
                                                               new[]
                                                                   {
                                                                       new KeyValuePair<AstNode, AstNode>(
                                                                           new LiteralValue(SourceInfo.Empty,
                                                                                            LiteralValueType.Numeric,
                                                                                            "1"),
                                                                           new LiteralValue(SourceInfo.Empty,
                                                                                            LiteralValueType.String, "2"))
                                                                   }),
                                          out value));

                Assert.Equal(new[] {new Tuple<string, string>("1", "2")},
                             (Tuple<string, string>[]) value);

                Assert.Empty(builder.Errors);
            }
        }

        [Fact]
        public void AssocArray_To_List_of_Tuple_of_String_String()
        {
            using (ValueBuilder builder = new ValueBuilder())
            {
                object value;
                Assert.True(builder.Build(typeof (List<Tuple<string, string>>),
                                          new AssociativeArray(SourceInfo.Empty,
                                                               new[]
                                                                   {
                                                                       new KeyValuePair<AstNode, AstNode>(
                                                                           new LiteralValue(SourceInfo.Empty,
                                                                                            LiteralValueType.Numeric,
                                                                                            "1"),
                                                                           new LiteralValue(SourceInfo.Empty,
                                                                                            LiteralValueType.String, "2"))
                                                                   }),
                                          out value));

                Assert.Equal(new List<Tuple<string, string>>(
                                 new[]
                                     {
                                         new Tuple<string, string>("1", "2")
                                     }
                                 ),
                             (List<Tuple<string, string>>) value);

                Assert.Empty(builder.Errors);
            }
        }
    }
    // ReSharper restore InconsistentNaming
}