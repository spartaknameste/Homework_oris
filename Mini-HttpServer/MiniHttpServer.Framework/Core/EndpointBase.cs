using MiniHttpServer.Framework.Core.HttpResponse;
using System.Net;

namespace MiniHttpServer.Framework.Core
{
    public abstract class EndpointBase
    {
        protected HttpListenerContext Context { get; private set; }

        internal void SetContext(HttpListenerContext context)
        {
            Context = context;
        }

        protected IHttpResult Page(string pathTemplate, object data) => new PageResult(pathTemplate, data);

        protected IHttpResult Json(object data) => new JsonResult(data);
    }
}