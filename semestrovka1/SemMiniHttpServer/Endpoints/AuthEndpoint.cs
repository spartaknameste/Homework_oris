using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.Attributes;
using MiniHttpServer.Framework.Core.HttpResponse;
using MiniHttpServer.Services;

namespace MiniHttpServer.Endpoints
{
    [Endpoint("/auth")]
    internal class AuthEndpoint : EndpointBase
    {
        private const string AdminEmail = "VDMizharev@yandex.ru";

        [HttpGet("login")]
        public IHttpResult LoginPage()
        {
            try
            {
                return Page("login.html", new { });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
                Context.Response.StatusCode = 500;
                return Json(new { error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("register")]
        public IHttpResult RegisterPage()
        {
            try
            {
                return Page("register.html", new { });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                Context.Response.StatusCode = 500;
                return Json(new { error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("login")]
        public async Task<IHttpResult> Login()
        {
            try
            {
                using var reader = new StreamReader(Context.Request.InputStream);
                var formData = await reader.ReadToEndAsync();

                var data = ParseFormData(formData);
                string email = data.GetValueOrDefault("email", "");
                string password = data.GetValueOrDefault("password", "");

                if (!ValidationService.ValidateEmail(email, out string emailError))
                {
                    Context.Response.StatusCode = 400;
                    return Json(new { success = false, error = emailError });
                }

                string subject = "Вход в систему TourSystem";
                string message = $@"
                    <h2>Это были вы?</h2>
                    <p>Мы зафиксировали вход в ваш аккаунт.</p>
                    <p><strong>Детали:</strong></p>
                    <ul>
                        <li>Email: {email}</li>
                        <li>Время: {DateTime.Now:dd.MM.yyyy HH:mm}</li>
                    </ul>
                    <p>Если это были не вы, свяжитесь с поддержкой.</p>
                ";

                await EmailService.SendEmail(email, subject, message);

                if (email.Equals(AdminEmail, StringComparison.OrdinalIgnoreCase))
                {
                    Context.Response.Headers.Add("Set-Cookie",
                        "admin_session=authorized; Path=/; HttpOnly");
                }

                return Json(new { success = true, message = "Вход выполнен успешно" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка входа: {ex.Message}");
                Context.Response.StatusCode = 500;
                return Json(new { success = false, error = "Ошибка сервера" });
            }
        }

        [HttpGet("logout")]
        public IHttpResult Logout()
        {
            Context.Response.Headers.Add("Set-Cookie",
                "admin_session=; Path=/; HttpOnly; Expires=Thu, 01 Jan 1970 00:00:00 GMT");
            return new RedirectResult("/");
        }

        [HttpPost("forgot-password")]
        public async Task<IHttpResult> ForgotPassword()
        {
            try
            {
                using var reader = new StreamReader(Context.Request.InputStream);
                var formData = await reader.ReadToEndAsync();

                var data = ParseFormData(formData);
                string email = data.GetValueOrDefault("email", "");

                if (!ValidationService.ValidateEmail(email, out string emailError))
                {
                    Context.Response.StatusCode = 400;
                    return Json(new { success = false, error = emailError });
                }


                string resetToken = Guid.NewGuid().ToString();
                string resetLink = $"http://localhost:8080/auth/reset-password?token={resetToken}";

                string subject = "Восстановление пароля - TourSystem";
                string message = $@"
            <h2>Восстановление пароля</h2>
            <p>Вы запросили восстановление пароля для аккаунта {email}.</p>
            <p>Перейдите по ссылке для сброса пароля:</p>
            <p><a href=""{resetLink}"">Восстановить пароль</a></p>
            <p>Ссылка действительна в течение 1 часа.</p>
            <p>Если вы не запрашивали восстановление пароля, проигнорируйте это письмо.</p>
        ";

                bool sent = await EmailService.SendEmail(email, subject, message);

                if (sent)
                {
                    return Json(new { success = true, message = "Инструкции отправлены на email" });
                }
                else
                {
                    Context.Response.StatusCode = 500;
                    return Json(new { success = false, error = "Ошибка отправки email" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка восстановления пароля: {ex.Message}");
                Context.Response.StatusCode = 500;
                return Json(new { success = false, error = "Ошибка сервера" });
            }
        }

        [HttpPost("register")]
        public async Task<IHttpResult> Register()
        {
            try
            {
                using var reader = new StreamReader(Context.Request.InputStream);
                var formData = await reader.ReadToEndAsync();

                var data = ParseFormData(formData);
                string username = data.GetValueOrDefault("username", "");
                string email = data.GetValueOrDefault("email", "");
                string password = data.GetValueOrDefault("password", "");

                if (!ValidationService.ValidateUsername(username, out string usernameError))
                {
                    Context.Response.StatusCode = 400;
                    return Json(new { success = false, error = usernameError });
                }

                if (!ValidationService.ValidateEmail(email, out string emailError))
                {
                    Context.Response.StatusCode = 400;
                    return Json(new { success = false, error = emailError });
                }

                if (!ValidationService.ValidatePassword(password, out string passwordError))
                {
                    Context.Response.StatusCode = 400;
                    return Json(new { success = false, error = passwordError });
                }

                string subject = "Добро пожаловать в TourSystem!";
                string message = $@"
                    <h2>Регистрация завершена!</h2>
                    <p>Здравствуйте, {username}!</p>
                    <p>Ваш аккаунт успешно создан.</p>
                    <ul>
                        <li>Имя: {username}</li>
                        <li>Email: {email}</li>
                    </ul>
                ";

                await EmailService.SendEmail(email, subject, message);

                return Json(new { success = true, message = "Регистрация успешна! Проверьте email." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка регистрации: {ex.Message}");
                Context.Response.StatusCode = 500;
                return Json(new { success = false, error = "Ошибка сервера" });
            }
        }

        private Dictionary<string, string> ParseFormData(string formData)
        {
            var result = new Dictionary<string, string>();
            var pairs = formData.Split('&');

            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    result[parts[0]] = Uri.UnescapeDataString(parts[1]);
                }
            }

            return result;
        }
    }
}
