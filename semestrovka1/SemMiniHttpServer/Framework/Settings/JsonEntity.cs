namespace MiniHttpServer.Framework.Settings
{
    public class JsonEntity
    {
        public string Domain { get; set; }
        public string Port { get; set; }

        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpEmail { get; set; }
        public string SmtpPassword { get; set; }

        public string ConnectionString { get; set; }

        public JsonEntity()
        {
        }
    }
}
