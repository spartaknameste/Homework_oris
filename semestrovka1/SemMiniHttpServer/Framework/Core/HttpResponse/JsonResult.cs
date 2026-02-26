using System.Net;
using System.Text;
using System.Text.Json;

namespace MiniHttpServer.Framework.Core.HttpResponse
{
    public class JsonResult : IHttpResult
    {
        private readonly object _data;

        public JsonResult(object data)
        {
            _data = data;
        }

        public async Task ExecuteAsync(HttpListenerContext context)
        {
            try
            {
                var response = context.Response;
                response.ContentType = "application/json; charset=UTF-8";
                response.StatusCode = 200;

                var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                var buffer = Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = buffer.Length;

                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                await response.OutputStream.FlushAsync();
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки JSON: {ex.Message}");
            }
        }
    }
}
