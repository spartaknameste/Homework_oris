namespace MiniHttpServer.Framework.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpPut : Attribute
    {
        public string? Route { get; }

        public HttpPut()
        {
        }

        public HttpPut(string? route)
        {
            Route = route;
        }
    }
}