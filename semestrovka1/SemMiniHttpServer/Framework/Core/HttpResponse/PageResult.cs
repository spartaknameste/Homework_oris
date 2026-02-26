using System.Net;
using System.Text;
using MiniTemplateEngine;

namespace MiniHttpServer.Framework.Core.HttpResponse
{
    public class PageResult : IHttpResult
    {
        private readonly string _templatePath;
        private readonly object _data;

        public PageResult(string templatePath, object data)
        {
            _templatePath = templatePath;
            _data = data;
        }

        public async Task ExecuteAsync(HttpListenerContext context)
        {
            try
            {
                var response = context.Response;
                response.ContentType = "text/html; charset=UTF-8";
                response.StatusCode = 200;

                var renderer = new HtmlTemplateRenderer();
                //Собираем полный путь к файлу
                var fullPath = Path.Combine("Public", _templatePath);

                string html;
                if (File.Exists(fullPath))
                {
                    html = renderer.RenderFromFile(fullPath, _data);
                }
                else
                {
                    html = $"<html><body><h1>Ошибка 404</h1><p>Файл {_templatePath} не найден</p></body></html>";
                    response.StatusCode = 404;
                }

                var buffer = Encoding.UTF8.GetBytes(html);
                response.ContentLength64 = buffer.Length;

                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                await response.OutputStream.FlushAsync();
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки страницы: {ex.Message}");

                var errorHtml = $"<html><body><h1>Ошибка 500</h1><p>{ex.Message}</p></body></html>";
                var buffer = Encoding.UTF8.GetBytes(errorHtml);

                context.Response.StatusCode = 500;
                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
            }
        }
    }
}
