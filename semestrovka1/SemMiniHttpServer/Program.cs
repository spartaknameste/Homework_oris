using MiniHttpServer.Framework.Server;
using MiniHttpServer.Framework.Settings;
using MiniHttpServer.Models;
using Npgsql;
using System.Text.Json;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("MiniHttpServer - Tour");
        
        try
        {
            var settings = Singleton.GetInstance().Settings;
            if (settings == null)
            {
                Console.WriteLine("Ошибка: не удалось загрузить settings.json");
                return;
            }

            if (!string.IsNullOrEmpty(settings.ConnectionString))
            {
                try
                {
                    using (var connection = new NpgsqlConnection(settings.ConnectionString))
                    {
                        await connection.OpenAsync();
                        Console.WriteLine("Подключение к PostgreSQL успешно");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Предупреждение: не удалось подключиться к БД");
                    Console.WriteLine($"   {ex.Message}");
                }
            }

            if (string.IsNullOrEmpty(settings.SmtpEmail))
            {
                Console.WriteLine("Предупреждение: Email не настроен");
            }
            else
            {
                Console.WriteLine($"Email настроен: {settings.SmtpEmail}");
            }

            Console.WriteLine();
            Console.WriteLine($"Запуск сервера на {settings.Domain}:{settings.Port}");
            Console.WriteLine();

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            await Task.Run(() =>
            {
                try
                {
                    HttpServer server = new HttpServer(settings);
                    server.Start(token);

                    Console.WriteLine("Сервер запущен!");
                    Console.WriteLine($"Откройте в браузере: http://{settings.Domain}:{settings.Port}");
                    Console.WriteLine();
                    Console.WriteLine("Команды:");
                    Console.WriteLine("/stop - остановить сервер");
                    Console.WriteLine();

                    while (!token.IsCancellationRequested)
                    {
                        var input = Console.ReadLine();
                        if (input == "/stop")
                        {
                            Console.WriteLine("Остановка сервера...");
                            cts.Cancel();
                            break;
                        }
                    }

                    server.Stop();
                    Console.WriteLine("Сервер остановлен");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Критическая ошибка: {ex.Message}");
                }
            });
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Файл settings.json не найден");
        }
        catch (JsonException)
        {
            Console.WriteLine("Ошибка формата JSON в settings.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
        }
    }
}
