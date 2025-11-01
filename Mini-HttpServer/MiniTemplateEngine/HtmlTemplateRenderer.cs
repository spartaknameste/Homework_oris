using System;
using System.IO;

namespace MiniTemplateEngine
{
    public class HtmlTemplateRenderer : IHtmlTemplateRenderer
    {
        public string RenderFromString(string htmlTemplate, object dataModel)
        {
            return htmlTemplate;
        }

        public string RenderFromFile(string filePath, object dataModel)
        {
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
            throw new FileNotFoundException($"Template file not found: {filePath}");
        }

        public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
        {
            string content = RenderFromFile(inputFilePath, dataModel);
            File.WriteAllText(outputFilePath, content);
            return content;
        }
    }
}