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

using LBi.Cli.Arguments.Parsing;

namespace LBi.Cli.Arguments
{
    public enum ErrorType
    {
        /// <summary>
        /// Unable to convert <see cref="ParsedArgument"/> to <see cref="Parameter"/> type.
        /// </summary>
        IncompatibleType,
        
        /// <summary>
        /// Required <see cref="Parameter"/> not specified.
        /// </summary>
        MissingRequiredParameter,
        
        /// <summary>
        /// <see cref="ParsedArgument"/> does not match any <see cref="Parameter"/>
        /// </summary>
        ArgumentNameMismatch,
        
        /// <summary>
        /// No matching Positional <see cref="Parameter"/>.
        /// </summary>
        ArgumentPositionMismatch,

        /// <summary>
        /// <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/> error.
        /// </summary>
        Validation,

        /// <summary>
        /// More than one <see cref="Parameter"/>  matches the <see cref="ParsedArgument"/>.
        /// </summary>
        AmbigiousName
    }
}