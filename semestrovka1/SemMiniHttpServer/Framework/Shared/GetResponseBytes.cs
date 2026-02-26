namespace MiniHttpServer.Framework.Shared
{
    public class GetResponseBytes
    {
        public static byte[] Invoke(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || path == "/" || path == "index.html")
                {
                    string indexPath = Path.Combine("Public", "index.html");
                    if (File.Exists(indexPath))
                    {
                        Console.WriteLine($"Файл найден: {indexPath}");
                        return File.ReadAllBytes(indexPath);
                    }
                }

                if (path.StartsWith("/"))
                    path = path.Substring(1);

     
                string fullPath = Path.Combine("Public", path);

      
                if (File.Exists(fullPath))
                {
                    Console.WriteLine($"Файл найден: {fullPath}");
                    return File.ReadAllBytes(fullPath);
                }

      
                if (!Path.HasExtension(path))
                {
                    string indexInFolder = Path.Combine("Public", path, "index.html");
                    if (File.Exists(indexInFolder))
                    {
                        Console.WriteLine($"Файл найден: {indexInFolder}");
                        return File.ReadAllBytes(indexInFolder);
                    }
                }

                Console.WriteLine($"Файл не найден: {path}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка чтения файла {path}: {ex.Message}");
                return null;
            }
        }
    }
}
