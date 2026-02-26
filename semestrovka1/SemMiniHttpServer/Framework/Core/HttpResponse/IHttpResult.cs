using System.Net;

namespace MiniHttpServer.Framework.Core.HttpResponse
{
    public interface IHttpResult
    {
        Task ExecuteAsync(HttpListenerContext context);
    }
}