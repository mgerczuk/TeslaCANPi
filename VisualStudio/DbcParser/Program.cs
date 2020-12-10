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
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Dbc;
using Newtonsoft.Json;
using TeslaCAN.CanDB;

namespace DbcParse
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var network = ReadDbc(@"..\..\model3dbc\Model3CAN.dbc");
            var vehicleBus = network.Node.First(n => n.Name == "VehicleBus");

            var db = ReadJson(@"..\..\..\TeslaCAN\CanDB\Model3CAN.json");
            var db2 = new CanDB();

            foreach (var pdu in db.Pdus)
            {
                var msg = vehicleBus.TxMessage.First(m => m.ID == pdu.Id);
                var msg2 = new Message
                {
                    Id = msg.ID,
                    Name = GetMessageName(msg.Name, msg.ID)
                };

                var muxedSignals = msg.Signal.Where(s => !string.IsNullOrEmpty(s.MultiplexerSignal)).ToList();
                if (muxedSignals.Any())
                {
                    foreach (var g in muxedSignals.GroupBy(s => new {Signal = s.MultiplexerSignal, Value = s.MultiplexerValue}).OrderBy(g => g.Key.Value))
                    {
                        var sm = new SignalOrMux
                        {
                            Multiplex = new Multiplex
                            {
                                SignalName = GetSignalName(g.Key.Signal, msg.ID), 
                                SignalValue = g.Key.Value
                            }
                        };
                        sm.Multiplex.Signals.AddRange(g.Select(s => CreateSignal(s, msg.ID)).OrderBy(s => s.BitPos));
                        msg2.SignalOrMuxes.Add(sm);
                    }
                }
                //else
                {
                    foreach (var signal in msg.Signal.Where(s => string.IsNullOrEmpty(s.MultiplexerSignal)).OrderBy(s => s.Bitposition))
                    {
                        var sm = new SignalOrMux {Signal = CreateSignal(signal, msg.ID)};
                        msg2.SignalOrMuxes.Add(sm);
                    }
                }
                db2.Pdus.Add(msg2);
            }

            WriteJson(@"..\..\..\TeslaCAN\CanDB\Model3CAN-out.json", db2);
        }

        private static Signal CreateSignal(NetworkNodeTxMessageSignal s, uint msgId)
        {
            var name = s.Name;
            name = GetSignalName(name, msgId);

            return new Signal
            {
                Name = name,
                SignalType = (SignalType) Enum.Parse(typeof(SignalType), s.Valuetype),
                BitPos = s.Bitposition, 
                BitSize = s.Bitsize,
                Factor = s.Factor,
                Offset = s.Offset,
                Minimum = s.Minimum ?? 0.0,
                Maximum = s.Maximum ?? 0.0,
                Unit = s.Unit
            };
        }

        private static string GetSignalName(string name, uint msgId)
        {
            var id = $"{msgId:X}";

            if (name.EndsWith(id))
                name = name.Substring(0, name.Length - id.Length);
            return name;
        }

        private static string GetMessageName(string msgName, uint msgId)
        {
            var id = $"ID{msgId:X}";
            if (msgName.StartsWith(id))
                return msgName.Substring(id.Length);

            return msgName;
        }

        private static CanDB ReadJson(string fileName)
        {
            CanDB db;
            using (var stream = new FileStream(fileName, FileMode.Open))
            {
                var serializer = new JsonSerializer();
                using (var r1 = new StreamReader(stream))
                using (var r2 = new JsonTextReader(r1))
                {
                    db = serializer.Deserialize<CanDB>(r2);
                }
            }

            return db;
        }

        private static void WriteJson(string fileName, CanDB canDB)
        {
            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                var serializer = JsonSerializer.Create(new JsonSerializerSettings(){Formatting = Formatting.Indented});
                using (var r1 = new StreamWriter(stream))
                using (var r2 = new JsonTextWriter(r1))
                {
                    serializer.Serialize(r2, canDB);
                }
            }
        }

        private static Network ReadDbc(string fileName)
        {
            var lexer = new DbcLexer(new AntlrFileStream(fileName));
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new ErrorListener());

            var tokens = new CommonTokenStream(lexer);

            var parser = new DbcParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ErrorListener());

            return new Network(parser.dbc());
        }

        private class ErrorListener : IAntlrErrorListener<IToken>, IAntlrErrorListener<int>
        {
            #region Implementation of IAntlrErrorListener<in int>

            public void SyntaxError(IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine,
                string msg,
                RecognitionException e)
            {
            }

            #endregion

            #region Implementation of IAntlrErrorListener<in IToken>

            public void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
                string msg,
                RecognitionException e)
            {
            }

            #endregion
        }
    }
}