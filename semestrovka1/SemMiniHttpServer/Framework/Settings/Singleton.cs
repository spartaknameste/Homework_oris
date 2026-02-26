using System.Text.Json;

namespace MiniHttpServer.Framework.Settings
{
    public class Singleton
    {
        private static Singleton instance;
        public JsonEntity Settings { get; private set; }

        private Singleton()
        {
            var json = File.ReadAllText("settings.json");
            Settings = JsonSerializer.Deserialize<JsonEntity>(json);

            // переопределение настроек из переменных окружения (для докера)
            var envDomain = Environment.GetEnvironmentVariable("SERVER_DOMAIN");
            if (!string.IsNullOrEmpty(envDomain))
                Settings.Domain = envDomain;

            var envPort = Environment.GetEnvironmentVariable("SERVER_PORT");
            if (!string.IsNullOrEmpty(envPort))
                Settings.Port = envPort;

            var envConn = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            if (!string.IsNullOrEmpty(envConn))
                Settings.ConnectionString = envConn;

            var envSmtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
            if (!string.IsNullOrEmpty(envSmtpHost))
                Settings.SmtpHost = envSmtpHost;

            var envSmtpPort = Environment.GetEnvironmentVariable("SMTP_PORT");
            if (!string.IsNullOrEmpty(envSmtpPort) && int.TryParse(envSmtpPort, out int smtpPort))
                Settings.SmtpPort = smtpPort;

            var envSmtpEmail = Environment.GetEnvironmentVariable("SMTP_EMAIL");
            if (!string.IsNullOrEmpty(envSmtpEmail))
                Settings.SmtpEmail = envSmtpEmail;

            var envSmtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
            if (!string.IsNullOrEmpty(envSmtpPassword))
                Settings.SmtpPassword = envSmtpPassword;
        }

        public static Singleton GetInstance()
        {
            if (instance == null)
            {
                instance = new Singleton();
            }
            return instance;
        }
    }
}