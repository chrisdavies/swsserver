using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketService.Client
{
    public class WebSocketClient : IDisposable
    {
        private IConnectionProcessor processor;
        private WebSocket4Net.WebSocket sock;
        private string locker = "";
        private int retryMs = 500;
        private Action retry;
        private Timer timer;

        private bool IsDisposed { get { return this.retry == null; } }

        public WebSocketClient(
            string uri, 
            IConnectionProcessor processor, 
            WebSocketCredential credential = null)
        {
            this.processor = processor;
            this.retry = this.BeginRetry;

            var cookies = new List<KeyValuePair<string, string>>();

            if (credential != null) 
            {
                var token = credential.ToString();
                if (token == null) throw new NullReferenceException("The credential must be a non-null string.");
                cookies.Add(new KeyValuePair<string, string>(credential.Type, token));
            }

            this.sock = new WebSocket4Net.WebSocket(uri: uri, cookies: cookies);

            this.sock.Opened += (o, e) =>
            {
                this.EndRetry();
                this.processor.Opened();
            };

            this.sock.Closed += (o, e) =>
            {
                this.processor.Closed();
                Lock(() => this.retry());
            };

            this.sock.MessageReceived += (o, e) => this.processor.MessageReceived(e.Message);

            this.sock.Error += (o, e) =>
            {
                this.processor.Error(e.Exception);
                Lock(() => this.retry());
            };

            this.Open();
        }

        public void Send(string message)
        {
            Lock(() => sock.Send(message));
        }

        public void Dispose()
        {
            Lock(() => {
                this.retry = null;

                if (sock != null)
                {
                    sock.Close();
                    sock = null;
                }

                if (timer != null)
                {
                    timer.Dispose();
                    timer = null;
                }
            }, false);
        }

        private void Open()
        {
            Lock(() => sock.Open());
        }

        private void BeginRetry()
        {
            Console.WriteLine("Retrying in " + retryMs + "ms");
            Lock(() =>
            {
                if (this.timer != null) return;

                this.timer = new Timer(
                o =>
                {
                    Lock(() =>
                    {
                        this.timer.Dispose();
                        this.timer = null;
                        this.retryMs = Math.Min(15000, this.retryMs + 500);
                        this.Open();
                    });
                },
                this,
                retryMs,
                Timeout.Infinite);
            });
        }

        private void EndRetry()
        {
            this.retryMs = 500;
        }

        private void Lock(Action fn, bool isNotDisposed = true)
        {
            lock (locker)
            {
                if (isNotDisposed == !this.IsDisposed)
                {
                    fn();
                }
            }
        }
    }
}
