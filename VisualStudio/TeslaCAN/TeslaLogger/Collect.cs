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
using System.Threading;
using Mono.Unix.Native;
using TeslaCAN.SocketCAN;

namespace TeslaCAN.TeslaLogger
{
    public class Collect
    {
        private readonly Thread canThread;
        private readonly Database database;
        private readonly Thread dbThread;
        private readonly BlockingCollection<IList<Database.Can>> messages;
        private readonly Sampler sampler;
        private Socket socket;
        private bool stopped;

        public Collect(Database database1)
        {
            messages = new BlockingCollection<IList<Database.Can>>();
            canThread = new Thread(CanThreadProc) {IsBackground = true};
            dbThread = new Thread(DbThreadProc) {IsBackground = true};
            database = database1;
            sampler = new Sampler(messages);
        }

        public void Start()
        {
            socket = new Socket(UnixSocketType.SOCK_RAW, CanProtocolType.Raw);
            var index = socket.GetIfIndex("can0");
            var sa = new CanEndPoint(index);
            socket.Bind(sa);

            canThread.Start();
            dbThread.Start();
        }

        public void Stop()
        {
            stopped = true;

            if (!canThread.Join(500))
            {
                // Blocked in socket read. Closing socket does not reliably work so abort the thread.
                canThread.Abort();
                messages.CompleteAdding();
                canThread.Join();
            }

            socket.Dispose();

            dbThread.Join();
        }

        private void CanThreadProc()
        {
            try
            {
                sampler.Start(DateTime.Now);

                while (!stopped)
                {
                    var f = socket.ReadFrame();
                    var ts = socket.GetTimeStamp();

                    sampler.SaveFrameData(f, ts);

                    sampler.CheckExpired(ts);
                }

                sampler.CheckExpired(DateTime.Now);
                messages.CompleteAdding();
            }
            catch (ThreadAbortException e)
            {
            }
        }

        private void DbThreadProc()
        {
            try
            {
                foreach (var list in messages.GetConsumingEnumerable()) database.InsertRecords(list);
            }
            catch (OperationCanceledException e)
            {
            }
        }
    }
}