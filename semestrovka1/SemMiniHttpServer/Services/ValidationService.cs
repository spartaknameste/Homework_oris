using System.Text.RegularExpressions;

namespace MiniHttpServer.Services
{
    public static class ValidationService
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled
        );

        private static readonly Regex PasswordRegex = new Regex(
            @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d@$!%*#?&]{8,}$",
            RegexOptions.Compiled
        );

        public static bool ValidateEmail(string email, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(email))
            {
                error = "Email не может быть пустым";
                return false;
            }

            if (!EmailRegex.IsMatch(email))
            {
                error = "Некорректный формат email";
                return false;
            }

            return true;
        }

        public static bool ValidatePassword(string password, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(password))
            {
                error = "Пароль не может быть пустым";
                return false;
            }

            if (!PasswordRegex.IsMatch(password))
            {
                error = "Пароль должен содержать минимум 8 символов, включая буквы и цифры";
                return false;
            }

            return true;
        }

        public static bool ValidateUsername(string username, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(username))
            {
                error = "Имя пользователя не может быть пустым";
                return false;
            }

            if (username.Length < 3)
            {
                error = "Имя пользователя должно быть не менее 3 символов";
                return false;
            }

            return true;
        }
    }
}