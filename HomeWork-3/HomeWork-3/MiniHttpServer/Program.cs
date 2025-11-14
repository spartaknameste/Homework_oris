using MiniHttpServer.shared;
using System.Text.Json;

namespace MiniHttpServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var settingsContent = string.Empty;
            var cancellationSource = new CancellationTokenSource();

            try
            {
                settingsContent = File.ReadAllText("settings.json");
                Console.WriteLine("Конфигурация settings.json загружена");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Файл settings.json отсутствует");
                Environment.Exit(1);
            }

            SettingsModel configuration = null;

            try
            {
                configuration = JsonSerializer.Deserialize<SettingsModel>(settingsContent);
                Console.WriteLine($"Параметры: Домен={configuration.Domain}, Порт={configuration.Port}, Папка={configuration.PublicDirectoryPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Некорректный settings.json: " + ex.Message);
                Environment.Exit(1);
            }

            var availableSites = ScanForSites(configuration.PublicDirectoryPath);

            if (availableSites.Count == 0)
            {
                Console.WriteLine("В папке public нет веб-сайтов!");
                Environment.Exit(1);
            }

            var chosenSite = SelectWebsite(availableSites);
            var siteFullPath = chosenSite == "[корневой]" || string.IsNullOrEmpty(chosenSite)
                ? configuration.PublicDirectoryPath
                : Path.Combine(configuration.PublicDirectoryPath, chosenSite);

            Console.WriteLine($"Запускается сайт: {(string.IsNullOrEmpty(chosenSite) ? "[корневой]" : chosenSite)}");
            Console.WriteLine($"Физический путь: {siteFullPath}");

            configuration.PublicDirectoryPath = siteFullPath;

            var serverInstance = HttpServer.GetInstance(configuration);
            serverInstance.Start();

            var consoleProcessor = Task.Run(() =>
            {
                while (!cancellationSource.Token.IsCancellationRequested)
                {
                    var userInput = Console.ReadLine();
                    if (userInput?.Trim().ToLower() == "/stop")
                    {
                        Console.WriteLine("Инициирована остановка...");
                        cancellationSource.Cancel();
                        break;
                    }
                    else if (userInput?.Trim().ToLower() == "/files")
                    {
                        Console.WriteLine("Содержимое текущего сайта:");
                        var siteFiles = Directory.GetFiles(configuration.PublicDirectoryPath, "*", SearchOption.AllDirectories);
                        foreach (var file in siteFiles)
                        {
                            Console.WriteLine($"  {file.Replace(configuration.PublicDirectoryPath, "").TrimStart(Path.DirectorySeparatorChar)}");
                        }
                    }
                    else if (userInput?.Trim().ToLower() == "/sites")
                    {
                        Console.WriteLine("Доступные веб-сайты:");
                        var sites = ScanForSites(Path.GetDirectoryName(configuration.PublicDirectoryPath));
                        foreach (var site in sites)
                        {
                            var currentMarker = site == chosenSite ? " (активен)" : "";
                            Console.WriteLine($"  {site}{currentMarker}");
                        }
                    }
                    else if (userInput?.Trim().ToLower() == "/switch")
                    {
                        Console.WriteLine("Смена активного сайта...");
                        var sites = ScanForSites(Path.GetDirectoryName(configuration.PublicDirectoryPath));
                        var newSite = SelectWebsite(sites);
                        var newSitePath = newSite == "[корневой]" || string.IsNullOrEmpty(newSite)
                            ? Path.GetDirectoryName(configuration.PublicDirectoryPath)
                            : Path.Combine(Path.GetDirectoryName(configuration.PublicDirectoryPath), newSite);

                        if (newSitePath != configuration.PublicDirectoryPath)
                        {
                            Console.WriteLine($"Активируется сайт: {(string.IsNullOrEmpty(newSite) ? "[корневой]" : newSite)}");
                            serverInstance.Stop();
                            configuration.PublicDirectoryPath = newSitePath;
                            serverInstance = HttpServer.GetInstance(configuration);
                            serverInstance.Start();
                            chosenSite = newSite;
                        }
                    }
                    else if (!string.IsNullOrEmpty(userInput))
                    {
                        Console.WriteLine("Доступные команды:");
                        Console.WriteLine("  /stop - остановка сервера");
                        Console.WriteLine("  /files - файлы сайта");
                        Console.WriteLine("  /sites - список сайтов");
                        Console.WriteLine("  /switch - сменить сайт");
                    }
                }
            });

            try
            {
                await Task.Delay(Timeout.Infinite, cancellationSource.Token);
            }
            catch (TaskCanceledException)
            {
            }

            serverInstance.Stop();
        }

        private static List<string> ScanForSites(string publicDir)
        {
            var sitesFound = new List<string>();

            if (!Directory.Exists(publicDir))
            {
                Console.WriteLine($"Создаем публичную папку: {publicDir}");
                Directory.CreateDirectory(publicDir);
                return sitesFound;
            }

            var directories = Directory.GetDirectories(publicDir)
                .Where(dir =>
                    !new DirectoryInfo(dir).Attributes.HasFlag(FileAttributes.Hidden) &&
                    !Path.GetFileName(dir).StartsWith("."))
                .ToList();

            foreach (var directory in directories)
            {
                var dirName = Path.GetFileName(directory);
                if (ContainsIndexFiles(directory))
                {
                    sitesFound.Add(dirName);
                }
            }

            if (ContainsIndexFiles(publicDir))
            {
                sitesFound.Add("[корневой]");
            }

            return sitesFound;
        }

        private static bool ContainsIndexFiles(string directoryPath)
        {
            var indexFileNames = new[]
            {
                "index.html", "index.htm",
                "default.html", "default.htm"
            };

            return indexFileNames.Any(file => File.Exists(Path.Combine(directoryPath, file)));
        }

        private static string SelectWebsite(List<string> websites)
        {
            if (websites.Count == 0)
            {
                Console.WriteLine("В папке public нет сайтов!");
                Environment.Exit(1);
            }

            if (websites.Count == 1)
            {
                var singleWebsite = websites[0];
                Console.WriteLine($"Автовыбор: {singleWebsite}");
                return singleWebsite == "[корневой]" ? "" : singleWebsite;
            }

            Console.WriteLine("\nОбнаруженные сайты:");
            for (int i = 0; i < websites.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {websites[i]}");
            }

            while (true)
            {
                Console.Write("\nУкажите сайт (номер или имя): ");
                var input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                if (int.TryParse(input, out int choice) && choice >= 1 && choice <= websites.Count)
                {
                    var selected = websites[choice - 1];
                    return selected == "[корневой]" ? "" : selected;
                }

                var matched = websites.FirstOrDefault(site =>
                    site.Equals(input, StringComparison.OrdinalIgnoreCase));

                if (matched != null)
                {
                    return matched == "[корневой]" ? "" : matched;
                }

                Console.WriteLine("Некорректный ввод. Попробуйте снова.");
            }
        }
    }
}