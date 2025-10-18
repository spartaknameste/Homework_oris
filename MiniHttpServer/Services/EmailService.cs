using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MiniHttpServer.Settings;

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
                    Console.WriteLine("Ошибка: SMTP настройки не найдены в settings.json");
                    return false;
                }

                Console.WriteLine($"Пытаемся отправить письмо через MailKit...");
                Console.WriteLine($"От: {settings.SmtpEmail}");
                Console.WriteLine($"Кому: {to}");
                Console.WriteLine($"Тема: {subject}");
                Console.WriteLine($"SMTP: {settings.SmtpHost}:{settings.SmtpPort}");

                // создание письма
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("MiniHttpServer", settings.SmtpEmail));
                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;

                var builder = new BodyBuilder();
                builder.HtmlBody = message;
                email.Body = builder.ToMessageBody();

                // Отправка
                using var smtp = new SmtpClient();
                smtp.Timeout = 10000;

                Console.WriteLine($"Подключаемся к {settings.SmtpHost}:{settings.SmtpPort}...");
                await smtp.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.StartTls);

                Console.WriteLine($"Подключено!");
                Console.WriteLine($"Авторизуемся как {settings.SmtpEmail}...");
                await smtp.AuthenticateAsync(settings.SmtpEmail, settings.SmtpPassword);

                Console.WriteLine($"Авторизация успешна!");
                Console.WriteLine($"Отправляем письмо...");
                await smtp.SendAsync(email);

                Console.WriteLine($"Письмо отправлено!");
                await smtp.DisconnectAsync(true);

                Console.WriteLine($"Email успешно отправлен на {to}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки email: {ex.Message}");
                Console.WriteLine($"Тип: {ex.GetType().Name}");
                if (ex.InnerException != null)
                    Console.WriteLine($"InnerException: {ex.InnerException.Message}");

                if (ex.Message.Contains("535"))
                    Console.WriteLine($"Проблема аутентификации: проверьте пароль приложения и двухфакторную аутентификацию");
                else if (ex.Message.Contains("5.7.1"))
                    Console.WriteLine($"Проблема доступа: возможно, SMTP не разрешен для аккаунта");

                return false;
            }
        }
    }
}