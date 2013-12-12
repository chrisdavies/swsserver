using System;
using System.Net;
using System.Web;

namespace SuperWebServer
{
    public class SuperServerException : Exception
    {
        public SuperServerException(string message)
            : base(message)
        {

        }

        public static void Throw(HttpStatusCode httpStatusCode, string message)
        {
            throw new HttpException((int)httpStatusCode, message);
        }
    }
}
