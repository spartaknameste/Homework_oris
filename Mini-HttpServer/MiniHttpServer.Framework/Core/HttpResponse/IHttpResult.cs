using System.Net;

namespace MiniHttpServer.Framework.Core.HttpResponse
{
    public interface IHttpResult
    {
        string Execute(HttpListenerContext context);
    }
}
