using MiniHttpServer.Core.Abstracts;
using System.Net;

namespace MiniHttpServer.Core.Handlers
{
    internal class EndpointDispatcher : Handler
    {
        private readonly EndpointFinder _finder;
        private readonly MethodExecutor _executor;
        private readonly ResponseBuilder _responseBuilder;

        public EndpointDispatcher()
        {
            _finder = new EndpointFinder();
            _executor = new MethodExecutor();
            _responseBuilder = new ResponseBuilder();
        }

        public override async void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                // Находим подходящий метод эндпоинта
                var endpointMethod = _finder.FindEndpointMethod(request);
                if (endpointMethod == null)
                {
                    PassToSuccessor(context);
                    return;
                }

                // Выполняем метод
                var result = await _executor.ExecuteMethod(endpointMethod, request);

                // Отправляем ответ
                _responseBuilder.SendSuccess(response, result);

                Console.WriteLine($"{request.Url.AbsolutePath} - Status: 200");
            }
            catch (Exception ex)
            {
                _responseBuilder.SendError(response, ex);
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private void PassToSuccessor(HttpListenerContext context)
        {
            Successor?.HandleRequest(context);
        }
    }
}
