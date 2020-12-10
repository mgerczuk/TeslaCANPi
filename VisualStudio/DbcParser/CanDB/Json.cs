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
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TeslaCAN.CanDB
{
    public class CanDB
    {
        private readonly Lazy<IReadOnlyDictionary<uint, Message>> dict;

        public CanDB()
        {
            dict = new Lazy<IReadOnlyDictionary<uint, Message>>(() =>
            {
                var d = new Dictionary<uint, Message>();
                foreach (var message in Pdus) d.Add(message.Id, message);
                return d;
            });
        }

        public List<Message> Pdus { get; } = new List<Message>();

        [JsonIgnore] public IReadOnlyDictionary<uint, Message> PdusDict => dict.Value;
    }

    public class Message
    {
        public uint Id { get; set; }
        public string Name { get; set; }

        public List<SignalOrMux> SignalOrMuxes { get; } = new List<SignalOrMux>();
    }

    public class SignalOrMux
    {
        public Multiplex Multiplex { get; set; }
        public Signal Signal { get; set; }

        public bool ShouldSerializeMultiplex()
        {
            return Multiplex != null;
        }

        public bool ShouldSerializeSignal()
        {
            return Signal != null;
        }
    }

    public class Multiplex
    {
        public string SignalName { get; set; }

        public uint SignalValue { get; set; }

        public List<Signal> Signals { get; } = new List<Signal>();
    }

    public enum SignalType
    {
        Unsigned,
        Signed
    }

    public enum DbCycle
    {
        _5sec,
        _1min
    }

    public class Signal
    {
        public string Name { get; set; }

        public int TeslaloggerDbKey { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public SignalType SignalType { get; set; }

        public int BitPos { get; set; }

        public int BitSize { get; set; }

        public double Factor { get; set; }

        public double Offset { get; set; }

        public double Minimum { get; set; }

        public double Maximum { get; set; }

        public string Unit { get; set; }

        public bool ShouldSerializeTeslaloggerDbKey()
        {
            return TeslaloggerDbKey > 0;
        }
    }
}