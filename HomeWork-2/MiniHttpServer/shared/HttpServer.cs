using System.Net;
using System.Text;

namespace MiniHttpServer.shared
{
    public class HttpServer
    {
        private static HttpServer _instance;
        private static readonly object _lock = new object();

        private HttpListener _listener = new();
        private SettingsModel _config;
        private CancellationTokenSource _cts = new();
        private FileRequestHandler _fileHandler;

        // Приватный конструктор для Singleton
        private HttpServer(SettingsModel config)
        {
            _config = config;
            _fileHandler = new FileRequestHandler(config.PublicDirectoryPath);
        }

        // Singleton метод для получения экземпляра
        public static HttpServer GetInstance(SettingsModel config)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new HttpServer(config);
                    }
                }
            }
            else
            {
                // Если экземпляр уже существует, обновляем его конфигурацию
                _instance.UpdateConfiguration(config);
            }
            return _instance;
        }

        // Метод для сброса Singleton (для тестирования)
        public static void ResetInstance()
        {
            lock (_lock)
            {
                _instance = null;
            }
        }

        public void Start()
        {
            try
            {
                // Останавливаем предыдущий listener если он запущен
                if (_listener.IsListening)
                {
                    _listener.Stop();
                }

                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://{_config.Domain}:{_config.Port}/");
                _listener.Start();

                Console.WriteLine($"Сервер запущен на http://{_config.Domain}:{_config.Port}/");
                Console.WriteLine($"Текущий сайт: {Path.GetFullPath(_config.PublicDirectoryPath)}");

                // Пересоздаем обработчик файлов с новым путем
                _fileHandler = new FileRequestHandler(_config.PublicDirectoryPath);

                // Запускаем обработку запросов в фоновой задаче
                _ = Task.Run(async () => await ProcessRequestsAsync());
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine($"Ошибка запуска сервера: порт {_config.Port} занят. {ex.Message}");
                Environment.Exit(1);
            }
        }

        public void Stop()
        {
            _cts.Cancel();
            _listener.Stop();
            Console.WriteLine("Сервер остановил работу");
        }


        private void UpdateConfiguration(SettingsModel newConfig)
        {
            _config = newConfig;
        }

        private async Task ProcessRequestsAsync()
        {
            var requests = new List<Task>();

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync().ConfigureAwait(false);

                    // Обрабатываем запрос в отдельной задаче
                    var requestTask = ProcessRequestAsync(context);
                    requests.Add(requestTask);

                    // Очищаем завершенные задачи
                    if (requests.Count > 10)
                    {
                        requests.RemoveAll(t => t.IsCompleted);
                    }
                }
                catch (HttpListenerException) when (_cts.Token.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (_cts.Token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при получении контекста: {ex.Message}");
                }
            }

            if (requests.Count > 0)
            {
                await Task.WhenAll(requests);
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            int statusCode = 200;
            string method = request.HttpMethod;
            string url = request.Url?.AbsolutePath ?? "/";

            try
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {method} {url}");

                // Используем FileRequestHandler для обработки запроса
                var result = await _fileHandler.HandleRequestAsync(url, response, _cts.Token);
                statusCode = result ? 200 : 404;

                if (!result)
                {
                    await WriteErrorResponseAsync(response, "File not found", 404);
                }
            }
            catch (OperationCanceledException) when (_cts.Token.IsCancellationRequested)
            {
                Console.WriteLine($"Запрос {method} {url} отменен из-за остановки сервера");
                return;
            }
            catch (Exception ex)
            {
                statusCode = 500;
                Console.WriteLine($"Ошибка при обработке запроса {method} {url}: {ex.Message}");
                await WriteErrorResponseAsync(response, "Internal server error", statusCode);
            }
            finally
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {method} {url} -> {statusCode}");
                response.OutputStream?.Close();
            }
        }

        private async Task WriteErrorResponseAsync(HttpListenerResponse response, string message, int statusCode)
        {
            response.StatusCode = statusCode;
            string errorHtml = $@"
                <html>
                    <head><title>Error {statusCode}</title></head>
                    <body>
                        <h1>Error {statusCode}</h1>
                        <p>{message}</p>
                    </body>
                </html>";

            byte[] buffer = Encoding.UTF8.GetBytes(errorHtml);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "text/html; charset=utf-8";

            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length, _cts.Token);
        }
    }
}