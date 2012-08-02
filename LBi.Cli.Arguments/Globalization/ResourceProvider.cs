using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LBi.Cli.Arguments.Globalization
{
    public class ResourceProvider
    {
        public static Func<string> CreateStaticPropertyAccessor(Type helpMessageResourceType, string helpMessageResourceName)
        {
            PropertyInfo propInfo = helpMessageResourceType.GetProperty(helpMessageResourceName, BindingFlags.Static | BindingFlags.Public);
            Expression accessor = Expression.Property(null, propInfo);
            Expression<Func<string>> expr = Expression.Lambda<Func<string>>(accessor);
            return expr.Compile();
        }
    }
}