using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.Attributes;
using MiniHttpServer.Framework.Core.HttpResponse;
using MiniHttpServer.Framework.Settings;
using MiniHttpServer.Models;
using MyORMLibrary;
using System;
using System.Collections.Generic;

namespace MiniHttpServer.Endpoints
{
    [Endpoint("/admin")]
    internal class AdminEndpoint : EndpointBase
    {
        [HttpGet]
        public IHttpResult Index()
        {
            var redirect = CheckAdminAccess();
            if (redirect != null) return redirect;

            try
            {
                var settings = Singleton.GetInstance().Settings;

                var queryBuilder = new QueryBuilder<Tour>(settings.ConnectionString, "tours");
                var tours = queryBuilder.GetAll("Id");

                var toursForTemplate = tours.ConvertAll(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    Price = t.Price.ToString("N0"),
                    t.Duration,
                    t.Country,
                    t.ImageUrl,
                    DepartureDate = t.DepartureDate.ToString("yyyy-MM-dd"),
                    t.Nights,
                    t.Rating,
                    t.Location
                });

                return Page("admin/index.html", new { Tours = toursForTemplate });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка админ-панели: {ex.Message}");
                Context.Response.StatusCode = 500;
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("create")]
        public IHttpResult CreatePage()
        {
            var redirect = CheckAdminAccess();
            if (redirect != null) return redirect;
            return Page("admin/create.html", new { });
        }

        [HttpPost("create")]
        public async Task<IHttpResult> Create()
        {
            var redirect = CheckAdminAccess();
            if (redirect != null) return redirect;

            try
            {
                //Читаем тело запроса
                using var reader = new StreamReader(Context.Request.InputStream);
                var formData = await reader.ReadToEndAsync();
                var data = ParseFormData(formData);

                // Собираем объект Tour из данных формы
                var tour = new Tour
                {
                    Title = data.GetValueOrDefault("title", ""),
                    Description = data.GetValueOrDefault("description", ""),
                    Price = decimal.Parse(data.GetValueOrDefault("price", "0")),
                    Duration = int.Parse(data.GetValueOrDefault("duration", "0")),
                    Country = data.GetValueOrDefault("country", ""),
                    ImageUrl = data.GetValueOrDefault("image_url", ""),
                    IsActive = true,
                    DepartureDate = DateTime.Parse(data.GetValueOrDefault("departure_date", DateTime.Now.ToString("yyyy-MM-dd"))),
                    Nights = int.Parse(data.GetValueOrDefault("nights", "0")),
                    Rating = int.Parse(data.GetValueOrDefault("rating", "0")),
                    Location = data.GetValueOrDefault("location", "")
                };

                // Используем ORM вместо сырого SQL
                var settings = Singleton.GetInstance().Settings;
                var queryBuilder = new QueryBuilder<Tour>(settings.ConnectionString, "tours");
                queryBuilder.Insert(tour);

                return Redirect("/admin");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания тура: {ex.Message}");
                Context.Response.StatusCode = 500;
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("edit/{id}")]
        public IHttpResult EditPage(int id)
        {
            var redirect = CheckAdminAccess();
            if (redirect != null) return redirect;

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
                    tour.Duration,
                    tour.Country,
                    tour.ImageUrl,
                    DepartureDate = tour.DepartureDate.ToString("yyyy-MM-dd"),
                    tour.Nights,
                    tour.Rating,
                    tour.Location
                };

                return Page("admin/edit.html", new { Tour = tourData });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки тура: {ex.Message}");
                Context.Response.StatusCode = 500;
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost("edit/{id}")]
        public async Task<IHttpResult> Edit(int id)
        {
            var redirect = CheckAdminAccess();
            if (redirect != null) return redirect;

            try
            {
                using var reader = new StreamReader(Context.Request.InputStream);
                var formData = await reader.ReadToEndAsync();
                var data = ParseFormData(formData);

                // Собираем объект Tour из данных формы с указанием Id для WHERE
                var tour = new Tour
                {
                    Id = id,
                    Title = data.GetValueOrDefault("title", ""),
                    Description = data.GetValueOrDefault("description", ""),
                    Price = decimal.Parse(data.GetValueOrDefault("price", "0")),
                    Duration = int.Parse(data.GetValueOrDefault("duration", "0")),
                    Country = data.GetValueOrDefault("country", ""),
                    ImageUrl = data.GetValueOrDefault("image_url", ""),
                    IsActive = true,
                    DepartureDate = DateTime.Parse(data.GetValueOrDefault("departure_date", DateTime.Now.ToString("yyyy-MM-dd"))),
                    Nights = int.Parse(data.GetValueOrDefault("nights", "0")),
                    Rating = int.Parse(data.GetValueOrDefault("rating", "0")),
                    Location = data.GetValueOrDefault("location", "")
                };

                // Используем ORM вместо сырого SQL
                var settings = Singleton.GetInstance().Settings;
                var queryBuilder = new QueryBuilder<Tour>(settings.ConnectionString, "tours");
                queryBuilder.Update(tour);

                return Redirect("/admin");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления тура: {ex.Message}");
                Context.Response.StatusCode = 500;
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost("delete/{id}")]
        public IHttpResult Delete(int id)
        {
            var redirect = CheckAdminAccess();
            if (redirect != null) return redirect;

            try
            {
                // Используем ORM вместо сырого SQL
                var settings = Singleton.GetInstance().Settings;
                var queryBuilder = new QueryBuilder<Tour>(settings.ConnectionString, "tours");
                queryBuilder.Delete(id);

                return Redirect("/admin");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления тура: {ex.Message}");
                Context.Response.StatusCode = 500;
                return Json(new { error = ex.Message });
            }
        }

        private IHttpResult Redirect(string url)
        {
            return new RedirectResult(url);
        }

        private Dictionary<string, string> ParseFormData(string formData)
        {
            var result = new Dictionary<string, string>();
            var pairs = formData.Split('&');
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2)
                {
                    result[parts[0]] = Uri.UnescapeDataString(parts[1].Replace('+', ' '));
                }
            }
            return result;
        }
    }
}