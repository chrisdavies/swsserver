using Alchemy;
using Alchemy.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp
{
    public static class WebSocketTest
    {
        public static void Run()
        {
            var aServer = new WebSocketServer(81, IPAddress.Any)
            {
                OnReceive = OnReceive,
                OnSend = OnSend,
                OnConnect = OnConnect,
                OnConnected = OnConnected,
                OnDisconnect = OnDisconnect,
                TimeOut = new TimeSpan(0, 5, 0)
            };

            aServer.Start();
        }

        private static void OnDisconnect(UserContext context)
        {
            Console.WriteLine("OnDisconnect: " + context.ClientAddress);
        }

        private static void OnConnect(UserContext context)
        {
            Console.WriteLine("OnConnect: " + context.ClientAddress);
        }

        private static void OnSend(UserContext context)
        {
            Console.WriteLine("OnSend: " + context.ClientAddress);
        }

        private static void OnReceive(UserContext context)
        {
            Console.WriteLine("OnReceive: " + context.ClientAddress);
        }

        static void OnConnected(UserContext context)
        {
            Console.WriteLine("OnConnected : " + context.ClientAddress.ToString());
        }
    }
}
