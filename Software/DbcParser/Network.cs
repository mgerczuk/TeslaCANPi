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

using System.Linq;
using Dbc;

namespace DbcParse
{
    // Classes similar to XML export of DBC

    internal class Tools
    {
        public static string Strip(string s)
        {
            return s?.Substring(1, s.Length - 2);
        }
    }

    internal class Attribute
    {
        public Attribute(DbcParser.AttributeValueContext att)
        {
            Name = Tools.Strip(att.attribName.Text);
            var numVal = att.attribVal().numVal;
            Value = numVal == null ? Tools.Strip(att.attribVal().GetText()) : numVal.GetText();
        }

        public string Name { get; }
        public string Value { get; }
    }

    internal class NetworkNodeTxMessageSignal
    {
        public NetworkNodeTxMessageSignal(DbcParser.DbcContext dbc, DbcParser.SignalContext signalContext,
            DbcParser.MsgContext msg)
        {
            Name = signalContext.name.Text;
            Bitposition = byte.Parse(signalContext.startBit.Text);
            Bitsize = byte.Parse(signalContext.length.Text);
            Byteorder = signalContext.byteOrder.Text;
            Factor = signalContext.factor.ToDouble();
            Offset = signalContext.offset?.ToDouble() ?? 0.0;
            Minimum = signalContext.min?.ToDouble();
            Maximum = signalContext.max?.ToDouble();
            Unit = signalContext.unit == null ? null : Tools.Strip(signalContext.unit.Text);
            MultiplexerSignal = msg.signal().FirstOrDefault(s => s!= signalContext && s.mpxIndicator?.Text == "M")?.name.Text;
            var value = signalContext.mpxIndicator?.Text ?? string.Empty;
            MultiplexerValue = value.StartsWith("m") ? byte.Parse(value.Substring(1)) : (byte)0;
            Valuetype = signalContext.signed.Text == "+" ? "Unsigned" : "Signed";
        }

        public string Name { get; }

        public byte Bitposition { get; }

        public byte Bitsize { get; }

        public string Byteorder { get; }

        public string Valuetype { get; }

        public double Factor { get; }

        public double Offset { get; }

        public double? Minimum { get; }

        public double? Maximum { get; }

        public string Unit { get; }

        public string MultiplexerSignal { get; }

        public byte MultiplexerValue { get; }
    }

    internal class NetworkNodeTxMessage
    {
        public NetworkNodeTxMessage(DbcParser.DbcContext dbc, DbcParser.MsgContext msg)
        {
            Name = msg.name.Text;
            ID = uint.Parse(msg.id.Text);
            DLC = byte.Parse(msg.length.Text);

            Comment = Tools.Strip(dbc.comment()
                .Where(c => c.msgComment()?.msgId.Text == ID.ToString())
                .Select(c => c.msgComment().text.Text)
                .FirstOrDefault());

            Attribute = dbc.attributeValue()
                .Where(att => att.msgId?.Text == ID.ToString() && att.signalName == null)
                .Select(att => new Attribute(att))
                .ToArray();

            Signal = msg.signal()
                .Select(s => new NetworkNodeTxMessageSignal(dbc, s, msg))
                .ToArray();
        }

        public string Name { get; }

        public uint ID { get; }

        public byte DLC { get; }

        public string Comment { get; }

        public Attribute[] Attribute { get; }

        public NetworkNodeTxMessageSignal[] Signal { get; }
    }

    internal class NetworkNode
    {
        public NetworkNode(DbcParser.DbcContext dbc, string name)
        {
            Name = name;

            Attribute = dbc.attributeValue()
                .Where(att => att.nodeName?.Text == name)
                .Select(att => new Attribute(att))
                .ToArray();

            TxMessage = dbc.msg()
                .Where(m => m.sender?.Text == name).Reverse()
                .Select(m => new NetworkNodeTxMessage(dbc, m))
                .ToArray();
        }

        public string Name { get; }

        public Attribute[] Attribute { get; }

        public NetworkNodeTxMessage[] TxMessage { get; }
    }

    internal class Network
    {
        public Network(DbcParser.DbcContext dbc)
        {
            Attribute = dbc.attributeValue()
                .Where(att =>
                    att.nodeName == null && att.msgId == null && att.signalName == null && att.envVarName == null)
                .Select(att => new Attribute(att))
                .ToArray();

            Node = dbc.nodes()._nodeList
                .Select(n => new NetworkNode(dbc, n.Text))
                .ToArray();
        }

        public Attribute[] Attribute { get; }

        public NetworkNode[] Node { get; }
    }
}