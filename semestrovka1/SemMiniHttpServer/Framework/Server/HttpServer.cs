using MiniHttpServer.Framework.Core.Abstracts;
using MiniHttpServer.Framework.Core.Handlers;
using MiniHttpServer.Framework.Settings;
using System.Net;

namespace MiniHttpServer.Framework.Server
{
    public class HttpServer
    {
        private HttpListener listener;
        private JsonEntity config;
        private CancellationToken token;

        //Принимает настройки и сохраняет их в поле config
        public HttpServer(JsonEntity config)
        {
            this.config = config;
        }

        public void Start(CancellationToken token)
        {
            this.token = token;
            listener = new HttpListener();
            string url = $"http://{config.Domain}:{config.Port}/";
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine($"Сервер запущен: {url}");
            Receive();
        }

        public void Stop()
        {
            listener.Stop();
        }

        private void Receive()
        {
            listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
        }

        protected async void ListenerCallback(IAsyncResult result)
        {
            if (listener.IsListening && !token.IsCancellationRequested)
            {
                var context = listener.EndGetContext(result);
                string path = context.Request.Url.AbsolutePath;

                Console.WriteLine($"Запрос: {context.Request.HttpMethod} {path}");

                Handler endpointsHandler = new EndpointsHandler();
                Handler staticFilesHandler = new StaticFilesHandler();

                endpointsHandler.Successor = staticFilesHandler;
                endpointsHandler.HandleRequest(context);

                if (!token.IsCancellationRequested)
                    Receive();
            }
        }
    }
}
