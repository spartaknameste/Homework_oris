using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpGet : Attribute
    {
        public string? Route { get; }

        public HttpGet() { }

        public HttpGet(string? route)
        {
            Route = route;
        }
    }
}