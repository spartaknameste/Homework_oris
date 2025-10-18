using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MiniHttpServer.Core.Handlers
{
    internal class MethodExecutor
    {
        public async Task<object?> ExecuteMethod(EndpointMethod endpointMethod, HttpListenerRequest request)
        {
            var args = await PrepareArguments(endpointMethod.Method, request);
            var instance = Activator.CreateInstance(endpointMethod.EndpointType);
            var result = endpointMethod.Method.Invoke(instance, args);

            return await UnwrapTaskResult(result);
        }

        private async Task<object?[]> PrepareArguments(MethodInfo method, HttpListenerRequest request)
        {
            var parameters = method.GetParameters();
            object?[] args = new object?[parameters.Length];

            if (parameters.Length > 0 && request.HttpMethod == "POST")
            {
                var requestBody = await ReadRequestBody(request);
                if (!string.IsNullOrEmpty(requestBody))
                {
                    args[0] = JsonSerializer.Deserialize(requestBody, parameters[0].ParameterType,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }

            return args;
        }

        private async Task<string> ReadRequestBody(HttpListenerRequest request)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            return await reader.ReadToEndAsync();
        }

        private async Task<object?> UnwrapTaskResult(object? result)
        {
            if (result is Task task)
            {
                await task;
                return task.GetType().GetProperty("Result")?.GetValue(task);
            }
            return result;
        }
    }
}
