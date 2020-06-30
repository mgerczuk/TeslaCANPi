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
using System.Threading;
using DBus;
using Mono.BlueZ.DBus;
using org.freedesktop.DBus;

namespace TeslaCAN.Elm327
{
    public class Elm327Bluetooth
    {
        private const string SppProfileUuid = "00001101-0000-1000-8000-00805F9B34FB";
        private const string Service = "org.bluez";
        private static readonly ObjectPath BlueZPath = new ObjectPath("/org/bluez");
        private static readonly ObjectPath ProfilePath = new ObjectPath("/profiles");
        private static readonly ObjectPath AgentPath = new ObjectPath("/agent");

        private readonly ManualResetEvent startEvent = new ManualResetEvent(false);
        private Exception startupException;
        private Bus system;

        public Elm327Bluetooth()
        {
            // Run a message loop for DBus on a new thread.
            var t = new Thread(DBusLoop) {IsBackground = true};
            t.Start();
            startEvent.WaitOne(60 * 1000);
            startEvent.Close();
            if (startupException != null) throw startupException;
        }

        public void Start()
        {
            var profileManager = GetObject<ProfileManager1>(Service, BlueZPath);
            system.Register(ProfilePath, new Elm327Profile());
            profileManager.RegisterProfile(ProfilePath, SppProfileUuid, new Dictionary<string, object>());

            var agentManager = GetObject<AgentManager1>(Service, BlueZPath);
            system.Register(AgentPath, new BluezAgent());
            agentManager.RegisterAgent(AgentPath, "KeyboardDisplay");
            agentManager.RequestDefaultAgent(AgentPath);

            //get a copy of the object manager so we can browse the "tree" of bluetooth items
            var manager = GetObject<ObjectManager>(Service, ObjectPath.Root);

            var managedObjects = manager.GetManagedObjects();
            //find our adapter
            ObjectPath adapterPath = null;
            foreach (var obj in managedObjects.Keys)
                if (managedObjects[obj].ContainsKey(typeof(Adapter1).DBusInterfaceName()))
                {
                    Console.WriteLine("Adapter found at" + obj);
                    adapterPath = obj;
                    break;
                }

            if (adapterPath == null) throw new Exception("Couldn't find adapter (null)");

            var adapter = GetObject<Adapter1>(Service, adapterPath);

            adapter.Discoverable = true;
            adapter.Pairable = false;
            adapter.PairableTimeout = 2 * 60;
            adapter.Pairable = true;
        }

        public void Stop()
        {
            // TODO: gracefully shutdown Bluetooth...
        }

        private T GetObject<T>(string busName, ObjectPath path)
        {
            var obj = system.GetObject<T>(busName, path);
            return obj;
        }

        private void DBusLoop()
        {
            try
            {
                system = Bus.System;
            }
            catch (Exception ex)
            {
                startupException = ex;
                return;
            }
            finally
            {
                startEvent.Set();
            }

            while (true) system.Iterate();
        }
    }
}