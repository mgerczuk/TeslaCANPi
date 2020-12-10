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
using System.Runtime.InteropServices;
using System.Text;
using Mono.Unix.Native;

namespace TeslaCAN
{
    public static class Syslog
    {
        private static Lazy<TextWriter> getOut;
        private static Lazy<TextWriter> getError;

        public static TextWriter Info => getOut.Value;

        public static TextWriter Err => getError.Value;

        public static void Init(string ident, SyslogOptions option, SyslogFacility facility)
        {
            Syscall.openlog(Marshal.StringToHGlobalAnsi(ident), option, facility);

            getOut = new Lazy<TextWriter>(() => new SyslogWriter(facility, SyslogLevel.LOG_INFO));
            getError = new Lazy<TextWriter>(() => new SyslogWriter(facility, SyslogLevel.LOG_ERR));
        }

        private class SyslogWriter : TextWriter
        {
            private readonly SyslogFacility facility;
            private readonly SyslogLevel level;
            private string line = string.Empty;

            public SyslogWriter(SyslogFacility facility, SyslogLevel level)
            {
                this.facility = facility;
                this.level = level;
            }

            #region Overrides of TextWriter

            public override Encoding Encoding { get; } = Encoding.UTF8;

            public override string NewLine
            {
                get => "\n";
                set => throw new NotSupportedException();
            }

            public override void Write(char value)
            {
                if (value == '\n')
                    WriteLine();
                else
                    line += value;
            }

            public override void WriteLine()
            {
                Syscall.syslog(facility, level, line);
                line = string.Empty;
            }

            public override void WriteLine(string value)
            {
                Write(value);
                WriteLine();
            }

            #endregion
        }
    }
}