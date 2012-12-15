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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LBi.Cli.Arguments.Binding;

namespace LBi.Cli.Arguments
{
    [TypeConverter(typeof(SwitchTypeConverter))]
    public struct Switch
    {
        public static readonly Switch Present = new Switch(true);

        public static implicit operator Switch(bool boolean)
        {
            return new Switch(boolean);
        }


        public static implicit operator bool(Switch switchParam)
        {
            return switchParam.IsPresent;
        }

        public static bool operator ==(Switch swp, bool boolean)
        {
            return swp.IsPresent == boolean;
        }

        public static bool operator !=(Switch swp, bool boolean)
        {
            return !(swp == boolean);
        }

        public static bool operator ==(bool boolean, Switch swp)
        {
            return swp.IsPresent == boolean;
        }

        public static bool operator !=(bool boolean, Switch swp)
        {
            return !(swp == boolean);
        }

        public static bool operator ==(Switch sp1, Switch sp2)
        {
            return sp1.IsPresent == sp2.IsPresent;
        }

        public static bool operator !=(Switch sp1, Switch sp2)
        {
            return !(sp1 == sp2);
        }


        public override bool Equals(object obj)
        {
            Switch other = (Switch)obj;
            return other.IsPresent == this.IsPresent;
        }

        public override int GetHashCode()
        {
            return this.IsPresent.GetHashCode();
        }

        public readonly bool IsPresent;

        public Switch(bool present)
        {
            this.IsPresent = present;
        }
    }
}
