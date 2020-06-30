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
using System.Runtime.InteropServices;

namespace TeslaCAN.SocketCAN
{
    public class Frame
    {
        private can_frame frame;

        public Frame()
        {
            frame.data = new byte[8];
        }

        public uint Id
        {
            get => frame.can_id;
            set => frame.can_id = value;
        }

        public byte[] Data
        {
            get => frame.data;
            set
            {
                if (value.Length > 8)
                    throw new ArgumentOutOfRangeException(nameof(Data), "Array larger than 8 bytes.");
                Array.Copy(value, frame.data, value.Length);
                frame.can_dlc = (byte) value.Length;
            }
        }

        public ulong DataUlong => BitConverter.ToUInt64(Data.Concat(new byte[8]).ToArray(), 0);

        #region Overrides of Object

        public override string ToString()
        {
            return $"{Id:X3}({Data.Length})" + string.Join("", Data.Select(b => $"{b:X2}"));
        }

        #endregion

        public byte[] ToByteArray()
        {
            return frame.SerializeMessage();
        }

        public static Frame FromByteArray(byte[] data)
        {
            var frame = new Frame
            {
                frame = Convert.DeserializeMsg<can_frame>(data)
            };
            return frame;
        }

        public static Frame FromStream(Stream stream)
        {
            byte[] buf = new byte[Marshal.SizeOf(typeof(can_frame))];
            var read = stream.Read(buf, 0, buf.Length);
            if (read < buf.Length)
                throw new IOException("Error reading CAN frame.");
            return FromByteArray(buf);
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct can_frame
        {
            [FieldOffset(0)] public uint can_id;
            [FieldOffset(4)] public byte can_dlc;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] [FieldOffset(8)]
            public byte[] data;
        }
    }
}