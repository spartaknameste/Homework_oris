namespace MiniHttpServer.shared
{
    public static class MimeTypeRecognizer
    {
        private static readonly Dictionary<string, string> _typeMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Текстовые документы
            { ".html", "text/html; charset=utf-8" },
            { ".htm", "text/html; charset=utf-8" },
            { ".css", "text/css" },
            { ".txt", "text/plain" },
            { ".csv", "text/csv" },
            { ".xml", "text/xml" },
            { ".md", "text/markdown" },
            
            // JavaScript и данные
            { ".js", "application/javascript" },
            { ".json", "application/json" },
            { ".pdf", "application/pdf" },
            { ".zip", "application/zip" },
            { ".tar", "application/x-tar" },
            { ".gz", "application/gzip" },
            { ".7z", "application/x-7z-compressed" },
            { ".rar", "application/vnd.rar" },
            
            // Графические форматы
            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".gif", "image/gif" },
            { ".webp", "image/webp" },
            { ".svg", "image/svg+xml" },
            { ".ico", "image/x-icon" },
            { ".tiff", "image/tiff" },
            { ".tif", "image/tiff" },
            { ".avif", "image/avif" },
            { ".heic", "image/heic" },
            { ".heif", "image/heif" },
            { ".psd", "image/vnd.adobe.photoshop" },
            { ".bmp", "image/bmp" },
            
            // Аудиофайлы
            { ".mp3", "audio/mpeg" },
            { ".wav", "audio/wav" },
            { ".ogg", "audio/ogg" },
            { ".aac", "audio/aac" },
            { ".flac", "audio/flac" },
            { ".m4a", "audio/mp4" },
            { ".weba", "audio/webm" },
            { ".mid", "audio/midi" },
            { ".midi", "audio/midi" },
            
            // Видеофайлы
            { ".mp4", "video/mp4" },
            { ".avi", "video/x-msvideo" },
            { ".mov", "video/quicktime" },
            { ".wmv", "video/x-ms-wmv" },
            { ".flv", "video/x-flv" },
            { ".webm", "video/webm" },
            { ".mkv", "video/x-matroska" },
            { ".3gp", "video/3gpp" },
            { ".mpeg", "video/mpeg" },
            { ".mpg", "video/mpeg" },
            
            // Шрифтовые файлы
            { ".woff", "font/woff" },
            { ".woff2", "font/woff2" },
            { ".ttf", "font/ttf" },
            { ".otf", "font/otf" },
            { ".eot", "application/vnd.ms-fontobject" },
            
            // Офисные документы
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".ppt", "application/vnd.ms-powerpoint" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { ".odt", "application/vnd.oasis.opendocument.text" },
            { ".ods", "application/vnd.oasis.opendocument.spreadsheet" },
            { ".odp", "application/vnd.oasis.opendocument.presentation" },
            
            // Исполняемые файлы
            { ".exe", "application/octet-stream" },
            { ".dll", "application/octet-stream" },
            { ".bin", "application/octet-stream" },
            { ".iso", "application/octet-stream" },
            { ".msi", "application/octet-stream" },
            { ".apk", "application/vnd.android.package-archive" },
            { ".deb", "application/x-deb" },
            { ".rpm", "application/x-rpm" }
        };

        public static string GetMimeType(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(extension))
            {
                return "application/octet-stream";
            }

            if (_typeMappings.TryGetValue(extension, out string mimeType))
            {
                return mimeType;
            }

            return "application/octet-stream";
        }
    }
}