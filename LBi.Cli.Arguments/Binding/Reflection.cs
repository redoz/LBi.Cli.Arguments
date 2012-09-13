using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LBi.Cli.Arguments.Binding
{
    public static class Reflection
    {
        public static bool IsOfGenericType(this Type type, Type genType, out Type[] genArgs)
        {
            if (type.IsGenericType)
            {
                Type genTypeDef = type.GetGenericTypeDefinition();
                if (genTypeDef == genType)
                {
                    genArgs = type.GetGenericArguments();
                    return true;
                }
            }

            genArgs = null;
            return false;
        }
    }
}
