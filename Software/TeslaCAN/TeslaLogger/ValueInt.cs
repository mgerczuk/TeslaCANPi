﻿// 
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

using System.Collections.Generic;
using System.Linq;
using TeslaCAN.CanDB;
using TeslaCAN.SocketCAN;

namespace TeslaCAN.TeslaLogger
{
    public class ValueInt : ValueBase
    {
        public ValueInt(Signal signal)
            : base(signal)
        {
        }

        public List<int> Values { get; } = new List<int>();

        public static int GetIntValue(Signal signal, ulong data)
        {
            var mask = (1ul << signal.BitSize) - 1ul;
            var signMask = 1ul << (signal.BitSize - 1);
            var uiValue = (data >> signal.BitPos) & mask;
            var val = (uiValue & signMask) == 0 ? (int) uiValue : (int) (uiValue | ~mask);
            return val;
        }

        #region Overrides of ValueBase

        public override void AddValue(Frame frame)
        {
            Values.Add(GetIntValue(Signal, frame.DataUlong));
        }

        public override bool HasValue => Values.Any();

        public override double Mean
        {
            get { return Values.Select(i => (double) i).Sum() / Values.Count * Signal.Factor + Signal.Offset; }
        }

        public override double Min => (Signal.Factor < 0 ? Values.Max() : Values.Min()) * Signal.Factor + Signal.Offset;

        public override double Max => (Signal.Factor < 0 ? Values.Min() : Values.Max()) * Signal.Factor + Signal.Offset;

        public override double Last => Values.Last() * Signal.Factor + Signal.Offset;

        public override void Reset()
        {
            Values.Clear();
        }

        #endregion
    }
}