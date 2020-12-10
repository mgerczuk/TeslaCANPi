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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TeslaCAN.CanDB;
using TeslaCAN.SocketCAN;

namespace TeslaCAN.TeslaLogger
{
    internal class Sampler
    {
        private const long TicksPerSecond = 10000000L;
        internal const long TicksPer5Sec = 5L * TicksPerSecond;
        internal const long TicksPer1Min = 1L * 60 * TicksPerSecond;
        private readonly Model3 canDb;
        private readonly BlockingCollection<IList<Database.Can>> messages;
        private readonly Dictionary<int, ValueBase> values = new Dictionary<int, ValueBase>();
        private DateTime next1Min;
        private DateTime next5Sec;

        public Sampler(BlockingCollection<IList<Database.Can>> messages)
        {
            this.messages = messages;
            canDb = new Model3();
        }

        internal IDictionary<int, ValueBase> Values => values;

        public void Start(DateTime dateTime)
        {
            next5Sec = new DateTime((dateTime.Ticks / TicksPer5Sec + 1) * TicksPer5Sec);
            next1Min = new DateTime((dateTime.Ticks / TicksPer1Min + 1) * TicksPer1Min);
        }

        internal IList<Signal> FindSignals(Frame f)
        {
            if (!canDb.CanDb.PdusDict.TryGetValue(f.Id, out var msg)) return new List<Signal>();

            var signals = msg.SignalOrMuxes.Select(sm => sm.Signal)
                .Where(s => s != null)
                .ToList();
            var mux = msg.SignalOrMuxes
                .Select(sm => sm.Multiplex)
                .Where(m => m != null &&
                            ValueUInt.GetUIntValue(signals.First(s => s.Name == m.SignalName), f.DataUlong) ==
                            m.SignalValue)
                .SelectMany(m => m.Signals);
            signals.AddRange(mux);

            return signals;
        }

        public void SaveFrameData(Frame f, DateTime ts)
        {
            foreach (var signal in FindSignals(f).Where(s => s.TeslaloggerDbKey > 0))
            {
                if (!values.ContainsKey(signal.TeslaloggerDbKey))
                    values.Add(signal.TeslaloggerDbKey, ValueBase.Create(signal, ts));

                values[signal.TeslaloggerDbKey].AddValue(f);
            }
        }

        public void CheckExpired(DateTime ts)
        {
            var expired10s = ts >= next5Sec;
            var expired2min = ts >= next1Min;

            if (expired10s || expired2min)
            {
                var rounded = new DateTime(ts.Ticks / TicksPer5Sec * TicksPer5Sec);
                var list = GetFields(rounded, expired2min).ToList();
                if (list.Count > 0)
                    messages.Add(list);

                if (expired10s) next5Sec = new DateTime((ts.Ticks / TicksPer5Sec + 1) * TicksPer5Sec);
                if (expired2min) next1Min = new DateTime((ts.Ticks / TicksPer1Min + 1) * TicksPer1Min);

                if (expired2min && values.Any(kvp => kvp.Value.HasValue))
                {
                    Console.WriteLine("***** Unused data *****");
                    values.Clear();
                }
            }
        }

        internal ValueBase FindValue(DbId dbId)
        {
            return values.TryGetValue((int) dbId, out var val) && val.HasValue ? val : null;
        }

        private IEnumerable<Database.Can> GetFields(DateTime time, bool expired1Min)
        {
            if (expired1Min)
            {
                var cellTempMin = FindValue(DbId.CellTempMin);
                var cellTempMinNum = FindValue(DbId.CellTempMinNum);
                var cellTempMax = FindValue(DbId.CellTempMax);
                var cellTempMaxNum = FindValue(DbId.CellTempMaxNum);

                if (cellTempMin != null)
                {
                    yield return new Database.Can(time, DbId.CellTempMin, cellTempMin.Min);

                    if (cellTempMinNum != null)
                        yield return new Database.Can(time, DbId.CellTempMinNum,
                            cellTempMinNum.AtIndex(cellTempMin.MinIndex));
                }

                if (cellTempMin != null && cellTempMax != null)
                    yield return new Database.Can(time, DbId.CellTempMid, (cellTempMin.Min + cellTempMax.Max) / 2.0);
                if (cellTempMax != null)
                {
                    yield return new Database.Can(time, DbId.CellTempMax, cellTempMax.Max);

                    if (cellTempMaxNum != null)
                        yield return new Database.Can(time, DbId.CellTempMaxNum,
                            cellTempMaxNum.AtIndex(cellTempMax.MaxIndex));
                }

                cellTempMin?.Reset();
                cellTempMinNum?.Reset();
                cellTempMax?.Reset();
                cellTempMaxNum?.Reset();
            }

            {
                var voltageMin = FindValue(DbId.CellVoltMin);
                var voltageMinNum = FindValue(DbId.CellVoltMinNum);
                var voltageMax = FindValue(DbId.CellVoltMax);
                var voltageMaxNum = FindValue(DbId.CellVoltMaxNum);

                if (voltageMin != null)
                {
                    yield return new Database.Can(time, DbId.CellVoltMin, voltageMin.Min);

                    if (voltageMinNum != null)
                        yield return new Database.Can(time, DbId.CellVoltMinNum,
                            voltageMinNum.AtIndex(voltageMin.MinIndex));
                }

                if (voltageMin != null && voltageMax != null)
                    yield return new Database.Can(time, DbId.CellVoltMid, (voltageMin.Max + voltageMax.Min) / 2.0);
                if (voltageMax != null)
                {
                    yield return new Database.Can(time, DbId.CellVoltMax, voltageMax.Max);

                    if (voltageMaxNum != null)
                        yield return new Database.Can(time, DbId.CellVoltMaxNum,
                            voltageMaxNum.AtIndex(voltageMax.MaxIndex));
                }

                voltageMin?.Reset();
                voltageMinNum?.Reset();
                voltageMax?.Reset();
                voltageMaxNum?.Reset();
            }

            var v = FindValue(DbId.Odometer);
            if (v != null) yield return new Database.Can(time, DbId.Odometer, v.Last);
            v?.Reset();

            v = FindValue(DbId.BMSMaxDischarge);
            if (v != null) yield return new Database.Can(time, DbId.BMSMaxDischarge, v.Max);
            v?.Reset();

            {
                var volt = FindValue(DbId.BatteryVoltage);
                if (volt != null) yield return new Database.Can(time, DbId.BatteryVoltage, volt.Mean);

                var amp = FindValue(DbId.BatteryCurrent);

                if (amp != null && volt != null)
                    yield return new Database.Can(time, DbId.BatteryPower, amp.Mean * volt.Mean / 1000.0);

                if (amp != null)
                    yield return new Database.Can(time, DbId.BatteryCurrent, amp.Mean);

                var speed = FindValue(DbId.Speed);

                if (amp != null && volt != null && speed != null && Math.Abs(speed.Mean) > 0.1)
                    yield return new Database.Can(time, DbId.Consumption, amp.Mean * volt.Mean / speed.Mean);

                if (speed != null) yield return new Database.Can(time, DbId.Speed, speed.Mean);

                volt?.Reset();
                amp?.Reset();
                speed?.Reset();
            }

            v = FindValue(DbId.BatteryInlet);
            if (v != null) yield return new Database.Can(time, DbId.BatteryInlet, v.Mean);
            v?.Reset();

            v = FindValue(DbId.RadiatorBypass);
            if (v != null) yield return new Database.Can(time, DbId.RadiatorBypass, v.Mean);
            v?.Reset();

            v = FindValue(DbId.PTInlet);
            if (v != null) yield return new Database.Can(time, DbId.PTInlet, v.Mean);
            v?.Reset();

            v = FindValue(DbId.FTorque);
            if (v != null) yield return new Database.Can(time, DbId.FTorque, v.Mean);
            v?.Reset();

            v = FindValue(DbId.RTorque);
            if (v != null) yield return new Database.Can(time, DbId.RTorque, v.Mean);
            v?.Reset();

            v = FindValue(DbId.AcceleratorPedal);
            if (v != null) yield return new Database.Can(time, DbId.AcceleratorPedal, v.Mean);
            v?.Reset();

            v = FindValue(DbId.FPower);
            if (v != null) yield return new Database.Can(time, DbId.FPower, v.Mean);
            v?.Reset();

            v = FindValue(DbId.RPower);
            if (v != null) yield return new Database.Can(time, DbId.RPower, v.Mean);
            v?.Reset();

            v = FindValue(DbId.RStatorTemp);
            if (v != null) yield return new Database.Can(time, DbId.RStatorTemp, v.Mean);
            v?.Reset();

            v = FindValue(DbId.FStatorTemp);
            if (v != null) yield return new Database.Can(time, DbId.FStatorTemp, v.Mean);
            v?.Reset();

            v = FindValue(DbId.PowertrainFlow);
            if (v != null) yield return new Database.Can(time, DbId.PowertrainFlow, v.Mean);
            v?.Reset();

            v = FindValue(DbId.BatteryFlow);
            if (v != null) yield return new Database.Can(time, DbId.BatteryFlow, v.Mean);
            v?.Reset();

            if (expired1Min)
            {
                var v1 = FindValue(DbId.AC_ChargeTotal);
                if (v1 != null) yield return new Database.Can(time, DbId.AC_ChargeTotal, v1.Last);
                v1?.Reset();

                v1 = FindValue(DbId.DC_ChargeTotal);
                if (v1 != null) yield return new Database.Can(time, DbId.DC_ChargeTotal, v1.Last);
                v1?.Reset();

                v1 = FindValue(DbId.ChargeTotal);
                if (v1 != null) yield return new Database.Can(time, DbId.ChargeTotal, v1.Last);
                v1?.Reset();

                v1 = FindValue(DbId.RegenTotal);
                if (v1 != null) yield return new Database.Can(time, DbId.RegenTotal, v1.Last);
                v1?.Reset();

                v1 = FindValue(DbId.DischargeTotal);
                if (v1 != null) yield return new Database.Can(time, DbId.DischargeTotal, v1.Last);
                v1?.Reset();

                {
                    var nominalRemaining = FindValue(DbId.NominalRemaining);
                    var nominalFullPack = FindValue(DbId.NominalFullPack);
                    var energyBuffer = FindValue(DbId.EnergyBuffer);

                    if (nominalRemaining != null && nominalFullPack != null && energyBuffer != null)
                        yield return new Database.Can(time, DbId.SOC,
                            (nominalRemaining.Last - energyBuffer.Last) /
                            (nominalFullPack.Last - energyBuffer.Last) * 100.0);

                    if (nominalFullPack != null)
                        yield return new Database.Can(time, DbId.NominalFullPack, nominalFullPack.Last);

                    if (nominalRemaining != null)
                        yield return new Database.Can(time, DbId.NominalRemaining, nominalRemaining.Last);

                    if (energyBuffer != null) yield return new Database.Can(time, DbId.EnergyBuffer, energyBuffer.Last);

                    nominalRemaining?.Reset();
                    nominalFullPack?.Reset();
                    energyBuffer?.Reset();
                }

                v1 = FindValue(DbId.SOC_UI);
                if (v1 != null) yield return new Database.Can(time, DbId.SOC_UI, v1.Last);
                v1?.Reset();

                v1 = FindValue(DbId.SOC_Min);
                if (v1 != null) yield return new Database.Can(time, DbId.SOC_Min, v1.Min);
                v1?.Reset();

                v1 = FindValue(DbId.ExpectedRemaining);
                if (v1 != null) yield return new Database.Can(time, DbId.ExpectedRemaining, v1.Mean);
                v1?.Reset();

                v1 = FindValue(DbId.IdealRemaining);
                if (v1 != null) yield return new Database.Can(time, DbId.IdealRemaining, v1.Mean);
                v1?.Reset();

                v1 = FindValue(DbId.OutsideTempFiltered);
                if (v1 != null) yield return new Database.Can(time, DbId.OutsideTempFiltered, v1.Mean);
                v1?.Reset();

                v1 = FindValue(DbId.OutsideTemp);
                if (v1 != null) yield return new Database.Can(time, DbId.OutsideTemp, v1.Mean);
                v1?.Reset();
            }

            for (var i = DbId.CellVoltage0; i <= DbId.CellVoltage107; i++)
            {
                v = FindValue(i);
                if (v != null)
                {
                    yield return new Database.Can(time, i, v.Last);
                    v.Reset();
                }
            }
        }
    }
}