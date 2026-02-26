using Microsoft.Extensions.Configuration;
using AliasGame.Client.Forms;

namespace AliasGame.Client;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) => { };
        AppDomain.CurrentDomain.UnhandledException += (s, e) => { };
        TaskScheduler.UnobservedTaskException += (s, e) => { e.SetObserved(); };

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var serverHost = configuration["Server:Host"] ?? "127.0.0.1";
        var serverPort = int.Parse(configuration["Server:Port"] ?? "7777");

        ApplicationConfiguration.Initialize();
        Application.Run(new LoginForm(serverHost, serverPort));
    }
}
