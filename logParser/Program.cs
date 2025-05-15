using logParser;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Перетащите текстовый файл на этот исполняемый файл для его открытия.");
        // For testing
        //if (args.Length == 0)
        //{
        //    args = new string[] { @"Z:\FreemDocuments\log parser Игорь\log2.txt" };
        //}
        if (args.Length == 0)
        {
            Console.WriteLine("Файл не указан. Нажмите любую клавишу для выхода...");
            Console.ReadKey();
            return;
        }

        string filePath = args[0];

        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Файл не найден.");
            }

            if (Path.GetExtension(filePath).ToLower() != ".txt")
            {
                throw new ArgumentException("Поддерживаются только текстовые файлы (.txt).");
            }
            var parsed = Parse(filePath);
            if (!parsed.Any())
            {
                Console.WriteLine("Нет данных для сохранения");
            }
            else
            {
                Save(parsed, Path.ChangeExtension(filePath, ".agg.csv"));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }

        Console.WriteLine("\nНажмите любую клавишу для выхода...");
        Console.ReadKey();
    }

    private static Dictionary<DateTime, Dictionary<string, DataRow>> Parse(string filePath)
    {
        var parsedPath = Path.ChangeExtension(filePath, "parsed.csv");
        var result = new Dictionary<DateTime, Dictionary<string, DataRow>>();

        string pattern = @"^(?<date>\d{4}/\d{2}/\d{2}-\d{2}:\d{2}:\d{2})\s+\[[^\]]+\]\s+(?<event>.+)$";
        Console.WriteLine($"Начинаю обработку");
        using (var writer = new StreamWriter(parsedPath, false, Encoding.UTF8))
        {
            writer.WriteLine("Дата;Время;Событие");
            foreach (var line in File.ReadLines(filePath))
            {
                Match match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    string dateString = match.Groups["date"].Value;
                    string eventText = match.Groups["event"].Value;
                    if (DateTime.TryParseExact(dateString,
                                    "yyyy/MM/dd-HH:mm:ss",
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.None,
                                    out DateTime dateTime))
                    {
                        writer.WriteLine($"\"{dateTime.ToString("yyyy-MM-dd")}\";\"{dateTime.ToString("HH:mm")}\";{eventText}");
                        var date = dateTime.Date;
                        if (!result.ContainsKey(date))
                        {
                            result.Add(date, new Dictionary<string, DataRow>());
                        }
                        var eventsDict = result[date];
                        if (!eventsDict.ContainsKey(eventText))
                        {
                            eventsDict.Add(eventText, new DataRow());
                        }

                        eventsDict[eventText].Count++;
                        eventsDict[eventText].Times.Add(dateTime.TimeOfDay);
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка парса даты {dateString} в формат yyyy/MM/dd-HH:mm:ss");
                    }
                }
            }
        }
        Console.WriteLine($"Распарсенный лог сохранен в файл: {parsedPath}");
        return result;
    }

    private static void Save(Dictionary<DateTime, Dictionary<string, DataRow>> data, string savePath)
    {
        try
        {
            using (var writer = new StreamWriter(savePath, false, Encoding.UTF8))
            {
                // Записываем заголовки
                writer.WriteLine("Дата;Время;Событие;Количество событий");

                // Проходим по всем элементам словаря
                foreach (var dateEntry in data)
                {
                    DateTime date = dateEntry.Key;
                    Dictionary<string, DataRow> events = dateEntry.Value;

                    // Если для даты нет событий, записываем только дату
                    if (events == null || events.Count == 0)
                    {
                        writer.WriteLine($"\"{date.ToString("yyyy-MM-dd")}\";\"\";0");
                        continue;
                    }

                    // Записываем все события для текущей даты
                    foreach (var eventEntry in events)
                    {
                        string eventName = eventEntry.Key;

                        // Экранируем кавычки в названии события
                        eventName = eventName.Replace("\"", "\"\"");

                        // Форматируем строку для CSV
                        writer.WriteLine($"\"{date.ToString("yyyy-MM-dd")}\";\"{string.Join("; ", eventEntry.Value.Times.Distinct().Select(t => t.ToString("hh\\:mm")))}\";\"{eventName}\";{eventEntry.Value.Count}");
                    }
                }
            }

            Console.WriteLine($"Сгруппированные данные сохранены в файл: {savePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при сохранении файла: {ex.Message}");
        }
    }
}