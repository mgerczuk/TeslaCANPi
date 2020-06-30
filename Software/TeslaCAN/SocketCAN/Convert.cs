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
using System.Runtime.InteropServices;

namespace TeslaCAN.SocketCAN
{
    public static class Convert
    {
        public static byte[] SerializeMessage<T>(this T msg) where T : struct
        {
            int length = Marshal.SizeOf(typeof(T));
            byte[] destination = new byte[length];
            IntPtr num = Marshal.AllocHGlobal(length);
            Marshal.StructureToPtr((object) msg, num, true);
            Marshal.Copy(num, destination, 0, length);
            Marshal.FreeHGlobal(num);
            return destination;
        }

        public static T DeserializeMsg<T>(byte[] data) where T : struct
        {
            int num1 = Marshal.SizeOf(typeof(T));
            IntPtr num2 = Marshal.AllocHGlobal(num1);
            Marshal.Copy(data, 0, num2, num1);
            T structure = (T) Marshal.PtrToStructure(num2, typeof(T));
            Marshal.FreeHGlobal(num2);
            return structure;
        }
    }
}