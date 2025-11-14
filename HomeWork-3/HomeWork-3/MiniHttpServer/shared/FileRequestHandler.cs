using System.Net;
using System.Text;

namespace WebServerCore.Utilities
{
    public class StaticFileProcessor
    {
        private readonly string _contentRoot;

        public StaticFileProcessor(string contentRoot)
        {
            _contentRoot = Path.GetFullPath(contentRoot);
        }

        public async Task<bool> ProcessFileRequestAsync(string route, HttpListenerResponse httpResponse, CancellationToken cancelToken)
        {
            try
            {
                string physicalPath = LocatePhysicalFile(route);

                if (physicalPath != null && File.Exists(physicalPath) && ValidatePathSecurity(physicalPath))
                {
                    await TransmitFileDataAsync(httpResponse, physicalPath, cancelToken);
                    return true;
                }

                return false;
            }
            catch (Exception error)
            {
                Console.WriteLine($"Ошибка при обработке маршрута {route}: {error.Message}");
                return false;
            }
        }

        private string LocatePhysicalFile(string webRoute)
        {
            if (string.IsNullOrEmpty(webRoute) || webRoute == "/")
            {
                return FindDefaultFile();
            }

            var processedRoute = Uri.UnescapeDataString(webRoute);
            var normalizedRoute = processedRoute.TrimStart('/');
            normalizedRoute = normalizedRoute.Replace('/', Path.DirectorySeparatorChar);

            var absolutePath = Path.Combine(_contentRoot, normalizedRoute);

            if (Directory.Exists(absolutePath))
            {
                return LocateIndexFile(absolutePath);
            }

            if (File.Exists(absolutePath))
            {
                return absolutePath;
            }

            if (!Path.HasExtension(absolutePath))
            {
                var htmlFile = absolutePath + ".html";
                if (File.Exists(htmlFile))
                {
                    return htmlFile;
                }
            }

            return FindFileByPattern(absolutePath);
        }

        private string FindDefaultFile()
        {
            var possibleDefaults = new[]
            {
                "main.html", "main.htm", "home.html",
                "index.html", "index.htm", "welcome.html"
            };

            foreach (var filename in possibleDefaults)
            {
                var fullPath = Path.Combine(_contentRoot, filename);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        private string LocateIndexFile(string folderPath)
        {
            var indexOptions = new[]
            {
                "main.html", "index.html", "index.htm",
                "default.html", "default.htm"
            };

            foreach (var file in indexOptions)
            {
                var fullPath = Path.Combine(folderPath, file);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        private string FindFileByPattern(string baseFilePath)
        {
            try
            {
                var container = Path.GetDirectoryName(baseFilePath);
                var nameOnly = Path.GetFileNameWithoutExtension(baseFilePath);

                if (Directory.Exists(container))
                {
                    var matchingFiles = Directory.GetFiles(container, $"{nameOnly}.*");
                    return matchingFiles.FirstOrDefault();
                }
            }
            catch (Exception error)
            {
                Console.WriteLine($"Ошибка поиска файла {baseFilePath}: {error.Message}");
            }

            return null;
        }

        private bool ValidatePathSecurity(string filePath)
        {
            var normalizedPath = Path.GetFullPath(filePath);
            return normalizedPath.StartsWith(_contentRoot, StringComparison.OrdinalIgnoreCase);
        }

        private async Task TransmitFileDataAsync(HttpListenerResponse response, string filePath, CancellationToken cancelToken)
        {
            try
            {
                var fileDetails = new FileInfo(filePath);
                response.ContentType = ContentTypeMapper.GetContentType(filePath);
                response.ContentLength64 = fileDetails.Length;

                await using var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await inputStream.CopyToAsync(response.OutputStream, 81920, cancelToken);
                await response.OutputStream.FlushAsync();
            }
            catch (Exception error)
            {
                Console.WriteLine($"Ошибка передачи файла {filePath}: {error.Message}");
                throw;
            }
        }
    }
}