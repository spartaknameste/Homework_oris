using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.Attributes;
using MiniHttpServer.Framework.Core.HttpResponse;
using MiniHttpServer.Framework.Settings;
using MyORMLibrary;
using Npgsql;

namespace MiniHttpServer.Endpoints
{
    [Endpoint]
    internal class UserEndpoint : EndpointBase
    {
        private readonly ORMContext _ormContext;

        public UserEndpoint()
        {
            var connectionString = Singleton.GetInstance().Settings.ConnectionString;
            _ormContext = new ORMContext(connectionString);

        }

        [HttpGet("users")]
        public IHttpResult GetUsers()
        {
            try
            {
                var users = _ormContext.ReadAll("Users");
                return Json(users);
            }
            catch (Exception ex)
            {
                Context.Response.StatusCode = 500;
                return Json(new { error = $"Database error: {ex.Message}" });
            }
        }

        [HttpGet("users/{id}")]
        public IHttpResult GetUser(int id)
        {
            string connectionString = Singleton.GetInstance().Settings.ConnectionString;

            string sqlExpression = "SELECT * FROM Users WHERE Id = @id";
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(sqlExpression, connection))
                {
                    command.Parameters.AddWithValue("@id", id);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            var result = new System.Text.StringBuilder();

                            result.AppendLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}",
                                reader.GetName(0), reader.GetName(1), reader.GetName(2),
                                reader.GetName(3), reader.GetName(4)));

                            while (reader.Read())
                            {
                                object id_val = reader.GetValue(0);
                                object username = reader.GetValue(1);
                                object login = reader.GetValue(2);
                                object password = reader.GetValue(3);
                                object age = reader.GetValue(4);

                                result.AppendLine(string.Format("{0} \t{1} \t{2} \t{3} \t{4}",
                                    id_val, username, login, password, age));
                            }

                            reader.Close();

                            return Json(result.ToString());
                        }
                        else
                        {
                            reader.Close();
                            Context.Response.StatusCode = 404;
                            return Json(new { error = "User not found" });
                        }
                    }
                }
            }
        }

        [HttpPost("users")]
        public IHttpResult CreateUser()
        {
            var newUser = new User
            {
                Username = "test_user",
                Login = "test_login",
                Password = "test_password",
                Age = 20
            };

            _ormContext.Create(newUser, "Users");

            Context.Response.StatusCode = 404;
            return Json(new { message = "User created", user = newUser });
        }

        [HttpPut("users/{id}")]
        public IHttpResult UpdateUser(int id)
        {
            var existingUser = _ormContext.ReadById(id, "Users");
            if (existingUser == null)
            {
                Context.Response.StatusCode = 404;
                return Json(new { error = "User not found" });
            }

            var updatedUser = new User
            {
                Username = "updated_user",
                Login = "updated_login",
                Password = "updated_password",
                Age = 30
            };

            _ormContext.Update(id, updatedUser, "Users");

            return Json(new { message = "User updated", user = updatedUser });
        }

        [HttpDelete("users/{id}")]
        public IHttpResult DeleteUser(int id)
        {
            var existingUser = _ormContext.ReadById(id, "Users");
            if (existingUser == null)
            {
                Context.Response.StatusCode = 404;
                return Json(new { error = "User not found" });
            }

            _ormContext.Delete(id, "Users");

            return Json(new { message = "User deleted" });
        }
    }
}