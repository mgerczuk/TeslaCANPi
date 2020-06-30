// 
//  TeslaloggerOBD
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

using System.Runtime.InteropServices;
using Mono.Unix.Native;

namespace TeslaCAN.SocketCAN
{
    public class SocketAddress
    {
        private sockaddr_can sockaddr;

        public SocketAddress(UnixAddressFamily family, int ifIndex)
        {
            Family = family;
            IfIndex = ifIndex;
        }

        public UnixAddressFamily Family
        {
            get => (UnixAddressFamily) sockaddr.can_family;
            private set => sockaddr.can_family = (uint) value;
        }

        public int IfIndex
        {
            get => sockaddr.can_ifindex;
            set => sockaddr.can_ifindex = value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct sockaddr_can
        {
            public uint can_family;
            public int can_ifindex;
        }
    }
}