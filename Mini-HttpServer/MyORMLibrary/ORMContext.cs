using Npgsql;

namespace MyORMLibrary
{
    public class ORMContext
    {
        private readonly string _connectionString;

        public ORMContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Create(User user, string tableName)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            string sql = $"INSERT INTO {tableName} (Username, Login, Password, Age) VALUES (@username, @login, @password, @age)";
            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@username", user.Username);
            command.Parameters.AddWithValue("@login", user.Login);
            command.Parameters.AddWithValue("@password", user.Password);
            command.Parameters.AddWithValue("@age", user.Age);

            command.ExecuteNonQuery();
        }

        public User ReadById(int id, string tableName)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            string sql = $"SELECT * FROM {tableName} WHERE Id = @id";
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Login = reader.GetString(2),
                    Password = reader.GetString(3),
                    Age = reader.GetInt32(4)
                };
            }
            return null;
        }

        public List<User> ReadAll(string tableName)
        {
            var users = new List<User>();

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            string sql = $"SELECT * FROM {tableName}";
            using var command = new NpgsqlCommand(sql, connection);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Login = reader.GetString(2),
                    Password = reader.GetString(3),
                    Age = reader.GetInt32(4)
                });
            }
            return users;
        }

        public void Update(int id, User user, string tableName)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            string sql = $"UPDATE {tableName} SET Username = @username, Login = @login, Password = @password, Age = @age WHERE Id = @id";
            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@username", user.Username);
            command.Parameters.AddWithValue("@login", user.Login);
            command.Parameters.AddWithValue("@password", user.Password);
            command.Parameters.AddWithValue("@age", user.Age);
            command.Parameters.AddWithValue("@id", id);

            command.ExecuteNonQuery();
        }

        public void Delete(int id, string tableName)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            string sql = $"DELETE FROM {tableName} WHERE Id = @id";
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);

            command.ExecuteNonQuery();
        }
    }
}
