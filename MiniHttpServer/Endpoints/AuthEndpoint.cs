using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Services;

namespace MiniHttpServer.Endpoints
{
    [Endpoint]
    internal class AuthEndpoint
    {
        // Post /auth/sendEmail
        [HttpPost("sendEmail")]
        public async Task<EmailResponse> SendEmail(EmailRequest request)
        {
            if (string.IsNullOrEmpty(request?.To) ||
                string.IsNullOrEmpty(request?.Title) ||
                string.IsNullOrEmpty(request?.Message))
            {
                return new EmailResponse
                {
                    Success = false,
                    Message = "Все поля обязательны"
                };
            }

            bool sent = await EmailService.SendEmail(request.To, request.Title, request.Message);

            return new EmailResponse
            {
                Success = sent,
                Message = sent ? "Email отправлен успешно" : "Ошибка отправки email"
            };
        }
    }

    public class EmailRequest
    {
        public string To { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
    }

    public class EmailResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}