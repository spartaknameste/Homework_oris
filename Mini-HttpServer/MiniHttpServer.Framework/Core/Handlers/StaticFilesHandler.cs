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

            if (isGetMethod && (absolutePath == "/" || absolutePath == ""))
            {
                var response = context.Response;
                byte[]? buffer = GetResponseBytes.Invoke("index.html");

                response.ContentType = "text/html; charset=utf-8";

                if (buffer == null)
                {
                    response.StatusCode = 404;
                    buffer = Encoding.UTF8.GetBytes("<html><body>404 - Not Found</body></html>");
                }

                response.ContentLength64 = buffer.Length;
                using Stream output = response.OutputStream;
                await output.WriteAsync(buffer, 0, buffer.Length);
                await output.FlushAsync();
                return;
            }

            var isStaticFile = absolutePath.Split('/').Any(x => x.Contains("."));

            if (isGetMethod && isStaticFile)
            {
                var response = context.Response;

                byte[]? buffer = null;

                string path = request.Url.AbsolutePath.Trim('/');

                buffer = GetResponseBytes.Invoke(path);

                response.ContentType = MiniHttpServer.Framework.Shared.ContentType.GetContentType(path.Trim('/'));

                if (buffer == null)
                {
                    response.StatusCode = 404;
                    string errorText = "<html><body>404 - Not Found</html></body>";
                    buffer = Encoding.UTF8.GetBytes(errorText);
                }

                response.ContentLength64 = buffer.Length;

                using Stream output = response.OutputStream;
                await output.WriteAsync(buffer, 0, buffer.Length);
                await output.FlushAsync();
            }
            else if (Successor != null)
            {
                Successor.HandleRequest(context);
            }
        }
    }
}