namespace CSharpClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Linq;
    using WebSocket4Net;

    public static class StressTest
    {
        private static bool IsLooping = false;
        private static int Count = 0;
        private static Stopwatch watch = Stopwatch.StartNew();

        public static WebSocket4Net.WebSocket DoSock(WebSocket4Net.WebSocket sweb, int numMessages)
        {
            sweb.Open();
            sweb.Closed += (o, e) => Console.WriteLine("Closed");
            sweb.Opened += (o, e) => Console.Write(".");
            sweb.MessageReceived += (o, e) =>
            {
                if (Interlocked.Increment(ref Count) == numMessages)
                {
                    Console.WriteLine("Read {0} in {1}ms", numMessages, watch.ElapsedMilliseconds);
                }
            };
            sweb.Error += (o, e) => Console.WriteLine("Error " + e.Exception.Message);
            return sweb;
        }

        public static void Run(string[] args)
        {
            var numClients = 10000;
            var numMessages = 10000;
            var socks = new List<WebSocket4Net.WebSocket>();

            for (var cl = 0; cl < numClients; ++cl)
            {
                socks.Add(DoSock(new WebSocket4Net.WebSocket("ws://localhost:8181/"), numMessages));
            }

            Console.Clear();
            Console.WriteLine("Ready...");

            var s = Console.ReadLine();
            var socks2 = socks.Where(c => c.State == WebSocket4Net.WebSocketState.Open).ToArray();
            Console.WriteLine("{0} are connected", socks2.Length);

            while (!string.IsNullOrWhiteSpace(s))
            {
                watch = Stopwatch.StartNew();
                Count = 0;

                for (var i = 0; i < numMessages; ++i)
                {
                    socks2[i % socks2.Length].Send(s);
                }

                Console.WriteLine("Wrote {0} in {1}ms", numMessages, watch.ElapsedMilliseconds);

                s = Console.ReadLine();
            }
        }
    }
}