// 
//  TeslaCAN
// 
//  Copyright 2020 Martin Gerczuk
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

namespace Dbc
{
    public partial class DbcParser
    {
        public partial class NumberContext
        {
            public double ToDouble()
            {
                var s = GetText();
                if (s.StartsWith("."))
                    s = "0" + s;
                return double.Parse(s);
            }
        }

    }
}