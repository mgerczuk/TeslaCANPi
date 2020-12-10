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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using DBus;
using Mono.BlueZ.DBus;
using Mono.Unix.Native;
using TeslaCAN.SocketCAN;

namespace TeslaCAN.Elm327
{
    // Really simple ELM327 emulation. Just good enough to make ScanMyTesla work...
    public class Elm327Profile : Profile1
    {
        private readonly Dictionary<uint, DownSampler> downSampler = new Dictionary<uint, DownSampler>();

        private bool connected;

        private bool echo;

        private void Run(object obj)
        {
            var strm = (Stream) obj;
            echo = true;
            var spaces = true;
            var headers = true;
            var canAutoFormatting = true;
            var protocol = 0;
            try
            {
                while (connected)
                {
                    var cmd = ReadLine(strm);
                    cmd = Regex.Replace(cmd, @"\s+", "").ToUpper();
                    Console.WriteLine(cmd);
                    switch (GetCommand(cmd))
                    {
                        case "ATZ":
                            echo = true;
                            Write(strm, "PiCan v1.0\r");
                            break;

                        case "ATE":
                            echo = cmd.EndsWith("1");
                            Write(strm, "OK\r");
                            break;

                        case "ATS":
                            spaces = cmd.EndsWith("1");
                            Write(strm, "OK\r");
                            break;

                        case "ATH":
                            headers = cmd.EndsWith("1");
                            Write(strm, "OK\r");
                            break;

                        case "ATCAF":
                            canAutoFormatting = cmd.EndsWith("1");
                            Write(strm, "OK\r");
                            break;

                        case "ATSP":
                        case "ATSPA":
                        case "ATSPB":
                        case "ATSPC":
                            protocol = int.Parse(cmd.Substring(4), NumberStyles.HexNumber);
                            Write(strm, "OK\r");
                            break;

                        case "ATMA":
                        case "STM":
                            MonitorAll(strm);
                            break;

                        case "STFAP":
                            Write(strm, "OK\r");
                            break;

                        case "STDI":
                            Write(strm, "?\n");
                            break;

                        default:
                            Write(strm, "?\r");
                            break;
                    }

                    strm.Flush();
                    Write(strm, "\r>");
                    strm.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                connected = false;
            }
        }

        private static string GetCommand(string cmd)
        {
            var sb = new StringBuilder();
            foreach (var c in cmd)
            {
                if (!char.IsLetter(c))
                    break;
                sb.Append(c);
            }

            return sb.ToString();
        }

        private string ReadLine(Stream strm)
        {
            var sb = new StringBuilder();
            for (var s = strm.ReadByte(); s != '\r'; s = strm.ReadByte())
            {
                if (s < 0)
                    throw new IOException("Read aborted");
                if (echo)
                    strm.WriteByte((byte) s);

                sb.Append((char) s);
            }

            var cmd = sb.ToString();
            return cmd;
        }

        private static void Write(Stream strm, string text, bool echo = true)
        {
            var buf = Encoding.ASCII.GetBytes(text);
            if (echo)
                Console.Write(text);
            strm.Write(buf, 0, buf.Length);
        }

        private void MonitorAll(Stream strm)
        {
            var socket = new Socket(UnixSocketType.SOCK_RAW, CanProtocolType.Raw);
            var index = socket.GetIfIndex("can0");
            var sa = new CanEndPoint(index);
            socket.Bind(sa);

            bool running = true;
            var thread = new Thread(() =>
            {
                try
                {
                    strm.ReadByte();
                    running = false;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
            thread.Start();

            while (running)
            {
                var f = socket.ReadFrame();
                var ts = socket.GetTimeStamp();

                if (!downSampler.ContainsKey(f.Id))
                {
                    downSampler.Add(f.Id, new DownSampler());
                    Console.WriteLine($"Added {f.Id:X3}");
                }

                var ds = downSampler[f.Id];
                var send = ds.CheckTime(ts);

                if (send)
                {
                    var bytes = string.Join("", f.Data.Select(b => $"{b:X2}"));
                    Write(strm, $"{f.Id:X3}{bytes}\r", false);
                }
            }
        }

        private class DownSampler
        {
            private const int BlockTimeMillis = 100;

            private DateTime blockedUntil;

            public DownSampler()
            {
                blockedUntil = DateTime.Now.AddMinutes(-1);
            }

            public bool CheckTime(DateTime dt)
            {
                if (dt < blockedUntil)
                    return false;

                blockedUntil = dt.AddMilliseconds(BlockTimeMillis);
                return true;
            }
        }


        #region Implementation of Profile1

        public void Release()
        {
        }

        public void NewConnection(ObjectPath device, FileDescriptor fd, IDictionary<string, object> properties)
        {
            Console.WriteLine("NewConnection");

            fd.SetBlocking();
            var strm = fd.OpenAsStream(false);

            connected = true;
            new Thread(Run).Start(strm);
        }

        public void RequestDisconnection(ObjectPath device)
        {
            connected = false;
        }

        #endregion
    }
}