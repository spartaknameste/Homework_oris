using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MiniHttpServer.Framework.Settings;

namespace MiniHttpServer.Services
{
    public static class EmailService
    {
        public static async Task<bool> SendEmail(string to, string subject, string message)
        {
            try
            {
                var settings = Singleton.GetInstance().Settings;

                if (string.IsNullOrEmpty(settings.SmtpEmail) || string.IsNullOrEmpty(settings.SmtpPassword))
                {
                    Console.WriteLine("Email не настроен");
                    return false;
                }

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("TourSystem", settings.SmtpEmail));
                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;

                // Создаем HTML тело письма
                var builder = new BodyBuilder();
                builder.HtmlBody = message;
                email.Body = builder.ToMessageBody();

                // Отправляем через SMTP
                using var smtp = new SmtpClient();

                // Подключаемся к SMTP серверу с TLS
                await smtp.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.StartTls);

                // Аутентифицируемся
                await smtp.AuthenticateAsync(settings.SmtpEmail, settings.SmtpPassword);

                // Отправляем письмо
                await smtp.SendAsync(email);

                // Отключаемся
                await smtp.DisconnectAsync(true);

                Console.WriteLine($"Email отправлен → {to}");
                return true;
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не падаем
                Console.WriteLine($"Ошибка отправки email: {ex.Message}");
                return false;
            }
        }
    }
}
