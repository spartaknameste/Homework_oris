using System;
using System.Collections.Generic;
using System.Linq;
using MiniHttpServer.Framework.Core.HttpResponse;
using System.Net;
using System.Text.Json;

namespace MiniHttpServer.Framework.Core.HttpResponse
{
    public class JsonResult : IHttpResult
    {
        private readonly object _data;

        public JsonResult(object data)
        {
            _data = data;
        }

        public string Execute(HttpListenerContext context)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.StatusCode = 200;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            return JsonSerializer.Serialize(_data, options);
        }
    }
}
