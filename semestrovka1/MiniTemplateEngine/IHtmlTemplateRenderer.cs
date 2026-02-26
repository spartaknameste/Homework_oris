namespace MiniTemplateEngine
{
    public interface IHtmlTemplateRenderer
    {
        string RenderFromString(string htmlTemplate, object dataModel);
        string RenderFromFile(string filePath, object dataModel);
    }
}