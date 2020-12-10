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

using System.IO;
using Newtonsoft.Json;

namespace TeslaCAN.CanDB
{
    internal class Model3
    {
        public Model3()
        {
            var resourceName = "TeslaCAN.CanDB.Model3CAN.json";
            var stream = typeof(Model3).Assembly.GetManifestResourceStream(resourceName);
            var serializer = new JsonSerializer();
            using (var r1 = new StreamReader(stream))
            using (var r2 = new JsonTextReader(r1))
            {
                CanDb = serializer.Deserialize<CanDB>(r2);
            }
        }

        public CanDB CanDb { get; }
    }
}