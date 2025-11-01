namespace MiniHttpServer.Framework.Shared
{
    public class GetResponseBytes
    {
        public static byte[]? Invoke(string path)
        {
            if (string.IsNullOrEmpty(path) || path == "/" || !Path.HasExtension(path))
            {
                var indexResult = TryGetFile("index.html");
                if (indexResult != null)
                    return indexResult;

                if (!string.IsNullOrEmpty(path) && path != "/")
                    return TryGetFile(path + "/index.html");
            }

            if (Path.HasExtension(path))
                return TryGetFile(path);
            else
                return TryGetFile(path + "/index.html");
        }

        private static byte[]? TryGetFile(string path)
        {
            try
            {
                var targetPath = Path.Combine(path.Split("/"));

                string? found = Directory.EnumerateFiles("Public", $"{Path.GetFileName(path)}", SearchOption.AllDirectories)
                                     .FirstOrDefault(f => f.EndsWith(targetPath, StringComparison.OrdinalIgnoreCase));

                if (found == null)
                    throw new FileNotFoundException(path);

                return File.ReadAllBytes(found);
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}