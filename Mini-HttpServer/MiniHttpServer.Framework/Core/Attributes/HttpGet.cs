namespace MiniHttpServer.Framework.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpGet : Attribute
    {
        public string? Route {  get; }
        public HttpGet()
        {
        }

        public HttpGet(string? route) { 
            Route = route;
        }
    }
}
