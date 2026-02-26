using System.Linq.Expressions;
using System.Text;
using Npgsql;

namespace MyORMLibrary
{
    public class QueryBuilder<T> where T : class, new()
    {
        private readonly string connectionString;
        private readonly string tableName;

        //Сохраняет строку подключения. Если имя таблицы не передано — генерирует автоматически
        public QueryBuilder(string connectionString, string tableName = null)
        {
            this.connectionString = connectionString;
            this.tableName = tableName ?? typeof(T).Name + "s";
        }


        // SELECT * FROM table WHERE condition
        public List<T> Where(Expression<Func<T, bool>> condition)
        {
            string whereClause = ParseExpression(condition.Body);
            string sql = $"SELECT * FROM {tableName} WHERE {whereClause}";
            return ExecuteQuery(sql);
        }

        // SELECT * FROM table WHERE condition LIMIT 1
        public T FirstOrDefault(Expression<Func<T, bool>> condition)
        {
            string whereClause = ParseExpression(condition.Body);
            string sql = $"SELECT * FROM {tableName} WHERE {whereClause} LIMIT 1";
            var results = ExecuteQuery(sql);
            return results.FirstOrDefault();
        }

        // SELECT * FROM table (все записи)
        public List<T> GetAll()
        {
            string sql = $"SELECT * FROM {tableName}";
            return ExecuteQuery(sql);
        }

        // SELECT * FROM table ORDER BY column
        public List<T> GetAll(string orderBy)
        {
            string sql = $"SELECT * FROM {tableName} ORDER BY {ToSnakeCase(orderBy)}";
            return ExecuteQuery(sql);
        }


        public void Insert(T entity)
        {
            // Получаем все свойства кроме Id (Id генерируется автоматически в БД)
            var properties = typeof(T).GetProperties()
                .Where(p => !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Собираем имена колонок: "title, price, country, ..."
            var columns = string.Join(", ", properties.Select(p => ToSnakeCase(p.Name)));

            // Собираем параметры: "@Title, @Price, @Country, ..."
            var parameters = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            string sql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            using var command = new NpgsqlCommand(sql, connection);

            // Через рефлексию достаём значение каждого свойства и подставляем в параметр
            foreach (var prop in properties)
            {
                var value = prop.GetValue(entity) ?? DBNull.Value;
                command.Parameters.AddWithValue($"@{prop.Name}", value);
            }

            command.ExecuteNonQuery();
        }

        public void Update(T entity)
        {
            // Все свойства кроме Id — для SET
            var properties = typeof(T).GetProperties()
                .Where(p => !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Собираем "title = @Title, price = @Price, country = @Country, ..."
            var setClauses = string.Join(", ", properties.Select(p => $"{ToSnakeCase(p.Name)} = @{p.Name}"));

            string sql = $"UPDATE {tableName} SET {setClauses} WHERE id = @Id";

            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            using var command = new NpgsqlCommand(sql, connection);

            // Параметры для SET
            foreach (var prop in properties)
            {
                var value = prop.GetValue(entity) ?? DBNull.Value;
                command.Parameters.AddWithValue($"@{prop.Name}", value);
            }

            // Параметр для WHERE id = @Id
            var idProp = typeof(T).GetProperty("Id");
            command.Parameters.AddWithValue("@Id", idProp.GetValue(entity));

            command.ExecuteNonQuery();
        }

        // DELETE FROM table WHERE id = @Id
        public void Delete(int id)
        {
            string sql = $"DELETE FROM {tableName} WHERE id = @Id";

            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.ExecuteNonQuery();
        }


        //превращает C#-выражение в SQL-строку
        private string ParseExpression(Expression expression)
        {
            if (expression is BinaryExpression binary)
            {
                string left = ParseExpression(binary.Left);
                string right = ParseExpression(binary.Right);
                string op = GetSqlOperator(binary.NodeType);
                return $"{left} {op} {right}";
            }

            if (expression is MemberExpression member)
            {
                // Если это свойство параметра лямбды (t.IsActive) — это имя колонки
                if (member.Expression is ParameterExpression)
                {
                    return ToSnakeCase(member.Member.Name);
                }

                // Если это внешняя переменная (id, name) — вычисляем её значение
                return FormatConstant(GetMemberValue(member));
            }

            //Константа (число, строка):
            if (expression is ConstantExpression constant)
            {
                return FormatConstant(constant.Value);
            }

            //Унарное выражение (один операнд):
            if (expression is UnaryExpression unary)
            {
                return ParseExpression(unary.Operand);
            }

            //Вызов метода:
            if (expression is MethodCallExpression methodCall)
            {
                return ParseMethodCall(methodCall);
            }

            throw new NotSupportedException($"Unsupported expression type: {expression.GetType().Name}");
        }

        //таблица соответствий
        private string GetSqlOperator(ExpressionType nodeType)
        {
            return nodeType switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "!=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.LessThan => "<",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.AndAlso => "AND",
                ExpressionType.OrElse => "OR",
                _ => throw new NotSupportedException($"Unsupported node type: {nodeType}")
            };
        }

        //обработка null, bool, decimal
        private string FormatConstant(object value)
        {
            if (value is null) return "NULL";
            if (value is string s) return $"'{s}'";
            if (value is bool b) return b ? "true" : "false";  // PostgreSQL ожидает true/false с маленькой буквы
            if (value is decimal d) return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return value.ToString();
        }

        // Например: var id = 5; Where(t => t.Id == id) — нужно получить значение 5
        private object GetMemberValue(MemberExpression member)
        {
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        //обработка вызова метода
        private string ParseMethodCall(MethodCallExpression methodCall)
        {
            if (methodCall.Method.Name == "Contains" && methodCall.Arguments.Count == 1)
            {
                var member = ParseExpression(methodCall.Object);
                var value = ParseExpression(methodCall.Arguments[0]);
                return $"{member} LIKE '%{value.Trim('\'')}%'";
            }

            throw new NotSupportedException($"Unsupported method: {methodCall.Method.Name}");
        }


        // выполнение запроса
        private List<T> ExecuteQuery(string sql)
        {
            var results = new List<T>();

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(sql, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        //Каждую строку превращаем в объект
                        results.Add(MapToEntity(reader));
                    }
                }
            }

            return results;
        }

        // ToPascalCase для конвертации snake_case колонок в PascalCase свойства
        private T MapToEntity(NpgsqlDataReader reader)
        {
            var entity = new T();
            var properties = typeof(T).GetProperties();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);
                string pascalName = ToPascalCase(columnName); 

                var property = properties.FirstOrDefault(p =>
                    p.Name.Equals(pascalName, StringComparison.OrdinalIgnoreCase));

                if (property != null && property.CanWrite && !reader.IsDBNull(i))
                {
                    object value = reader.GetValue(i);

                    // Конвертация типов PostgreSQL → C#
                    if (property.PropertyType == typeof(decimal) && value is not decimal)
                        value = Convert.ToDecimal(value);
                    else if (property.PropertyType == typeof(int) && value is not int)
                        value = Convert.ToInt32(value);
                    else if (property.PropertyType == typeof(bool) && value is not bool)
                        value = Convert.ToBoolean(value);
                    else if (property.PropertyType == typeof(DateTime) && value is DateOnly dateOnly)
                        value = dateOnly.ToDateTime(TimeOnly.MinValue);

                    property.SetValue(entity, value);
                }
            }

            return entity;
        }

        // Нужен в ParseExpression: свойство C# t.IsActive → SQL-колонка is_active
        private static string ToSnakeCase(string name)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char ch = name[i];
                if (char.IsUpper(ch))
                {
                    if (i > 0) sb.Append('_');
                    sb.Append(char.ToLower(ch));
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        // Нужен в MapToEntity: колонка БД image_url → свойство C# ImageUrl
        private static string ToPascalCase(string name)
        {
            var sb = new StringBuilder();
            bool capitalizeNext = true;
            foreach (char ch in name)
            {
                if (ch == '_')
                {
                    capitalizeNext = true;
                }
                else
                {
                    sb.Append(capitalizeNext ? char.ToUpper(ch) : ch);
                    capitalizeNext = false;
                }
            }
            return sb.ToString();
        }
    }
}