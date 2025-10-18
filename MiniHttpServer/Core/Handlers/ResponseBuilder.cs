using System.Net;
using System.Text;
using System.Text.Json;

namespace MiniHttpServer.Core.Handlers
{
    internal class ResponseBuilder
    {
        public void SendSuccess(HttpListenerResponse response, object? data)
        {
            SendJson(response, 200, data ?? new { message = "Success" });
        }

        public void SendError(HttpListenerResponse response, Exception ex)
        {
            var statusCode = ex is JsonException ? 400 : 500;
            var errorData = new { error = GetErrorMessage(ex), details = ex.Message };
            SendJson(response, statusCode, errorData);
        }

        public void SendNotFound(HttpListenerResponse response)
        {
            SendJson(response, 404, new { error = "Not found" });
        }

        private void SendJson(HttpListenerResponse response, int statusCode, object data)
        {
            try
            {
                response.StatusCode = statusCode;
                response.ContentType = "application/json; charset=UTF-8";
                response.Headers.Add("Access-Control-Allow-Origin", "*");

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                var buffer = Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки ответа: {ex.Message}");
            }
        }

        private string GetErrorMessage(Exception ex)
        {
            return ex switch
            {
                JsonException => "Invalid JSON",
                _ => "Internal server error"
            };
        }
    }
}