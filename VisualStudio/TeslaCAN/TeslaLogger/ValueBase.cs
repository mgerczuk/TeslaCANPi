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

using System;
using TeslaCAN.CanDB;
using TeslaCAN.SocketCAN;

namespace TeslaCAN.TeslaLogger
{
    public abstract class ValueBase
    {
        protected ValueBase(Signal signal)
        {
            Signal = signal;
        }

        public Signal Signal { get; }

        public abstract bool HasValue { get; }

        public abstract double Mean { get; }

        public double Min => AtIndex(MinIndex);

        public abstract int MinIndex { get; }

        public abstract int MaxIndex { get; }

        public double Max => AtIndex(MaxIndex);

        public abstract double Last { get; }

        public abstract double AtIndex(int index);

        public abstract void Reset();

        public abstract void AddValue(Frame frame);

        public static ValueBase Create(Signal signal, DateTime ts)
        {
            return signal.SignalType == SignalType.Signed
                ? (ValueBase) new ValueInt(signal)
                : new ValueUInt(signal);
        }
    }
}