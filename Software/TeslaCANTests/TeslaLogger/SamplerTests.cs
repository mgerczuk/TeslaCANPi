// 
//  TeslaloggerOBD
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
using NUnit.Framework;
using TeslaCAN.SocketCAN;

namespace TeslaCAN.TeslaLogger.Tests
{
    [TestFixture]
    public class SamplerTests
    {
        [Test]
        public void FindSignalsTest()
        {
            var sampler = new Sampler(new BlockingCollection<IList<Database.Can>>());

            var frame = new Frame
            {
                Id = 0x332, // BattCellMinMax
                Data = new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00}
            };

            var result = sampler.FindSignals(frame);

            CollectionAssert.AreEquivalent(
                new[] {"BattCellMultiplexer", "BattCellTempMax", "BattCellTempMin"},
                result.Select(s => s.Name));

            frame = new Frame
            {
                Id = 0x332, // BattCellMinMax
                Data = new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00}
            };

            result = sampler.FindSignals(frame);

            CollectionAssert.AreEquivalent(
                new[] {"BattCellMultiplexer", "BattCellBrickVoltageMax", "BattCellBrickVoltageMin"},
                result.Select(s => s.Name));

            frame = new Frame
            {
                Id = 0x3D2, // TotalChargeDischarge
                Data = new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}
            };

            result = sampler.FindSignals(frame);

            CollectionAssert.AreEquivalent(
                new[] {"TotalDischargeKWh", "TotalChargeKWh"},
                result.Select(s => s.Name));

            frame = new Frame
            {
                Id = 0xFFF, // does not exist
                Data = new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}
            };

            result = sampler.FindSignals(frame);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void SaveFrameDataTest()
        {
            var sampler = new Sampler(new BlockingCollection<IList<Database.Can>>());

            var frame = new Frame
            {
                Id = 0x3D2, // TotalChargeDischarge
                Data = new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}
            };
            var timestamp = new DateTime(2020, 6, 5, 12, 0, 0, 0);

            sampler.SaveFrameData(frame, timestamp);

            CollectionAssert.AreEquivalent(
                new[] {"TotalDischargeKWh", "TotalChargeKWh"},
                sampler.Values.Select(s => s.Value.Signal.Name));
            CollectionAssert.AreEquivalent(
                new[] {DbId.DischargeTotal, DbId.ChargeTotal},
                sampler.Values.Select(s => (DbId) s.Value.Signal.TeslaloggerDbKey));

            var totalDischarge = (ValueUInt) sampler.Values[(int) DbId.DischargeTotal];
            var totalCharge = (ValueUInt) sampler.Values[(int) DbId.ChargeTotal];

            CollectionAssert.AreEqual(new[] {0}, totalDischarge.Values);
            CollectionAssert.AreEqual(new[] {0}, totalCharge.Values);

            frame.Data = new byte[] {0x10, 0x27, 0x00, 0x00, 0x20, 0x4E, 0x00, 0x00};
            timestamp = timestamp.AddMilliseconds(500);

            sampler.SaveFrameData(frame, timestamp);

            CollectionAssert.AreEqual(new[] {0, 10000}, totalDischarge.Values);
            CollectionAssert.AreEqual(new[] {0, 20000}, totalCharge.Values);
        }

        [Test]
        public void CheckExpiredTest()
        {
            var messages = new BlockingCollection<IList<Database.Can>>();
            var sampler = new Sampler(messages);
            var timestamp = new DateTime(2020, 6, 5, 12, 0, 0, 0);
            sampler.Start(timestamp);

            sampler.SaveFrameData(new Frame
            {
                Id = 0x3D2, // TotalChargeDischarge
                Data = new byte[] { 0x10, 0x27, 0x00, 0x00, 0x20, 0x4E, 0x00, 0x00 }
            }, timestamp.AddMilliseconds(500));
            sampler.SaveFrameData(new Frame
            {
                Id = 0x3D2, // TotalChargeDischarge
                Data = new byte[] { 0x04, 0x29, 0x00, 0x00, 0x14, 0x50, 0x00, 0x00 }
            }, timestamp.AddMilliseconds(1000));
            sampler.SaveFrameData(new Frame
            {
                Id = 0x257, // UIspeed
                Data = new byte[] { 0x00, 0x50, 0x46, 0x00, 0x02, 0x00, 0x00, 0x00 }
            }, timestamp.AddMilliseconds(500));
            sampler.SaveFrameData(new Frame
            {
                Id = 0x257, // UIspeed
                Data = new byte[] { 0x00, 0x20, 0x4E, 0x00, 0x02, 0x00, 0x00, 0x00 }
            }, timestamp.AddMilliseconds(1000));

            sampler.CheckExpired(timestamp.AddMilliseconds(1000));
            sampler.CheckExpired(timestamp.AddTicks(Sampler.TicksPer10Sec-1));

            Assert.AreEqual(0, messages.Count);

            sampler.CheckExpired(timestamp.AddTicks(Sampler.TicksPer10Sec));

            Assert.AreEqual(1, messages.Count);
            var list = messages.Take();
            Assert.AreEqual(1, list.Count);
            var can = list[0];

            Assert.AreEqual(DbId.Speed, can.Id);
            Assert.AreEqual(timestamp.AddTicks(Sampler.TicksPer10Sec), can.Timestamp);
            Assert.AreEqual(55.0, can.Val);

            sampler.CheckExpired(timestamp.AddTicks(Sampler.TicksPer2Min-1));

            Assert.AreEqual(0, messages.Count);

            sampler.CheckExpired(timestamp.AddTicks(Sampler.TicksPer2Min));

            Assert.AreEqual(1, messages.Count);
            list = messages.Take();
            Assert.AreEqual(2, list.Count);

            can = list.First(c => c.Id == DbId.ChargeTotal);
            Assert.AreEqual(DbId.ChargeTotal, can.Id);
            Assert.AreEqual(timestamp.AddTicks(Sampler.TicksPer2Min), can.Timestamp);
            Assert.AreEqual(20.5, can.Val);

            can = list.First(c => c.Id == DbId.DischargeTotal);
            Assert.AreEqual(DbId.DischargeTotal, can.Id);
            Assert.AreEqual(timestamp.AddTicks(Sampler.TicksPer2Min), can.Timestamp);
            Assert.AreEqual(10.5, can.Val);
        }
    }
}