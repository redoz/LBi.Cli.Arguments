using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LBi.Cli.Arguments;
using LBi.Cli.Arguments.Binding;
using LBi.Cli.Arguments.Parsing;
using LBi.Cli.Arguments.Parsing.Ast;
using Xunit;

namespace LBi.CLI.Arguments.Test
{
    public class ValueBuilderTest
    {
        class SourceInfo : ISourceInfo
        {
            public static readonly ISourceInfo Empty = new SourceInfo();

            public SourceInfo()
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
            ValueBuilder builder = new ValueBuilder();

            Assert.True(builder.Build(typeof(string), new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "test")));
            Assert.Equal("test", (string)builder.Value);
            Assert.Empty(builder.Errors);
        }


        [Fact]
        public void String_ToByte()
        {
            ValueBuilder builder = new ValueBuilder();
            Assert.True(builder.Build(typeof(byte), new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "255")));
            Assert.Equal((byte)255, (byte)builder.Value);
            Assert.Empty(builder.Errors);
        }


        [Fact]
        public void String_ToDecimal()
        {
            ValueBuilder builder = new ValueBuilder();
            Assert.True(builder.Build(typeof(decimal), new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "2.55")));
            Assert.Equal((decimal)2.55, (decimal)builder.Value);
            Assert.Empty(builder.Errors);
        }

        [Fact]
        public void String_ToDateTime()
        {
            ValueBuilder builder = new ValueBuilder();
            Assert.True(builder.Build(typeof(DateTime), new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "2001-02-03 04:05:06")));
            Assert.Equal(new DateTime(2001, 02, 03, 04, 05, 06), (DateTime)builder.Value);
            Assert.Empty(builder.Errors);
        }

        [Fact]
        public void Number_ToSByte()
        {
            ValueBuilder builder = new ValueBuilder();
            Assert.True(builder.Build(typeof(sbyte), new LiteralValue(SourceInfo.Empty, LiteralValueType.Numeric, "50")));
            Assert.Equal((sbyte)50, (sbyte)builder.Value);
            Assert.Empty(builder.Errors);
        }


        [Fact]
        public void Number_ToByte()
        {
            ValueBuilder builder = new ValueBuilder();
            Assert.True(builder.Build(typeof(byte), new LiteralValue(SourceInfo.Empty, LiteralValueType.Numeric, "50")));
            Assert.Equal((byte)50, (byte)builder.Value);
            Assert.Empty(builder.Errors);
        }

        [Fact]
        public void Number_ToSingle()
        {
            ValueBuilder builder = new ValueBuilder();
            Assert.True(builder.Build(typeof(Single), new LiteralValue(SourceInfo.Empty, LiteralValueType.Numeric, "2")));
            Assert.Equal((Single)2, (Single)builder.Value);
            Assert.Empty(builder.Errors);
        }

        [Fact]
        public void Number_ToDouble()
        {
            ValueBuilder builder = new ValueBuilder();
            Assert.True(builder.Build(typeof(double), new LiteralValue(SourceInfo.Empty, LiteralValueType.Numeric, "2.55")));
            Assert.Equal((double)2.55, (double)builder.Value, 2);
            Assert.Empty(builder.Errors);
        }

        [Fact]
        public void Number_ToDecimal()
        {
            ValueBuilder builder = new ValueBuilder();
            Assert.True(builder.Build(typeof(decimal), new LiteralValue(SourceInfo.Empty, LiteralValueType.Numeric, "2.55")));
            Assert.Equal((decimal)2.55, (decimal)builder.Value);
            Assert.Empty(builder.Errors);
        }

        [Fact]
        public void String_Any_ToBoolean()
        {
            ValueBuilder builder = new ValueBuilder();
            Assert.True(builder.Build(typeof(bool), new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "False")));
            Assert.True((bool)builder.Value);
            Assert.Empty(builder.Errors);
        }

        [Fact]
        public void String_Empty_ToBoolean()
        {
            ValueBuilder builder = new ValueBuilder();
            Assert.True(builder.Build(typeof(bool), new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "")));
            Assert.False((bool)builder.Value);
            Assert.Empty(builder.Errors);
        }

        [Fact]
        public void Booean_True_ToInt()
        {
            ValueBuilder builder = new ValueBuilder();
            Assert.True(builder.Build(typeof(int), new LiteralValue(SourceInfo.Empty, LiteralValueType.Boolean, "$true")));
            Assert.Equal(1, (int)builder.Value);
            Assert.Empty(builder.Errors);
        }

        [Fact]
        public void Booean_False_ToInt()
        {
            ValueBuilder builder = new ValueBuilder();
            Assert.True(builder.Build(typeof(int), new LiteralValue(SourceInfo.Empty, LiteralValueType.Boolean, "$false")));
            Assert.Equal(0, (int)builder.Value);
            Assert.Empty(builder.Errors);
        }


        [Fact]
        public void Sequence_Empty_ToIEnumerable_of_Object()
        {
            ValueBuilder builder = new ValueBuilder();
            Assert.True(builder.Build(typeof (IEnumerable<object>),
                                      new Sequence(SourceInfo.Empty, Enumerable.Empty<AstNode>())));
            Assert.Empty((IEnumerable)builder.Value);
            Assert.Empty(builder.Errors);
        }

        [Fact]
        public void Sequence_ToIEnumerable_of_Boolean()
        {
            ValueBuilder builder = new ValueBuilder();
            Assert.True(builder.Build(typeof (IEnumerable<bool>),
                                      new Sequence(SourceInfo.Empty,
                                                   new[]
                                                       {
                                                           new LiteralValue(SourceInfo.Empty, LiteralValueType.Boolean, "$true"),
                                                           new LiteralValue(SourceInfo.Empty, LiteralValueType.Boolean, "$false"),
                                                           new LiteralValue(SourceInfo.Empty, LiteralValueType.Numeric, "1"),
                                                           new LiteralValue(SourceInfo.Empty, LiteralValueType.Numeric, "0"),
                                                           new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "Any"),
                                                           new LiteralValue(SourceInfo.Empty, LiteralValueType.String, "")
                                                       })));

            Assert.Equal(new[] {true, false, true, false, true, false}, ((IEnumerable<bool>) builder.Value).ToArray());

            Assert.Empty(builder.Errors);
        }



    }
}
