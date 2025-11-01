using MiniHttpServer.Framework.Server;
using MiniHttpServer.Framework.Settings;
using System.Text.Json;
using Npgsql;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var settings = Singleton.GetInstance().Settings;
            if (settings == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(settings.ConnectionString))
            {
                return;
            }

            using (var connection = new NpgsqlConnection(settings.ConnectionString))
            {
                await connection.OpenAsync();

                using (var command = new NpgsqlCommand("SELECT COUNT(*) FROM users", connection))
                {
                    try
                    {
                        command.ExecuteScalar();
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
            }
        }
        catch (Exception)
        {
            return;
        }

        CancellationTokenSource cts = new CancellationTokenSource();
        CancellationToken token = cts.Token;

        await Task.Run(() =>
        {
            try
            {
                var settings = Singleton.GetInstance().Settings;

                if (settings == null)
                {
                    return;
                }

                HttpServer server = new HttpServer(settings);
                server.Start(token);

                while (!token.IsCancellationRequested)
                {
                    var input = Console.ReadLine();
                    if (input == "/stop")
                    {
                        cts.Cancel();
                        break;
                    }
                }

                server.Stop();
            }
            catch (FileNotFoundException)
            {
                return;
            }
            catch (JsonException)
            {
                return;
            }
            catch (Exception)
            {
                return;
            }
        });
    }
}