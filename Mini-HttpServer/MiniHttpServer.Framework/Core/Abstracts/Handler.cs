using System.Net;

namespace MiniHttpServer.Framework.Core.Abstracts
{
    abstract class Handler
    {
        public Handler Successor { get; set; }
        public abstract void HandleRequest(HttpListenerContext context);
    }

}
