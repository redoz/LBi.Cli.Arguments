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
    public class IntransigentTypeConverter : ITypeConverter
    {
        protected readonly CultureInfo _culture;

        public IntransigentTypeConverter(CultureInfo cultureInfo)
        {
            this._culture = cultureInfo;
        }

        public virtual bool TryConvertType(Type targetType, ref object value, out Exception exception)
        {
            bool success = false;
            exception = null;
            object ret = value;
            if (value == null)
            {
                success = !targetType.IsValueType;
            }
            else if (targetType.IsInstanceOfType(value))
                success = true;
            else
            {
                var targetConverter = TypeDescriptor.GetConverter(targetType);
                var sourceConverter = TypeDescriptor.GetConverter(value);

                if (targetConverter.CanConvertFrom(value.GetType()))
                {
                    try
                    {
                        ret = targetConverter.ConvertFrom(null, this._culture, value);
                        success = true;
                    }
                    catch (NotSupportedException ex)
                    {
                        exception = ex;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }
                else if (sourceConverter.CanConvertTo(targetType))
                {
                    ret = sourceConverter.ConvertTo(null, this._culture, value, targetType);
                    success = true;
                }
                else
                {
                    try
                    {
                        ret = Convert.ChangeType(value, targetType, this._culture);
                        success = true;
                    }
                    catch (InvalidCastException ex)
                    {
                        exception = ex;
                    }
                    catch (FormatException ex)
                    {
                        exception = ex;
                    }
                    catch (OverflowException ex)
                    {
                        exception = ex;
                    }
                    catch (ArgumentNullException ex)
                    {
                        exception = ex;
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
                                    tmp = sourceConverter.ConvertToString(null, this._culture, value);
                                else
                                    tmp = value.ToString();

                                ret = targetConverter.ConvertFromString(null, this._culture, tmp);
                                success = true;
                            }
                            catch (Exception ex)
                            {
                                exception = ex;
                            }
                        }
                    }
                }
            }


            if (!success)
            {

                // check for ctor with single param
                IEnumerable<ConstructorInfo> ctors = targetType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                ctors = ctors.Where(ct => ct.GetParameters().Length == 1);

                foreach (var ct in ctors)
                {
                    var ctorParams = ct.GetParameters();

                    if (this.TryConvertType(ctorParams[0].ParameterType, ref ret, out exception))
                    {
                        try
                        {
                            ret = ct.Invoke(new[] { ret });
                            success = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                        }
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
                        exception = ex;
                    }
                }

            }

            value = ret;
            return success;
        }
    }
}