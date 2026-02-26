using MiniHttpServer.Framework.Core.Abstracts;
using MiniHttpServer.Framework.Core.Attributes;
using MiniHttpServer.Framework.Core.HttpResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core.Handlers
{
    internal class EndpointsHandler : Handler
    {
        public override async void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            Console.WriteLine($"EndpointsHandler проверяет: {request.Url.AbsolutePath}");

            try
            {
                var assembly = Assembly.GetEntryAssembly();
                var endpointTypes = assembly.GetTypes()
                    .Where(t => t.GetCustomAttribute<EndpointAttribute>() != null)
                    .ToList();

                Console.WriteLine($"Найдено эндпоинтов: {endpointTypes.Count}");
                foreach (var ep in endpointTypes)
                {
                    Console.WriteLine($"  - {ep.Name}");
                }

                var (endpointType, method, routeParams) = FindMatchingEndpoint(endpointTypes, request);

                if (endpointType != null && method != null)
                {
                    Console.WriteLine($"Найден эндпоинт: {endpointType.Name}.{method.Name}");
                    await ExecuteEndpointMethod(endpointType, method, context, routeParams);
                    return;
                }
                else
                {
                    Console.WriteLine($"Эндпоинт не найден для: {request.Url.AbsolutePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка EndpointsHandler: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                await SendErrorResponse(response, 500, "Internal Server Error");
            }

            Successor?.HandleRequest(context);
        }

        // сопоставление маршрутов
        private (Type, MethodInfo, Dictionary<string, string>) FindMatchingEndpoint(List<Type> endpointTypes, HttpListenerRequest request)
        {
            var httpMethod = request.HttpMethod;

            //Например /api/tour/5/ станет api/tour/5.
            var path = request.Url.AbsolutePath.Trim('/');

            foreach (var endpointType in endpointTypes)
            {
                // Через рефлексию получаем атрибут
                var endpointAttr = endpointType.GetCustomAttribute<EndpointAttribute>();

                //Достаём базовый путь из атрибута
                var basePath = endpointAttr?.Route?.Trim('/') ?? "";
                var methods = endpointType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(m => m.GetCustomAttributes()
                        .Any(attr => attr.GetType().Name.StartsWith($"Http{httpMethod}", StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                foreach (var method in methods)
                {
                    // Получаем сам атрибут (например объект [HttpGET("/api/tour/{id}")])
                    var httpAttribute = method.GetCustomAttributes()
                        .First(attr => attr.GetType().Name.StartsWith($"Http{httpMethod}", StringComparison.OrdinalIgnoreCase));

                    // Достаём шаблон маршрута из атрибута, например {id} или tour/{id}.
                    var routeTemplate = GetRouteTemplate(httpAttribute);
                    // Если basePath пустой — берём только route
                    var fullRoute = string.IsNullOrEmpty(basePath)
                        ? routeTemplate
                        : $"{basePath}/{routeTemplate}".Trim('/');

                    if (IsRouteMatch(fullRoute, path, out var routeParams))
                    {
                        return (endpointType, method, routeParams);
                    }
                }
            }

            return (null, null, null);
        }

        private string GetRouteTemplate(object httpAttribute)
        {
            // из [HttpGET("/api/tour/{id}")] достанет строку "/api/tour/{id}"
            var property = httpAttribute.GetType().GetProperty("Route");
            return property?.GetValue(httpAttribute) as string ?? string.Empty;
        }

        private bool IsRouteMatch(string routeTemplate, string requestPath, out Dictionary<string, string> routeParams)
        {
            routeParams = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(routeTemplate))
            {
                return string.IsNullOrEmpty(requestPath);
            }

            var templateSegments = routeTemplate.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var pathSegments = requestPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (templateSegments.Length != pathSegments.Length)
            {
                return false;
            }

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

        private async Task ExecuteEndpointMethod(Type endpointType, MethodInfo method, HttpListenerContext context, Dictionary<string, string> routeParams)
        {
            // Через рефлексию создаём экземпляр класса
            var instance = Activator.CreateInstance(endpointType);

            // Проверяем, наследуется ли класс от EndpointBase.
            if (instance is EndpointBase endpointBase)
            {
                endpointBase.SetContext(context);
            }

            var parameters = method.GetParameters();

            // Создаём массив, куда положим значения для каждого параметра.
            var methodParams = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];

                if (routeParams.ContainsKey(param.Name))
                {
                    //routeParams["id"] = "5" → Convert.ChangeType("5", typeof(int)) → 5
                    methodParams[i] = Convert.ChangeType(routeParams[param.Name], param.ParameterType);
                }

                //Если параметр имеет тип HttpListenerContext — передаём туда context.
                else if (param.ParameterType == typeof(HttpListenerContext))
                {
                    methodParams[i] = context;
                }
                else
                {
                    methodParams[i] = param.DefaultValue ?? (param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null);
                }
            }

            // instance.GetTour(5);
            var result = method.Invoke(instance, methodParams);

            //Если метод вернул Task (асинхронный) — ждём его завершения.
            if (result is Task task)
            {
                await task;
                //Если Task имеет результат (например Task<string>) — достаём этот результат.
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

        //определяет, как отправить результат клиенту.
        private async Task HandleResult(object result, HttpListenerContext context)
        {
            if (result == null) return;

            if (result is IHttpResult httpResult)
            {
                //позволяет эндпоинту самому контролировать формат ответа (код статуса, заголовки и т.д.).
                await httpResult.ExecuteAsync(context);
            }
            else if (result is string stringResult)
            {
                await WriteResponseAsync(context.Response, stringResult);
            }
            // если result объект
            else
            {
                var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    //отступы
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
            using (System.IO.Stream output = response.OutputStream)
            {
                //записывает байты в поток
                await output.WriteAsync(buffer);
                //гарантирует, что все данные отправлены, а не застряли в буфере
                await output.FlushAsync();
            }
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
