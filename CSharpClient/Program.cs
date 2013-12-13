namespace CSharpClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Linq;
    using WebSocketService.Client;

    public static class Program
    {
        private class TestProcessor : IConnectionProcessor
        {
            public void Error(Exception ex)
            {
                Console.WriteLine(ex);
            }

            public void Opened()
            {
                Console.WriteLine("Opened");
            }

            public void Closed()
            {
                Console.WriteLine("Closed");
            }

            public void MessageReceived(string message)
            {
                Console.WriteLine(message);
            }
        }

        [STAThread]
        public static void Main(string[] args)
        {
            using (var sock = new WebSocketClient("ws://localhost:8181/", new TestProcessor(), new SystemCredential("abcdefg")))
            {
                Console.WriteLine("Press 'q' to quit");

                var line = Console.ReadLine();

                while (line != "q")
                {
                    sock.Send(line);
                    line = Console.ReadLine();
                }
            }
        }

        private static void SimpleMessageTest()
        {
            var sweb = new WebSocket4Net.WebSocket("ws://localhost:8181/");

            sweb.Open();
            sweb.Closed += (o, e) => Console.WriteLine("Closed.");
            sweb.Opened += (o, e) => Console.WriteLine("Opened.");
            sweb.MessageReceived += (o, e) =>
            {
                Console.WriteLine(">>: " + e.Message);
            };
            sweb.Error += (o, e) => Console.WriteLine("ERR: " + e.Exception.Message);

            Console.WriteLine("Enter a command, or 'q' to quit.");
            var command = Console.ReadLine();

            while (command != "q")
            {
                if (command == "clear")
                {
                    Console.Clear();
                }
                else
                {
                    sweb.Send(command);
                }

                command = Console.ReadLine();
            }

            sweb.Close();
        }
    }
}