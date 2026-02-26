using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpDelete : Attribute
    {
        public string? Route { get; }

        public HttpDelete() { }

        public HttpDelete(string? route)
        {
            Route = route;
        }
    }
}
