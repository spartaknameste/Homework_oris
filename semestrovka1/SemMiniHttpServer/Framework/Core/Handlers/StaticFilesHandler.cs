using MiniHttpServer.Framework.Core.Abstracts;
using MiniHttpServer.Framework.Shared;
using System.Net;
using System.Text;

namespace MiniHttpServer.Framework.Core.Handlers
{
    internal class StaticFilesHandler : Handler
    {
        public override async void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var isGetMethod = request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase);
            var absolutePath = request.Url.AbsolutePath;

            // Обработка главной страницы:
            if (isGetMethod && absolutePath == "/")
            {
                var response = context.Response;
                byte[] buffer = GetResponseBytes.Invoke("index.html");
                response.ContentType = "text/html; charset=utf-8";

                if (buffer == null)
                {
                    response.StatusCode = 404;
                    buffer = Encoding.UTF8.GetBytes("<html><body>404 - Not Found</body></html>");
                }

                response.ContentLength64 = buffer.Length;
                using (Stream output = response.OutputStream)
                {
                    await output.WriteAsync(buffer, 0, buffer.Length);
                    await output.FlushAsync();
                }
                return;
            }

            var isStaticFile = absolutePath.Split("/").Any(x => x.Contains("."));

            // Проверка на статический файл:
            if (isGetMethod && isStaticFile)
            {
                string path = request.Url.AbsolutePath.Trim('/');
                byte[] buffer = GetResponseBytes.Invoke(path);

                // Файл существует — отдаём его
                if (buffer != null)
                {
                    var response = context.Response;
                    response.ContentType = ContentType.GetContentType(path.Trim('/'));
                    response.ContentLength64 = buffer.Length;
                    using (Stream output = response.OutputStream)
                    {
                        await output.WriteAsync(buffer, 0, buffer.Length);
                        await output.FlushAsync();
                    }
                    return;
                }
            }

            // Эндпоинт не найден и файл не существует — редирект на главную
            if (Successor != null)
            {
                Successor.HandleRequest(context);
            }
            else
            {
                var response = context.Response;
                response.StatusCode = 302;
                response.RedirectLocation = "/";
                response.ContentLength64 = 0;
                response.Close();
            }
        }
    }
}