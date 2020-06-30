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
using System.IO;
using System.Linq;
using SQLite;

namespace TeslaCAN.TeslaLogger
{
    public class Database
    {
        private readonly object dbLock = new object();
        private SQLiteConnection db;

        public void OpenDatabase()
        {
            var path = Path.Combine(Environment.GetEnvironmentVariable("HOME"), "TeslaCAN");
            Directory.CreateDirectory(path);
            lock (dbLock)
            {
                db = new SQLiteConnection(Path.Combine(path, "teslalogger.db3"));
                if (db.TableMappings.All(m => m.MappedType.Name != typeof(Can).Name)) db.CreateTable<Can>();
            }
        }

        public void CloseDatabase()
        {
            lock (dbLock)
            {
                db?.Dispose();
                db = null;
            }
        }

        public void InsertRecords(IList<Can> list)
        {
            lock (dbLock)
            {
                foreach (var can in list) InsertRecord(can);
            }
        }

        public UploadData GetRecord()
        {
            lock (dbLock)
            {
                var oldestTimestamp = db.ExecuteScalar<DateTime?>($"SELECT MIN(Timestamp) FROM {typeof(Can).Name}");
                if (oldestTimestamp == null)
                {
                    db.Execute($"DELETE FROM sqlite_sequence WHERE name = \"{typeof(Can).Name}\"");
                    return null;
                }

                var results = db.Query<Can>($"SELECT * FROM {typeof(Can).Name} WHERE Timestamp = ?", oldestTimestamp);

                var d = new UploadData {d = oldestTimestamp.Value};
                foreach (var can in results)
                {
                    d.dict[((int) can.Id).ToString()] = can.Val;

                    DeleteRecord(can);
                }

                return d;
            }
        }

        private void InsertRecord(Can can)
        {
            for (var retry = true; retry;)
                try
                {
                    db.Insert(can);
                    retry = false;
                }
                catch (SQLiteException exception)
                {
                    Console.WriteLine(exception.Message);
                    if (exception.Result != SQLite3.Result.Busy)
                        throw;
                }
        }

        private void DeleteRecord(Can can)
        {
            for (var retry = true; retry;)
                try
                {
                    db.Delete<Can>(can.Key);
                    retry = false;
                }
                catch (SQLiteException e)
                {
                    Console.WriteLine(e);
                    if (e.Result != SQLite3.Result.Busy)
                        throw;
                }
        }

        public class UploadData
        {
            public DateTime d { get; set; }
            public Dictionary<string, double> dict { get; } = new Dictionary<string, double>();
        }

        public class Can
        {
            public Can()
            {
            }

            public Can(DateTime timestamp, DbId id, double val)
            {
                Timestamp = timestamp;
                Id = id;
                Val = val;
            }

            [PrimaryKey] [AutoIncrement] public uint Key { get; set; }

            [Indexed] public DateTime Timestamp { get; set; }

            public DbId Id { get; set; }

            public double Val { get; set; }
        }
    }
}