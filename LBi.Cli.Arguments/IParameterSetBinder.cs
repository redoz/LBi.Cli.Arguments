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


using System.Globalization;
using LBi.Cli.Arguments.Binding;
using LBi.Cli.Arguments.Parsing;

namespace LBi.Cli.Arguments
{
    // TODO should this encapsulate the ITypeConverter & CultureInfo
    public interface IParameterSetBinder
    {
        ParameterSetResult Build(ITypeConverter typeConverter,
                                 CultureInfo cultureInfo,
                                 NodeSequence sequence,
                                 ParameterSet paramSet);

    }
}