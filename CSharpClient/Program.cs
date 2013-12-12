namespace CSharpClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Linq;

    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
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