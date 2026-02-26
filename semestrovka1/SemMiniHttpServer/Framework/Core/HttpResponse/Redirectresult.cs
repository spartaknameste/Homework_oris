using System.Net;

namespace MiniHttpServer.Framework.Core.HttpResponse
{
    public class RedirectResult : IHttpResult
    {
        private readonly string _url;

        public RedirectResult(string url)
        {
            _url = url;
        }

        public Task ExecuteAsync(HttpListenerContext context)
        {
            //временное перенаправление
            context.Response.StatusCode = 302;
            context.Response.RedirectLocation = _url;
            context.Response.ContentLength64 = 0;
            context.Response.OutputStream.Close();
            //заглушка.
            return Task.CompletedTask;
        }
    }
}