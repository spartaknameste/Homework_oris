using System.Net;
using System.Text;
using WebServerCore.Utilities;

namespace WebServerCore.Core
{
    public class WebHost
    {
        private static WebHost _sharedInstance;
        private static readonly object _syncRoot = new object();

        private HttpListener _httpListener = new();
        private ServerConfig _serverConfig;
        private CancellationTokenSource _shutdownToken = new();
        private StaticFileProcessor _fileProcessor;

        private WebHost(ServerConfig config)
        {
            _serverConfig = config;
            _fileProcessor = new StaticFileProcessor(config.ContentDirectory);
        }

        public static WebHost CreateInstance(ServerConfig config)
        {
            if (_sharedInstance == null)
            {
                lock (_syncRoot)
                {
                    if (_sharedInstance == null)
                    {
                        _sharedInstance = new WebHost(config);
                    }
                }
            }
            else
            {
                _sharedInstance.RefreshConfiguration(config);
            }
            return _sharedInstance;
        }

        public static void ClearInstance()
        {
            lock (_syncRoot)
            {
                _sharedInstance = null;
            }
        }

        public void Initialize()
        {
            try
            {
                if (_httpListener.IsListening)
                {
                    _httpListener.Stop();
                }

                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://{_serverConfig.Hostname}:{_serverConfig.ListeningPort}/");
                _httpListener.Start();

                Console.WriteLine($"Веб-сервер запущен: http://{_serverConfig.Hostname}:{_serverConfig.ListeningPort}/");
                Console.WriteLine($"Директория контента: {Path.GetFullPath(_serverConfig.ContentDirectory)}");

                _fileProcessor = new StaticFileProcessor(_serverConfig.ContentDirectory);

                _ = Task.Run(async () => await HandleIncomingRequestsAsync());
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine($"Ошибка инициализации: порт {_serverConfig.ListeningPort} недоступен. {ex.Message}");
                Environment.Exit(1);
            }
        }

        public void Shutdown()
        {
            _shutdownToken.Cancel();
            _httpListener.Stop();
            Console.WriteLine("Сервер завершил работу");
        }

        private void RefreshConfiguration(ServerConfig newConfig)
        {
            _serverConfig = newConfig;
        }

        private async Task HandleIncomingRequestsAsync()
        {
            var activeTasks = new List<Task>();

            while (!_shutdownToken.Token.IsCancellationRequested)
            {
                try
                {
                    var requestContext = await _httpListener.GetContextAsync().ConfigureAwait(false);

                    var processingTask = ProcessSingleRequestAsync(requestContext);
                    activeTasks.Add(processingTask);

                    if (activeTasks.Count > 10)
                    {
                        activeTasks.RemoveAll(task => task.IsCompleted);
                    }
                }
                catch (HttpListenerException) when (_shutdownToken.Token.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (_shutdownToken.Token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка получения запроса: {ex.Message}");
                }
            }

            if (activeTasks.Count > 0)
            {
                await Task.WhenAll(activeTasks);
            }
        }

        private async Task ProcessSingleRequestAsync(HttpListenerContext ctx)
        {
            var incomingRequest = ctx.Request;
            var outgoingResponse = ctx.Response;
            int resultCode = 200;
            string httpMethod = incomingRequest.HttpMethod;
            string requestPath = incomingRequest.Url?.AbsolutePath ?? "/";

            try
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {httpMethod} {requestPath}");

                var success = await _fileProcessor.ProcessFileRequestAsync(requestPath, outgoingResponse, _shutdownToken.Token);
                resultCode = success ? 200 : 404;

                if (!success)
                {
                    await SendErrorPageAsync(outgoingResponse, "Ресурс не найден", 404);
                }
            }
            catch (OperationCanceledException) when (_shutdownToken.Token.IsCancellationRequested)
            {
                Console.WriteLine($"Запрос {httpMethod} {requestPath} прерван");
                return;
            }
            catch (Exception ex)
            {
                resultCode = 500;
                Console.WriteLine($"Сбой обработки {httpMethod} {requestPath}: {ex.Message}");
                await SendErrorPageAsync(outgoingResponse, "Внутренняя ошибка сервера", resultCode);
            }
            finally
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {httpMethod} {requestPath} -> {resultCode}");
                outgoingResponse.OutputStream?.Close();
            }
        }

        private async Task SendErrorPageAsync(HttpListenerResponse response, string description, int statusCode)
        {
            response.StatusCode = statusCode;
            string errorContent = $@"
                <!DOCTYPE html>
                <html lang='ru'>
                <head>
                    <meta charset='utf-8'>
                    <title>Ошибка {statusCode}</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; }}
                        h1 {{ color: #d9534f; }}
                    </style>
                </head>
                <body>
                    <h1>Ошибка {statusCode}</h1>
                    <p>{description}</p>
                </body>
                </html>";

            byte[] contentBytes = Encoding.UTF8.GetBytes(errorContent);
            response.ContentLength64 = contentBytes.Length;
            response.ContentType = "text/html; charset=utf-8";

            await response.OutputStream.WriteAsync(contentBytes, 0, contentBytes.Length, _shutdownToken.Token);
        }
    }
}