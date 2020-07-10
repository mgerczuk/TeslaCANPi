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
            var network = ReadDbc(@"C:\Projects\DbcParser\Model3CAN.dbc");
            var vehicleBus = network.Node.First(n => n.Name == "VehicleBus");

            var db = ReadJson(@"..\..\..\TeslaCAN\CanDB\Model3CAN.json");

            foreach (var pdu in db.Pdus)
            {
                var msg = vehicleBus.TxMessage.FirstOrDefault(m => m.ID == pdu.Id.ToString());
            }
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