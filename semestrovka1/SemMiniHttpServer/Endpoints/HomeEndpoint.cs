using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.Attributes;
using MiniHttpServer.Framework.Core.HttpResponse;
using MiniHttpServer.Framework.Settings;
using MiniHttpServer.Models;
using Npgsql;
using MyORMLibrary;
using System;
using System.Collections.Generic;

namespace MiniHttpServer.Endpoints
{
    [Endpoint]
    internal class HomeEndpoint : EndpointBase
    {
        [HttpGet]
        public IHttpResult Index()
        {
            Console.WriteLine("HomeEndpoint.Index() вызван!");

            try
            {
                Console.WriteLine("Начинаем загрузку туров...");
                var settings = Singleton.GetInstance().Settings;

                var queryBuilder = new QueryBuilder<Tour>(settings.ConnectionString, "tours");
                var tours = queryBuilder.Where(t => t.IsActive == true);

                Console.WriteLine($"Загружено туров: {tours.Count}");

                if (tours.Count > 0)
                {
                    Console.WriteLine($"Первый тур: {tours[0].Title}");
                }

                // Преобразуем для шаблона в анонимные типы
                var toursForTemplate = tours.ConvertAll(t => new
                {
                    t.Id,
                    t.Title,
                    t.ImageUrl,
                    DepartureDate = t.DepartureDate.ToString("dd.MM"),
                    t.Nights,
                    Price = t.Price.ToString("N0"),
                    t.Rating,
                    t.Location,
                    t.Country
                });

                //Оборачиваем список в объект с полем Tours для шаблонизатора
                var data = new { Tours = toursForTemplate };

                Console.WriteLine("Рендерим index.html...");
                return Page("index.html", data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка на главной странице: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                Context.Response.StatusCode = 500;
                return Json(new { error = ex.Message });
            }
        }

    }
}
