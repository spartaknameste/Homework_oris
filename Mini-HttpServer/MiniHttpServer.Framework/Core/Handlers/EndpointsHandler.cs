using MiniHttpServer.Framework.Core.Abstracts;
using MiniHttpServer.Framework.Core.Attributes;
using MiniHttpServer.Framework.Core.HttpResponse;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MiniHttpServer.Framework.Core.Handlers
{
    internal class EndpointsHandler : Handler
    {
        public override async void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                var assembly = Assembly.GetEntryAssembly();
                var endpointTypes = assembly.GetTypes()
                    .Where(t => t.GetCustomAttribute<EndpointAttribute>() != null)
                    .ToList();

                var (endpointType, method, routeParams) = FindMatchingEndpoint(endpointTypes, request);

                if (endpointType != null && method != null)
                {
                    await ExecuteEndpointMethod(endpointType, method, context, routeParams);
                    return;
                }
            }
            catch (Exception)
            {
                await SendErrorResponse(response, 500, "Internal Server Error");
            }

            Successor?.HandleRequest(context);
        }

        private (Type, MethodInfo, Dictionary<string, string>) FindMatchingEndpoint(
            List<Type> endpointTypes, HttpListenerRequest request)
        {
            var httpMethod = request.HttpMethod;
            var path = request.Url.AbsolutePath.Trim('/');

            foreach (var endpointType in endpointTypes)
            {
                var methods = endpointType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(m => m.GetCustomAttributes()
                        .Any(attr => attr.GetType().Name.StartsWith($"Http{httpMethod}", StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                foreach (var method in methods)
                {
                    var httpAttribute = method.GetCustomAttributes()
                        .First(attr => attr.GetType().Name.StartsWith($"Http{httpMethod}", StringComparison.OrdinalIgnoreCase));

                    var routeTemplate = GetRouteTemplate(httpAttribute);

                    if (IsRouteMatch(routeTemplate, path, out var routeParams))
                    {
                        return (endpointType, method, routeParams);
                    }
                }
            }

            return (null, null, null);
        }

        private string GetRouteTemplate(object httpAttribute)
        {
            var property = httpAttribute.GetType().GetProperty("Route");
            return property?.GetValue(httpAttribute) as string;
        }

        private bool IsRouteMatch(string routeTemplate, string requestPath,
            out Dictionary<string, string> routeParams)
        {
            routeParams = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(routeTemplate))
                return string.IsNullOrEmpty(requestPath);

            var templateSegments = routeTemplate.Split('/');
            var pathSegments = requestPath.Split('/');

            if (templateSegments.Length != pathSegments.Length)
                return false;

            for (int i = 0; i < templateSegments.Length; i++)
            {
                if (templateSegments[i].StartsWith("{") && templateSegments[i].EndsWith("}"))
                {
                    var paramName = templateSegments[i].Trim('{', '}');
                    routeParams[paramName] = pathSegments[i];
                }
                else if (!templateSegments[i].Equals(pathSegments[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private async Task ExecuteEndpointMethod(Type endpointType, MethodInfo method,
            HttpListenerContext context, Dictionary<string, string> routeParams)
        {
            var instance = Activator.CreateInstance(endpointType);

            if (instance is EndpointBase endpointBase)
            {
                endpointBase.SetContext(context);
            }

            var parameters = method.GetParameters();
            var methodParams = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];

                if (routeParams.ContainsKey(param.Name))
                {
                    methodParams[i] = Convert.ChangeType(routeParams[param.Name], param.ParameterType);
                }
                else if (param.ParameterType == typeof(HttpListenerContext))
                {
                    methodParams[i] = context;
                }
                else
                {
                    methodParams[i] = param.DefaultValue ?? (param.ParameterType.IsValueType
                        ? Activator.CreateInstance(param.ParameterType)
                        : null);
                }
            }

            var result = method.Invoke(instance, methodParams);

            if (result is Task task)
            {
                await task;
                if (task.GetType().IsGenericType)
                {
                    result = task.GetType().GetProperty("Result")?.GetValue(task);
                }
                else
                {
                    result = null;
                }
            }

            await HandleResult(result, context);
        }

        private async Task HandleResult(object result, HttpListenerContext context)
        {
            if (result == null) return;

            if (result is IHttpResult httpResult)
            {
                var content = httpResult.Execute(context);
                await WriteResponseAsync(context.Response, content);
            }
            else if (result is string stringResult)
            {
                await WriteResponseAsync(context.Response, stringResult);
            }
            else
            {
                var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                context.Response.ContentType = "application/json; charset=utf-8";
                await WriteResponseAsync(context.Response, json);
            }
        }

        private static async Task WriteResponseAsync(HttpListenerResponse response, string content)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            using Stream output = response.OutputStream;
            await output.WriteAsync(buffer);
            await output.FlushAsync();
        }

        private static async Task SendErrorResponse(HttpListenerResponse response, int statusCode, string message)
        {
            response.StatusCode = statusCode;
            var errorObj = new { error = message };
            var json = JsonSerializer.Serialize(errorObj);
            response.ContentType = "application/json; charset=utf-8";
            await WriteResponseAsync(response, json);
        }
    }
}