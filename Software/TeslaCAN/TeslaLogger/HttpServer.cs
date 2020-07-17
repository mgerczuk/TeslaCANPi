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
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;

namespace TeslaCAN.TeslaLogger
{
    public class HttpServer
    {
        private readonly Database database;
        private HttpListener httpListener;
        private readonly Thread httpThread;
        private bool stopped;

        public HttpServer(Database database)
        {
            this.database = database;
            httpThread = new Thread(HttpThreadProc) {IsBackground = true};
        }

        public void Start()
        {
            httpThread.Start();

        }

        public void Stop()
        {
            stopped = true;

            if (!httpThread.Join(500))
            {
                httpListener.Stop();
                httpThread.Join();
            }

        }

        private void HttpThreadProc()
        {
            try
            {
                while (!stopped)
                    try
                    {
                        using (httpListener = new HttpListener {Prefixes = {"http://*:8080/"}})
                        {
                            httpListener.Start();

                            while (!stopped)
                            {
                                var context = httpListener.GetContext();
                                if (context.Request.Url.AbsolutePath == "/get_scanmytesla" &&
                                    context.Request.HttpMethod == "POST")
                                {
                                    var d = database.GetRecord();
                                    var responseBody = d == null
                                        ? "not found"
                                        : $"0\r\n{DateTime.Now}\r\n{JsonConvert.SerializeObject(d)}";

                                    context.Response.ContentType = "application/json";
                                    Respond(responseBody, context);
                                }
                                else if (context.Request.Url.AbsolutePath == "/" &&
                                         context.Request.HttpMethod == "GET")
                                {
                                    var ver = Assembly.GetExecutingAssembly().GetName().Version;
                                    var result = "<!DOCTYPE html> <html> <body>" +
                                                 $"<h1>TeslaCAN V{ver.Major}.{ver.Minor}.{ver.Build}</h1> " +
                                                 "<p><a href=\"update\">Update Software</a></p>" +
                                                 "</body></html>";

                                    Respond(result, context);
                                }
                                else if (context.Request.Url.AbsolutePath == "/update" &&
                                         context.Request.HttpMethod == "GET")
                                {
                                    var result = "Update Ok";
                                    try
                                    {
                                        Update();
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                        result = e.ToString();
                                    }

                                    Respond(result, context);
                                }
                                else
                                {
                                    context.Response.StatusCode = 404;
                                }
                            }
                        }
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine(e);
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine(e);
                    }
                    catch (HttpListenerException e)
                    {
                        if (e.ErrorCode != 500)
                            throw;
                    }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void Respond(string responseBody, HttpListenerContext context)
        {
            var buffer = Encoding.UTF8.GetBytes(responseBody);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }

        private void Update()
        {
            var homePath = Environment.GetEnvironmentVariable("HOME");
            var zipPath = Path.Combine(homePath, "Release.zip");

            using (var client = new WebClient())
            {
                client.DownloadFile(
                    "https://gitlab.fritz.box/root/teslacan/raw/master/Software/TeslaCAN/Release.zip",
                    zipPath);
            }

            var installPath = Path.Combine(homePath, "TeslaCAN");
            new FastZip().ExtractZip(zipPath, installPath, "");
        }
    }
}