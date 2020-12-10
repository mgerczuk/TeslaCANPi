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
using Mono.Unix;
using Mono.Unix.Native;

namespace TeslaCAN.SocketCAN
{
    public enum CanProtocolType
    {
        Raw = 1, /* RAW sockets */
        Bcm = 2, /* Broadcast Manager */
        TP16 = 3, /* VAG Transport Protocol v1.6 */
        TP20 = 4, /* VAG Transport Protocol v2.0 */
        MCNet = 5, /* Bosch MCNet */
        IsoTP = 6, /* ISO 15765-2 Transport Protocol */
        NProto = 7
    }

    public class Socket : IDisposable
    {
        private static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        private readonly UnixStream stream;

        public Socket(UnixSocketType socketType, CanProtocolType protocolType)
        {
            var handle = Syscall.socket(UnixAddressFamily.AF_CAN, socketType, (UnixSocketProtocol) protocolType);
            UnixMarshal.ThrowExceptionForLastErrorIf(handle);
            stream = new UnixStream(handle, true);
        }

        #region IDisposable

        public void Dispose()
        {
            ((IDisposable) stream)?.Dispose();
        }

        #endregion

        public void Close()
        {
            stream?.Close();
        }

        public int IOControl(int ioControlCode, byte[] optionInValue, byte[] optionOutValue)
        {
            IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(byte)) * optionInValue.Length);
            Marshal.Copy(optionInValue, 0, p, optionInValue.Length);

            var res = sys_ioctl(stream.Handle, ioControlCode, p);
            UnixMarshal.ThrowExceptionForLastErrorIf(res);

            Marshal.Copy(p, optionOutValue, 0, optionInValue.Length);
            Marshal.FreeHGlobal(p);
            return res;
        }

        public int GetIfIndex(string name)
        {
            var s = new ifreq {ifrn_name = name};
            var outValue = new byte[Marshal.SizeOf(typeof(ifreq))];

            var res = IOControl(SIOCGIFINDEX, s.SerializeMessage(), outValue);

            return Convert.DeserializeMsg<ifreq>(outValue).ifr_ifindex;
        }

        public DateTime GetTimeStamp()
        {
            if (Environment.Is64BitOperatingSystem) throw new NotSupportedException(); // test...

            timeval_32 tv = new timeval_32();
            var res = sys_ioctl(stream.Handle, SIOCGSTAMP, ref tv);
            UnixMarshal.ThrowExceptionForLastErrorIf(res);

            return epoch.AddTicks(10000000L * tv.tv_sec + 10L * tv.tv_usec).ToLocalTime();
        }

        public void Bind(CanEndPoint localEP)
        {
            sockaddr_can addr = new sockaddr_can
            {
                can_family = (ushort) localEP.Family,
                can_ifindex = localEP.IfIndex
            };

            var addrLen = Marshal.SizeOf(typeof(sockaddr_can));
            var res = sys_bind(stream.Handle, ref addr, addrLen);
            UnixMarshal.ThrowExceptionForLastErrorIf(res);
        }

        public Frame ReadFrame()
        {
            return Frame.FromStream(stream);
        }

        #region IOControl native

        // Socket-level I/O control calls.
        private const int SIOCGSTAMP = 0x8906;

        // Socket configuration controls.
        private const int SIOCGIFINDEX = 0x8933;

        [StructLayout(LayoutKind.Explicit)]
        public struct ifreq
        {
            [FieldOffset(0)] [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string ifrn_name;

            [FieldOffset(16)] public int ifr_ifindex;
            //[FieldOffset(16)] public int ifru_mtu;
        }

        [DllImport("libc", SetLastError = true, EntryPoint = "ioctl")]
        private static extern int sys_ioctl(int fd, int request, IntPtr data);

        [StructLayout(LayoutKind.Sequential)]
        private struct timeval_32
        {
            public readonly uint tv_sec;
            public readonly uint tv_usec;
        }

        [DllImport("libc", SetLastError = true, EntryPoint = "ioctl")]
        private static extern int sys_ioctl(int fd, int request, ref timeval_32 tv);

        #endregion

        #region Bind native

        [StructLayout(LayoutKind.Sequential)]
        private struct sockaddr_can
        {
            public ushort can_family;
            public int can_ifindex;
            public readonly uint rx_id;
            public readonly uint tx_id;
        }

        [DllImport("libc", SetLastError = true, EntryPoint = "bind")]
        private static extern int sys_bind(int fd, ref sockaddr_can addr, int addrlen);

        #endregion
    }
}