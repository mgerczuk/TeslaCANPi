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
using System.Threading;
using Mono.Unix;
using Mono.Unix.Native;
using TeslaCAN.Elm327;
using TeslaCAN.TeslaLogger;

namespace TeslaCAN
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            Syslog.Init("TeslaCAN", 0, SyslogFacility.LOG_DAEMON);
            Syslog.Info.WriteLine("started");

            var database = new Database();
            database.OpenDatabase();

            var collector = new Collect(database);
            collector.Start();
            var http = new HttpServer(database);
            http.Start();
            var bluetooth = new Elm327Bluetooth();
            bluetooth.Start();

            var signalHandlerThread = new Thread(
                o =>
                {
                    var signal = new UnixSignal(Signum.SIGTERM);
                    signal.WaitOne();

                    collector.Stop();
                    http.Stop();
                    bluetooth.Stop();
                    database.CloseDatabase();
                });

            signalHandlerThread.Start();
            signalHandlerThread.Join();
            Syslog.Info.WriteLine("stopped");
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Syslog.Err.WriteLine($"Unhandled Exception {DateTime.Now}:");
            Syslog.Err.WriteLine(e.ExceptionObject);
        }
    }
}