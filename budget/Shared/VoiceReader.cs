using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Text;
using System.Text.RegularExpressions;
using Vosk;
namespace budget.Shared
{
    /// <summary>
    /// Ридер, использующий речь в качестве ввода расходов
    /// </summary>
    public class VoiceReader : IReader
    {
        string modelDir_;
        int realYear_;
        string[] expenseHeaderShema_;
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="modelDir">директория с папкой модели</param>
        public VoiceReader(string modelDir, string[] expenseHeaderShema)
        {
            modelDir_ = modelDir;
            expenseHeaderShema_ = expenseHeaderShema;
            realYear_ = DateTime.Now.Year;
        }
        public IEnumerable<string[]> Read()
        {
            return Parse_();
        }
        /// <summary>
        /// Основная логика распознования голоса
        /// </summary>
        /// <returns>Полученный стрингбилдер со всеми распознаными фразами</returns>
        protected StringBuilder Recognise_()
        {
            // 1. Загружаем модель
            Vosk.Vosk.SetLogLevel(0);
            using var model = new Model(modelDir_);

            // 2. Создаем распознаватель. Vosk ожидает 16kHz, моно, 16-bit PCM.
            using var rec = new VoskRecognizer(model, 16000.0f);

            // 3. Используем WasapiCapture вместо WaveInEvent для работы через WASAPI.
            // По умолчанию будет использоваться устройство ввода по умолчанию.
            using var capture = new WasapiCapture();

            var sb = new StringBuilder();
            // ВАЖНО: Приводим формат захвата к ожидаемому Vosk.
            // WasapiCapture по умолчанию может выдавать аудио в формате устройства
            // (например, 48kHz, стерео, float). Vosk требует 16kHz, моно, 16-bit.
            // Для этого можно использовать WaveFormatConversionStream или
            // задать формат через свойства capture перед запуском, если это возможно.
            // Простейший путь — буферизация и ресемплинг, но для примера
            // мы просто передадим данные, если формат совпадает, или
            // настроим capture на нужный формат, если устройство поддерживает.

            // Попытка задать формат напрямую (может не сработать для всех устройств):
            capture.WaveFormat = new WaveFormat(16000, 16, 1);

            // Если устройство не поддерживает 16kHz напрямую, потребуется
            // дополнительный ресемплинг. В этом примере мы предполагаем,
            // что данные приходят в нужном формате, или устройство поддерживает его.

            // 4. Обработчик события получения данных
            capture.DataAvailable += (sender, e) =>
            {
                // Передаем байты в Vosk
                if (rec.AcceptWaveform(e.Buffer, e.BytesRecorded))
                {
                    var result = rec.Result();
                   
                    var text = System.Text.Json.JsonDocument.Parse(result)
                        .RootElement.GetProperty("text").GetString();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        sb.Append($"{text} ");
                        Console.Write('=');
                    }
                }
            };

            capture.RecordingStopped += (sender, e) =>
            {
                Console.WriteLine("Запись остановлена.");
            };

            // 5. Запуск и ожидание
            capture.StartRecording();
            Console.WriteLine("Говорите... Если количество символов '=' не увеличивается, то, скорее всего, микрофон вас не слышит - проверьте микрофон и перезапустите приложение. Нажмите Enter для окончания диктовки.");
            Console.Write('=');
            Console.ReadLine();

            capture.StopRecording();

            // Получаем последний остаток текста
            return sb;

        }
        // Словарь: слово-цифра -> символ
        private static readonly Dictionary<string, string> DigitWords = new()
        {
            ["ноль"] = "0",
            ["один"] = "1",
            ["одна"] = "1",
            ["два"] = "2",
            ["две"] = "2",
            ["три"] = "3",
            ["четыре"] = "4",
            ["пять"] = "5",
            ["шесть"] = "6",
            ["семь"] = "7",
            ["восемь"] = "8",
            ["девять"] = "9"
        };

        // Порядковые числительные -> номер дня
        private static readonly Dictionary<string, int> OrdinalDays = new()
        {
            ["первое"] = 1,
            ["второе"] = 2,
            ["третье"] = 3,
            ["четвёртое"] = 4,
            ["четвертое"] = 4,
            ["пятое"] = 5,
            ["шестое"] = 6,
            ["седьмое"] = 7,
            ["восьмое"] = 8,
            ["девятое"] = 9,
            ["десятое"] = 10,
            ["одиннадцатое"] = 11,
            ["двенадцатое"] = 12,
            ["тринадцатое"] = 13,
            ["четырнадцатое"] = 14,
            ["пятнадцатое"] = 15,
            ["шестнадцатое"] = 16,
            ["семнадцатое"] = 17,
            ["восемнадцатое"] = 18,
            ["девятнадцатое"] = 19,
            ["двадцатое"] = 20,
            ["двадцать первое"] = 21,
            ["двадцать второе"] = 22,
            ["двадцать третье"] = 23,
            ["двадцать четвёртое"] = 24,
            ["двадцать четвертое"] = 24,
            ["двадцать пятое"] = 25,
            ["двадцать шестое"] = 26,
            ["двадцать седьмое"] = 27,
            ["двадцать восьмое"] = 28,
            ["двадцать девятое"] = 29,
            ["тридцатое"] = 30,
            ["тридцать первое"] = 31
        };
        // Словарь месяцев: все падежные формы -> каноническое название в родительном падеже
        // слово (любой падеж) -> (номер месяца, каноническое имя в родительном падеже)
        private static readonly Dictionary<string, string> MonthWords = new()
        {
            ["январь"] = "01",
            ["января"] = "01",
            ["февраль"] = "02",
            ["февраля"] = "02",
            ["март"] = "03",
            ["марта"] = "03",
            ["апрель"] = "04",
            ["апреля"] = "04",
            ["май"] = "05",
            ["мая"] = "05",
            ["июнь"] = "06",
            ["июня"] = "06",
            ["июль"] = "07",
            ["июля"] = "07",
            ["август"] = "08",
            ["августа"] = "08",
            ["сентябрь"] = "09",
            ["сентября"] = "09",
            ["октябрь"] = "10",
            ["октября"] = "10",
            ["ноябрь"] = "11",
            ["ноября"] = "11",
            ["декабрь"] = "12",
            ["декабря"] = "12"
        };

        // Превращает "восемь девять девять" -> "899"
        private static string DigitsToNumber_(string words)
        {
            if (string.IsNullOrWhiteSpace(words)) return "";

            var sb = new StringBuilder();
            foreach (var w in words.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (DigitWords.TryGetValue(w, out var d))
                    sb.Append(d);
            }
            return sb.ToString();
        }
        /// <summary>
        /// Парсинг надектованных расходов
        /// </summary>
        /// <param name="monthName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        protected List<string[]> Parse_()
        {
            var text = Recognise_().ToString().ToLowerInvariant().Trim();

            // Месяцы — сортируем по длине, чтобы "сентябрь" не разбился на "сентябр"
            string monthWords = string.Join("|", MonthWords.Keys.OrderByDescending(k => k.Length));
            string dayWords = string.Join("|", OrdinalDays.Keys.OrderByDescending(k => k.Length));
            string digitWords = string.Join("|", DigitWords.Keys);

            string pattern = $@"(?<date>{dayWords})\s+(?<month>{monthWords})" +
                             $@"(?:\s+(?<rubles>(?:{digitWords})(?:\s+(?:{digitWords}))*))?" +
                             $@"(?:\s+запятая(?:\s+(?<kopecks>(?:{digitWords})(?:\s+(?:{digitWords}))*)))?";

            var result = new List<string[]> { expenseHeaderShema_ };
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);

            foreach (Match m in regex.Matches(text))
            {
                int day = OrdinalDays[m.Groups["date"].Value];
                // Приводим месяц к родительному падежу
                string month = MonthWords[m.Groups["month"].Value];

                string rubles = DigitsToNumber_(m.Groups["rubles"].Value);
                string kopecks = DigitsToNumber_(m.Groups["kopecks"].Value);

                string amount = rubles;
                if (!string.IsNullOrEmpty(kopecks))
                    amount += "," + kopecks.PadLeft(2, '0');


                result.Add([amount, $"{realYear_}-{month}-{day.ToString("D2")}", "Voice"]);
            }
            
            return result;
        }
    }
}