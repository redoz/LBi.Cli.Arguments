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
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace LBi.Cli.Arguments.Binding
{
    /// <summary>
    ///     <see cref="ITypeConverter" /> that uses a combinaition of <see cref="TypeConverter" /> and Reflection to try to
    ///     convert the value.
    /// </summary>
    public class IntransigentTypeConverter : ITypeConverter
    {
        public IntransigentTypeConverter()
        {
        }

        public virtual bool TryConvertType(CultureInfo culture, Type targetType, ref object value, out IEnumerable<Exception> errors)
        {
            bool success = false;
            List<Exception> outErrors = new List<Exception>();
            object ret = value;
            if (ret == null)
            {
                success = !targetType.IsValueType;
            }
            else if (targetType.IsInstanceOfType(value))
            {
                success = true;
            }
            else if (targetType.IsArray && targetType.GetElementType().IsInstanceOfType(value))
            {
                Array arr = Array.CreateInstance(targetType.GetElementType(), 1);
                arr.SetValue(value, 0);
                ret = arr;
                success = true;
            }
            else
            {
                var targetConverter = TypeDescriptor.GetConverter(targetType);
                var sourceConverter = TypeDescriptor.GetConverter(value);

                if (targetConverter.CanConvertFrom(value.GetType()))
                {
                    try
                    {
                        ret = targetConverter.ConvertFrom(null, culture, value);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        outErrors.Add(ex);
                    }
                }
                else if (sourceConverter.CanConvertTo(targetType))
                {
                    ret = sourceConverter.ConvertTo(null, culture, value, targetType);
                    success = true;
                }
                else
                {
                    try
                    {
                        ret = Convert.ChangeType(value, targetType, culture);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        outErrors.Add(ex);
                    }

                    if (!success)
                    {
                        // attempt round-trip to string
                        if (targetConverter.CanConvertFrom(typeof(string)))
                        {
                            try
                            {
                                string tmp;
                                if (sourceConverter.CanConvertTo(typeof(string)))
                                    tmp = sourceConverter.ConvertToString(null, culture, value);
                                else
                                    tmp = value.ToString();

                                ret = targetConverter.ConvertFromString(null, culture, tmp);
                                success = true;
                            }
                            catch (Exception ex)
                            {
                                outErrors.Add(ex);
                            }
                        }
                    }
                }
            }


            if (!success && value != null)
            {
                // check for ctor with single param
                IEnumerable<ConstructorInfo> ctors = targetType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                ctors = ctors.Where(ct => ct.GetParameters().Length == 1);

                foreach (var ct in ctors)
                {
                    var ctorParams = ct.GetParameters();

                    IEnumerable<Exception> exceptions;
                    if (this.TryConvertType(culture, ctorParams[0].ParameterType, ref ret, out exceptions))
                    {
                        try
                        {
                            ret = ct.Invoke(new[] { ret });
                            success = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            outErrors.Add(ex);
                        }
                    }
                    else
                    {
                        outErrors.AddRange(exceptions);
                    }
                }
                // check for static "Parse" method 

                object[] args = new[] { ret };
                MethodInfo parseMethod = targetType.GetMethod("Parse",
                                                              BindingFlags.Public | BindingFlags.Static,
                                                              null,
                                                              new[] { ret.GetType() },
                                                              null);

                if (parseMethod == null)
                {
                    args[0] = ret.ToString();
                    parseMethod = targetType.GetMethod("Parse",
                                                       BindingFlags.Public | BindingFlags.Static,
                                                       null,
                                                       new[] { typeof(string) },
                                                       null);
                }

                if (parseMethod != null)
                {
                    try
                    {
                        ret = parseMethod.Invoke(null, args);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        outErrors.Add(ex);
                    }
                }
            }

            errors = outErrors;
            value = ret;
            return success;
        }
    }
}
