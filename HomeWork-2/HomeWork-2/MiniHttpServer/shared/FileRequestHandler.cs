using System.Net;
using System.Text;

namespace MiniHttpServer.shared
{
    public class FileRequestHandler
    {
        private readonly string _publicDirectory;

        public FileRequestHandler(string publicDirectory)
        {
            _publicDirectory = Path.GetFullPath(publicDirectory);
        }

        public async Task<bool> HandleRequestAsync(string urlPath, HttpListenerResponse response, CancellationToken cancellationToken)
        {
            try
            {
                string filePath = ResolveFilePath(urlPath);

                if (filePath != null && File.Exists(filePath) && IsPathWithinPublicDirectory(filePath))
                {
                    await SendFileAsync(response, filePath, cancellationToken);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки запроса {urlPath}: {ex.Message}");
                return false;
            }
        }

        private string ResolveFilePath(string urlPath)
        {
            if (string.IsNullOrEmpty(urlPath) || urlPath == "/")
            {
                return GetDefaultFilePath();
            }

            // Декодируем URL и убираем начальный слеш
            var decodedPath = Uri.UnescapeDataString(urlPath);
            var cleanPath = decodedPath.TrimStart('/');

            // Заменяем слеши на разделители платформы
            cleanPath = cleanPath.Replace('/', Path.DirectorySeparatorChar);

            // Полный путь к файлу
            var fullPath = Path.Combine(_publicDirectory, cleanPath);

            // Если путь указывает на директорию, ищем index файлы
            if (Directory.Exists(fullPath))
            {
                return FindIndexFile(fullPath);
            }

            // Если файл существует - возвращаем его
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            // Пробуем добавить расширение .html
            if (!Path.HasExtension(fullPath))
            {
                var htmlPath = fullPath + ".html";
                if (File.Exists(htmlPath))
                {
                    return htmlPath;
                }
            }

            // Последняя попытка - найти файл с похожим именем
            return FindSimilarFile(fullPath);
        }

        private string GetDefaultFilePath()
        {
            var defaultFiles = new[]
            {
                "index.html",
                "index.htm",
                "default.html",
                "default.htm",
                "home.html"
            };

            foreach (var file in defaultFiles)
            {
                var fullPath = Path.Combine(_publicDirectory, file);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        private string FindIndexFile(string directoryPath)
        {
            var indexFiles = new[]
            {
                "index.html",
                "index.htm",
                "default.html",
                "default.htm"
            };

            foreach (var file in indexFiles)
            {
                var fullPath = Path.Combine(directoryPath, file);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        private string FindSimilarFile(string basePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(basePath);
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(basePath);

                if (Directory.Exists(directory))
                {
                    var files = Directory.GetFiles(directory, $"{fileNameWithoutExt}.*");
                    return files.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка поиска файла {basePath}: {ex.Message}");
            }

            return null;
        }

        private bool IsPathWithinPublicDirectory(string filePath)
        {
            var fullFilePath = Path.GetFullPath(filePath);
            return fullFilePath.StartsWith(_publicDirectory, StringComparison.OrdinalIgnoreCase);
        }

        private async Task SendFileAsync(HttpListenerResponse response, string filePath, CancellationToken cancellationToken)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                response.ContentType = MimeTypeRecognizer.GetMimeType(filePath);
                response.ContentLength64 = fileInfo.Length;

                await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await fileStream.CopyToAsync(response.OutputStream, 81920, cancellationToken);
                await response.OutputStream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке файла {filePath}: {ex.Message}");
                throw;
            }
        }
    }
}