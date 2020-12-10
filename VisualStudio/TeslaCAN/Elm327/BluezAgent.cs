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
using DBus;
using Mono.BlueZ.DBus;

namespace TeslaCAN.Elm327
{
    public class BluezAgent : Agent1
    {
        #region Implementation of Agent1

        public void Release()
        {
            throw new NotImplementedException();
        }

        public string RequestPinCode(ObjectPath device)
        {
            throw new NotImplementedException();
        }

        public void DisplayPinCode(ObjectPath device, string pinCode)
        {
            throw new NotImplementedException();
        }

        public uint RequestPasskey(ObjectPath device)
        {
            throw new NotImplementedException();
        }

        public void DisplayPasskey(ObjectPath device, uint passkey, ushort entered)
        {
            throw new NotImplementedException();
        }

        public void RequestConfirmation(ObjectPath device, uint passkey)
        {
            Console.WriteLine($"RequestConfirmation({device}, {passkey})");
        }

        public void RequestAuthorization(ObjectPath device)
        {
            throw new NotImplementedException();
        }

        public void AuthorizeService(ObjectPath device, string uuid)
        {
        }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}