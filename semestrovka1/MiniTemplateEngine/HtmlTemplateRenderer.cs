using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniTemplateEngine
{
    public class HtmlTemplateRenderer : IHtmlTemplateRenderer
    {
        public string RenderFromString(string htmlTemplate, object dataModel)
        {
            if (htmlTemplate == null)
                throw new ArgumentNullException(nameof(htmlTemplate));

            if (dataModel == null)
                dataModel = new { };

            string result = htmlTemplate;

            // 1. Обработка foreach циклов
            result = ProcessForeachLoops(result, dataModel);

            // 2. Обработка if-else условий
            result = ProcessIfElseConditions(result, dataModel);

            // 3. Замена переменных {{variable}}
            result = ProcessVariables(result, dataModel);

            return result;
        }

        public string RenderFromFile(string filePath, object dataModel)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Template file not found: {filePath}", filePath);


            return RenderFromString(File.ReadAllText(filePath), dataModel);
        }


        // Заменяет переменные {{Property}} значениями из dataModel
        private string ProcessVariables(string template, object dataModel)
        {
            return Regex.Replace(template, @"\{\{(.+?)\}\}", match =>
            {
                string propertyPath = match.Groups[1].Value.Trim();
                object value = GetPropertyValue(dataModel, propertyPath);
                return value?.ToString() ?? "";
            });
        }

        // Обрабатывает конструкции {{if Condition}}...{{else}}...{{endif}}
        private string ProcessIfElseConditions(string template, object dataModel)
        {
            // if с else
            template = Regex.Replace(
                template,
                @"\{\{if\s+(.+?)\}\}(.+?)\{\{else\}\}(.+?)\{\{endif\}\}",
                match => EvaluateCondition(dataModel, match.Groups[1].Value.Trim())
                    ? match.Groups[2].Value
                    : match.Groups[3].Value,
                RegexOptions.Singleline
            );

            // if без else
            template = Regex.Replace(
                template,
                @"\{\{if\s+(.+?)\}\}(.+?)\{\{endif\}\}",
                match => EvaluateCondition(dataModel, match.Groups[1].Value.Trim())
                    ? match.Groups[2].Value
                    : "",
                RegexOptions.Singleline
            );

            return template;
        }

        // Обрабатывает циклы {{foreach var in Collection}}...{{endfor}}
        private string ProcessForeachLoops(string template, object dataModel)
        {
            var match = Regex.Match(template, @"\{\{foreach\s+var\s+(\w+)\s+in\s+(.+?)\}\}(.+?)\{\{endfor\}\}", RegexOptions.Singleline);

            if (match.Success)
            {
                string varName = match.Groups[1].Value;           // "tour"
                string collectionPath = match.Groups[2].Value;    // "Tours"
                string loopBody = match.Groups[3].Value;          // HTML внутри цикла


                //Через рефлексию достаёт коллекцию Tours из данных.
                var collection = GetPropertyValue(dataModel, collectionPath) as IEnumerable;
                var result = new StringBuilder();

                if (collection != null)
                {
                    foreach (var item in collection)
                    {
                        //копия тела цикла
                        string itemText = loopBody;

                        if (item != null)
                        {
                            // Если элемент примитивного типа или string
                            if (item.GetType().IsPrimitive || item is string)
                            {
                                itemText = itemText.Replace($"{{{{{varName}}}}}", item.ToString());
                            }
                            else
                            {
                                // Если элемент - объект с свойствами
                                foreach (var prop in item.GetType().GetProperties())
                                {
                                    itemText = itemText.Replace(
                                        $"{{{{{varName}.{prop.Name}}}}}",
                                        prop.GetValue(item)?.ToString() ?? ""
                                    );
                                }
                            }
                        }
                        //Добавляет обработанный HTML в результат.
                        result.Append(itemText);
                    }
                }

                template = template.Replace(match.Value, result.ToString());
            }

            return template;
        }

        // Получает значение свойства объекта по пути (например, "User.Name")
        private object GetPropertyValue(object obj, string propertyPath)
        {
            if (obj == null) return null;

            foreach (string propertyName in propertyPath.Split('.'))
            {
                if (obj == null) return null;

                var type = obj.GetType();
                var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (property == null) return null;

                obj = property.GetValue(obj);
            }

            return obj;
        }

        // Вычисляет булево условие
        private bool EvaluateCondition(object dataModel, string condition)
        {
            var value = GetPropertyValue(dataModel, condition);

            // если это bool вернуть его значение
            if (value is bool b)
                return b;

            // если не null считаем true
            return value != null;
        }
    }
}