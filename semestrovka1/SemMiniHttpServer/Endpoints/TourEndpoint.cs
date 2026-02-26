using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.Attributes;
using MiniHttpServer.Framework.Core.HttpResponse;
using MiniHttpServer.Framework.Settings;
using MiniHttpServer.Models;
using Npgsql;
using MyORMLibrary;
using System;

namespace MiniHttpServer.Endpoints
{
    [Endpoint("/tour")]
    internal class TourEndpoint : EndpointBase
    {
        [HttpGet("{id}")]
        public IHttpResult TourPage(int id)
        {
            try
            {
                var settings = Singleton.GetInstance().Settings;

                var queryBuilder = new QueryBuilder<Tour>(settings.ConnectionString, "tours");
                var tour = queryBuilder.FirstOrDefault(t => t.Id == id);

                if (tour == null)
                {
                    Context.Response.StatusCode = 404;
                    return Json(new { error = "Тур не найден" });
                }

                var tourData = new
                {
                    tour.Id,
                    tour.Title,
                    tour.Description,
                    Price = (int)tour.Price,
                    PriceFormatted = tour.Price.ToString("N0"),
                    tour.Duration,
                    tour.Country,
                    tour.ImageUrl,
                    DepartureDate = tour.DepartureDate.ToString("dd.MM.yyyy"),
                    tour.Nights,
                    tour.Rating,
                    tour.Location
                };

                return Page("tour.html", new { Tour = tourData });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка страницы тура: {ex.Message}");
                Context.Response.StatusCode = 500;
                return Json(new { error = ex.Message });
            }
        }

        
    }
}