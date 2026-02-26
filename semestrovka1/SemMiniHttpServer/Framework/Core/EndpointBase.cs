using MiniHttpServer.Framework.Core.HttpResponse;
using System.Net;

namespace MiniHttpServer.Framework.Core
{
    public abstract class EndpointBase
    {
        protected HttpListenerContext Context { get; private set; }

        //Записывает context в свойство.
        internal void SetContext(HttpListenerContext context)
        {
            Context = context;
        }

        //Возвращает HTML-страницу
        protected IHttpResult Page(string pathTemplate, object data)
        {
            return new PageResult(pathTemplate, data);
        }

        //Возвращает данные в формате JSON
        protected IHttpResult Json(object data)
        {
            return new JsonResult(data);
        }

        protected bool IsAdmin()
        {
            var cookie = Context.Request.Cookies["admin_session"];
            return cookie != null && cookie.Value == "authorized";
        }

        protected IHttpResult CheckAdminAccess()
        {
            if (!IsAdmin())
            {
                return new RedirectResult("/");
            }
            return null;
        }
    }
}