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
using System.Web;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;

namespace TeslaCAN.TeslaLogger
{
    public class HttpServer
    {
        private readonly Database database;
        private readonly Thread httpThread;
        private HttpListener httpListener;
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
                {
                    try
                    {
                        using (httpListener = new HttpListener {Prefixes = {"http://*:8080/"}})
                        {
                            httpListener.Start();

                            while (!stopped)
                            {
                                var context = httpListener.GetContext();
                                var ap = context.Request.Url.AbsolutePath;

                                switch (context.Request.HttpMethod)
                                {
                                    case "POST":
                                        switch (ap)
                                        {
                                            case "/get_scanmytesla":
                                                GetScanMyTesla(context);
                                                break;
                                            default:
                                                context.Response.StatusCode = 404;
                                                break;
                                        }

                                        break;

                                    case "GET":
                                        switch (ap)
                                        {
                                            case "/":
                                                GetIndex(context);
                                                break;
                                            case "/w3.css":
                                                GetStyleSheet(context);
                                                break;
                                            case "/update":
                                                GetUpdate(context);
                                                break;
                                            case "/getdata":
                                                GetData(context);
                                                break;
                                            default:
                                                context.Response.StatusCode = 404;
                                                break;
                                        }

                                        break;

                                    default:
                                        context.Response.StatusCode = 404;
                                        break;
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void GetData(HttpListenerContext context)
        {
            try
            {
                var limit = 10;

                var ap = context.Request.RawUrl;
                var inx = ap.IndexOf('?');
                if (inx >= 0)
                {
                    var param = HttpUtility.ParseQueryString(ap.Substring(inx + 1));
                    var limitParam = param.Get("limit");
                    if (limitParam != null)
                        limit = int.Parse(limitParam);
                }

                var canRecords = database.GetOldestRecords(limit);

                context.Response.ContentType = "application/json";
                Respond(JsonConvert.SerializeObject(Database.GetUploadData(canRecords)), context);

                // delete data after successful send!
                database.DeleteRecords(canRecords);
            }
            catch (IOException e)
            {
                Console.WriteLine(e);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                context.Response.StatusCode = 400;
                Respond(e.Message, context);
            }
        }

        private static void GetStyleSheet(HttpListenerContext context)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var strm = assembly.GetManifestResourceStream("TeslaCAN.www.w3.css"))
            {
                context.Response.ContentLength64 = strm.Length;
                strm.CopyTo(context.Response.OutputStream);
                context.Response.OutputStream.Close();
            }
        }

        private static void GetIndex(HttpListenerContext context)
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            var result =
                $@"<!DOCTYPE html> <html>
                <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                <link rel=""stylesheet"" href=""w3.css"">
                <body>
                <header class=""w3-container w3-blue-grey w3-center"" style=""padding:128px 16px"">
                    <h1 class=""w3-margin w3-jumbo"">TeslaCAN</h1>
                    <p class=""w3-xlarge"">Version {ver.Major}.{ver.Minor}.{ver.Build}</p>
                    <p class=""w3-button w3-black w3-padding-large w3-large w3-margin-top""><a href=""update"">Update Software</a></p>
                </header>
                </body>
                </html>";

            Respond(result, context);
        }

        private void GetUpdate(HttpListenerContext context)
        {
            var updateResult = "Update Ok";
            try
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
            catch (Exception e)
            {
                Console.WriteLine(e);
                updateResult = e.ToString();
            }

            var result =
                $@"<!DOCTYPE html> <html>
                <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                <link rel=""stylesheet"" href=""w3.css"">
                <body>
                <header class=""w3-container w3-blue-grey w3-center"" style=""padding:128px 16px"">
                    <h1 class=""w3-margin w3-jumbo"">TeslaCAN</h1>
                    <p class=""w3-xlarge"">{updateResult}</p>
                    <p class=""w3-button w3-black w3-padding-large w3-large w3-margin-top""><a href=""/"">Back</a></p>
                </header>
                </body>
                </html>";

            Respond(result, context);
        }

        private void GetScanMyTesla(HttpListenerContext context)
        {
            var d = database.GetRecord();
            var responseBody = d == null
                ? "not found"
                : $"0\r\n{DateTime.Now}\r\n{JsonConvert.SerializeObject(d)}";

            context.Response.ContentType = "application/json";
            Respond(responseBody, context);
        }

        private static void Respond(string responseBody, HttpListenerContext context)
        {
            var buffer = Encoding.UTF8.GetBytes(responseBody);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
    }
}