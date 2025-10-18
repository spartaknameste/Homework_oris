using MiniHttpServer.Core.Attributes;
using System.Net;
using System.Reflection;

namespace MiniHttpServer.Core.Handlers
{
    internal class EndpointFinder
    {
        public EndpointMethod? FindEndpointMethod(HttpListenerRequest request)
        {
            var pathParts = request.Url?.AbsolutePath.Trim('/').Split('/');
            if (pathParts == null || pathParts.Length == 0) return null;

            var endpointName = pathParts[0];
            var routePart = pathParts.Length > 1 ? pathParts[1] : null;

            var endpointType = FindEndpointType(endpointName);
            if (endpointType == null) return null;

            var method = FindMethod(endpointType, request.HttpMethod, routePart);
            return method != null ? new EndpointMethod(endpointType, method) : null;
        }

        private Type? FindEndpointType(string endpointName)
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .FirstOrDefault(t => t.GetCustomAttribute<EndpointAttribute>() != null &&
                    (t.Name.Equals(endpointName, StringComparison.OrdinalIgnoreCase) ||
                     t.Name.Equals($"{endpointName}Endpoint", StringComparison.OrdinalIgnoreCase)));
        }

        private MethodInfo? FindMethod(Type endpointType, string httpMethod, string? routePart)
        {
            return endpointType.GetMethods()
                .FirstOrDefault(m => m.GetCustomAttributes()
                    .Any(attr => IsMatchingHttpMethod(attr, httpMethod, routePart)));
        }

        private bool IsMatchingHttpMethod(object attr, string httpMethod, string? routePart)
        {
            var attrType = attr.GetType();
            if (!attrType.Name.Equals($"Http{httpMethod}", StringComparison.OrdinalIgnoreCase))
                return false;

            var route = attrType.GetProperty("Route")?.GetValue(attr) as string;
            return route == routePart;
        }
    }

    internal record EndpointMethod(Type EndpointType, MethodInfo Method);
}
