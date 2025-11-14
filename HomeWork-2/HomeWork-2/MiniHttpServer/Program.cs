using MiniHttpServer.shared;
using System.Text.Json;

namespace MiniHttpServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var settingsJson = string.Empty;
            var cts = new CancellationTokenSource();

            // Чтение настроек
            try
            {
                settingsJson = File.ReadAllText("settings.json");
                Console.WriteLine("Файл settings.json успешно загружен");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Файл settings.json не существует");
                Environment.Exit(1);
            }

            SettingsModel settings = null;

            try
            {
                settings = JsonSerializer.Deserialize<SettingsModel>(settingsJson);
                Console.WriteLine($"Настройки: Domain={settings.Domain}, Port={settings.Port}, PublicDirectory={settings.PublicDirectoryPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Файл settings.json некорректен: " + ex.Message);
                Environment.Exit(1);
            }

            // Получаем список сайтов в папке public
            var availableSites = GetAvailableSites(settings.PublicDirectoryPath);

            if (availableSites.Count == 0)
            {
                Console.WriteLine("В папке public не найдено ни одного сайта!");
                Environment.Exit(1);
            }

            // Выбор сайта
            var selectedSite = SelectSite(availableSites);
            var sitePath = selectedSite == "[корневой]" || string.IsNullOrEmpty(selectedSite)
                ? settings.PublicDirectoryPath
                : Path.Combine(settings.PublicDirectoryPath, selectedSite);

            Console.WriteLine($"Выбран сайт: {(string.IsNullOrEmpty(selectedSite) ? "[корневой]" : selectedSite)}");
            Console.WriteLine($"Путь к сайту: {sitePath}");

            settings.PublicDirectoryPath = sitePath;

            var httpServer = HttpServer.GetInstance(settings);
            httpServer.Start();

            // Задача для обработки консольных команд
            var consoleTask = Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var input = Console.ReadLine();
                    if (input?.Trim().ToLower() == "/stop")
                    {
                        Console.WriteLine("Остановка сервера...");
                        cts.Cancel();
                        break;
                    }
                    else if (input?.Trim().ToLower() == "/files")
                    {
                        Console.WriteLine("Файлы в выбранном сайте:");
                        var files = Directory.GetFiles(settings.PublicDirectoryPath, "*", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            Console.WriteLine($"  {file.Replace(settings.PublicDirectoryPath, "").TrimStart(Path.DirectorySeparatorChar)}");
                        }
                    }
                    else if (input?.Trim().ToLower() == "/sites")
                    {
                        Console.WriteLine("Доступные сайты:");
                        var sites = GetAvailableSites(Path.GetDirectoryName(settings.PublicDirectoryPath));
                        foreach (var site in sites)
                        {
                            var isCurrent = site == selectedSite ? " (текущий)" : "";
                            Console.WriteLine($"  {site}{isCurrent}");
                        }
                    }
                    else if (input?.Trim().ToLower() == "/switch")
                    {
                        Console.WriteLine("Переключение на другой сайт...");
                        var sites = GetAvailableSites(Path.GetDirectoryName(settings.PublicDirectoryPath));
                        var newSite = SelectSite(sites);
                        var newSitePath = newSite == "[корневой]" || string.IsNullOrEmpty(newSite)
                            ? Path.GetDirectoryName(settings.PublicDirectoryPath)
                            : Path.Combine(Path.GetDirectoryName(settings.PublicDirectoryPath), newSite);

                        if (newSitePath != settings.PublicDirectoryPath)
                        {
                            Console.WriteLine($"Переключаемся на сайт: {(string.IsNullOrEmpty(newSite) ? "[корневой]" : newSite)}");
                            httpServer.Stop();
                            settings.PublicDirectoryPath = newSitePath;
                            httpServer = HttpServer.GetInstance(settings);
                            httpServer.Start();
                            selectedSite = newSite;
                        }
                    }
                    else if (!string.IsNullOrEmpty(input))
                    {
                        Console.WriteLine("Неизвестная команда. Используйте:");
                        Console.WriteLine("  /stop - остановка сервера");
                        Console.WriteLine("  /files - список файлов в текущем сайте");
                        Console.WriteLine("  /sites - список доступных сайтов");
                        Console.WriteLine("  /switch - переключиться на другой сайт");
                    }
                }
            });

            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (TaskCanceledException)
            {
            }

            httpServer.Stop();
        }

        private static List<string> GetAvailableSites(string publicDirectory)
        {
            var sites = new List<string>();

            if (!Directory.Exists(publicDirectory))
            {
                Console.WriteLine($"Создаем публичную директорию: {publicDirectory}");
                Directory.CreateDirectory(publicDirectory);
                return sites;
            }


            var directories = Directory.GetDirectories(publicDirectory)
                .Where(dir =>
                    !new DirectoryInfo(dir).Attributes.HasFlag(FileAttributes.Hidden) &&
                    !Path.GetFileName(dir).StartsWith("."))
                .ToList();

            foreach (var directory in directories)
            {
                var dirName = Path.GetFileName(directory);
                if (HasIndexFile(directory))
                {
                    sites.Add(dirName);
                }
            }

            if (HasIndexFile(publicDirectory))
            {
                sites.Add("public (root)");
            }

            return sites;
        }

        private static bool HasIndexFile(string directoryPath)
        {
            var indexFiles = new[]
            {
                "index.html",
                "index.htm",
                "default.html",
                "default.htm"
            };

            return indexFiles.Any(file => File.Exists(Path.Combine(directoryPath, file)));
        }

        private static string SelectSite(List<string> availableSites)
        {
            if (availableSites.Count == 0)
            {
                Console.WriteLine("Не найдено ни одного сайта в папке public!");
                Environment.Exit(1);
            }

            if (availableSites.Count == 1)
            {
                var singleSite = availableSites[0];
                Console.WriteLine($"Автоматически выбран сайт: {singleSite}");
                return singleSite == "[корневой]" ? "" : singleSite;
            }

            Console.WriteLine("\nДоступные сайты в папке public:");
            for (int i = 0; i < availableSites.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {availableSites[i]}");
            }

            while (true)
            {
                Console.Write("\nВыберите сайт для запуска (номер или название): ");
                var input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                // Если введен номер
                if (int.TryParse(input, out int number) && number >= 1 && number <= availableSites.Count)
                {
                    var selected = availableSites[number - 1];
                    return selected == "[корневой]" ? "" : selected;
                }

                // Если введено название
                var selectedSite = availableSites.FirstOrDefault(site =>
                    site.Equals(input, StringComparison.OrdinalIgnoreCase));

                if (selectedSite != null)
                {
                    return selectedSite == "[корневой]" ? "" : selectedSite;
                }

                Console.WriteLine("Неверный выбор. Попробуйте снова.");
            }
        }
    }
}