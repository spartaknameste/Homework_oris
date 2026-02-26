using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.Attributes;
using MiniHttpServer.Framework.Core.HttpResponse;

namespace MiniHttpServer.Endpoints
{
    [Endpoint]
    internal class UserEndpoint : EndpointBase
    {
        [HttpGet("users")]
        public IHttpResult GetUsers()
        {
            try
            {
                var users = new[]
                {
                    new { Id = 1, Username = "admin", Email = "admin@example.com" },
                    new { Id = 2, Username = "user1", Email = "user1@example.com" }
                };

                return Json(users);
            }
            catch (Exception ex)
            {
                Context.Response.StatusCode = 500;
                return Json(new { error = "Database error: " + ex.Message });
            }
        }

        [HttpGet("users/{id}")]
        public IHttpResult GetUser(int id)
        {
            try
            {
                var user = new { Id = id, Username = "testuser", Email = "test@example.com" };
                return Json(user);
            }
            catch (Exception ex)
            {
                Context.Response.StatusCode = 404;
                return Json(new { error = "User not found" });
            }
        }

        [HttpPost("users")]
        public IHttpResult CreateUser()
        {
            try
            {
                var newUser = new { Id = 1, Username = "newuser", Email = "new@example.com" };
                Context.Response.StatusCode = 201;
                return Json(new { message = "User created", user = newUser });
            }
            catch (Exception ex)
            {
                Context.Response.StatusCode = 500;
                return Json(new { error = ex.Message });
            }
        }
    }
}
