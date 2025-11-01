using MiniHttpServer.Framework.Core.HttpResponse;
using MiniTemplateEngine;
using System.Net;

namespace MiniHttpServer.Framework.Core.HttpResponse
{
    public class PageResult : IHttpResult
    {
        private readonly string _pathTemplate;
        private readonly object _data;
        private readonly IHtmlTemplateRenderer _templateRenderer;

        public PageResult(string pathTemplate, object data)
        {
            _pathTemplate = pathTemplate;
            _data = data;
            _templateRenderer = new HtmlTemplateRenderer();
        }

        public string Execute(HttpListenerContext context)
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.StatusCode = 200;

            try
            {
                string templatePath = FindTemplateFile(_pathTemplate);

                if (templatePath == null)
                {
                    context.Response.StatusCode = 404;
                    return $"<html><body>Template not found: {_pathTemplate}</body></html>";
                }

                return _templateRenderer.RenderFromFile(templatePath, _data);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                return $"<html><body>Template rendering error: {ex.Message}</body></html>";
            }
        }

        private string FindTemplateFile(string templatePath)
        {
            string[] possiblePaths = {
                templatePath,
                Path.Combine("Public", templatePath),
                Path.Combine("Templates", templatePath)
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }
    }
}