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
