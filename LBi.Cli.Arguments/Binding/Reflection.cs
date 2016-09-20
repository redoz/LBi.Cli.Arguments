﻿/*
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
