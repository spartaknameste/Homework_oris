using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.Attributes;
using MiniHttpServer.Framework.Core.HttpResponse;

namespace MiniHttpServer.Endpoints
{
    [Endpoint]
    internal class AuthEndpoint : EndpointBase
    {
        // Get /auth/login
        [HttpGet]
        public IHttpResult LoginPage()
        {
            var obj = new { };

            return Page("index.html", obj);
        }

        // Get /auth/json
        [HttpGet("json")]
        public IHttpResult GetJson()
        {
            var user = new { Name = "sfdsdf"};

            return Json(user);

            // ответ  '{"username":"Борис","Age":23}'
        }


        // Post /auth/
        [HttpPost]
        public void Login(/*string email, string password*/)
        {
            // Отправка на почту email указанного email и password
            // EmailService.SendEmail(email, title, message);
        }


        // Post /auth/sendEmail
        [HttpPost("sendEmail")]
        public void SendEmail(string to, string title, string message)
        {
            // Отправка на почту email указанного email и password

            
        }

    }
}
