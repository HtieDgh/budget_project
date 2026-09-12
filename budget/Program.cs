using budget.Models;
using budget.Shared;
using Fclp;
using Fclp.Internals;
using System.Text;
using System.Text.Json;
using static budget.Program.Options;

namespace budget
{
    public class Program
    {
        /// <summary>
        /// Описание возможных аргументов командной строки
        /// </summary>
        public class Options
        {
            public string? InputExpenseFilePath { get; set; }
            public string? OutputFilePath { get; set; }
            public string? StartDate { get; set; }
            public string? EndDate { get; set; }
            public string? CurDate { get; set; }
            public string? CurrentBudget { get; set; }
            public string? InputOcrImgesDirectory { get; set; }
            public bool VoiceRecognition { get; set; }
            public bool IsCurrentMonth { get; set; }

            public static readonly string InputExpenseFilePath_d = "Input file in csv format. Default as in config file.";
            public static readonly string OutputFilePath_d = "Write JSON output to file. File wil be overitten.";
            public static readonly string StartDate_d = "The date from which to take statistics into account. Default is DateOnly:MinValue";
            public static readonly string EndDate_d = "The date up to which statistics should be taken into account, not inclusive. Default is DateOnly:MaxValue";
            public static readonly string CurDate_d = "Current date. Default is today.";
            public static readonly string CurrentBudget_d = "Current budget.";
            public static readonly string InputOcrImgesDirectory_d = "Use OCR to read Sberbank mobile app screenshotes. Option -i will be ignored.";
            public static readonly string VoiceRecognition_d = "Use VOSK and Naudio to read from speach.";
            public static readonly string IsCurrentMonth_d = "Use current month for Voice recognition parsing.";
            public static readonly string helpText =
                """
                USAGE:
                    budget -s 2026-09-01 -e 2026-10-01 -b 15000 
                  
                """;
            public class Formater : ICommandLineOptionFormatter
            {
                private readonly int _optionNamePadding;

                /// <summary>
                /// Создает экземпляр форматтера с заданным отступом для названий опций.
                /// </summary>
                /// <param name="optionNamePadding">Минимальное количество символов для выравнивания названий опций.</param>
                public Formater(int optionNamePadding = 25)
                {
                    _optionNamePadding = optionNamePadding;
                }

                /// <summary>
                /// Основной метод форматирования. Принимает список опций и возвращает строку справки.
                /// </summary>
                public string Format(IEnumerable<ICommandLineOption> options)
                {
                    if (options == null || !options.Any())
                        return "  ERROR: No option provided.";

                    var sb = new StringBuilder();
                    sb.AppendLine("OPTIONS:");
                    sb.AppendLine("");

                    // Группируем опции: сначала обязательные, потом необязательные для наглядности
                    var sortedOptions = options
                        .OrderByDescending(o => o.IsRequired)
                        .ThenBy(o => o.LongName ?? o.ShortName);

                    foreach (var option in sortedOptions)
                    {
                        AppendOptionLine(sb, option);
                    }
                    sb.AppendLine("");
                    sb.Append(helpText);
                    return sb.ToString();
                }
                /// <summary>
                /// Форматирует и добавляет в StringBuilder одну строку для опции
                /// </summary>
                private void AppendOptionLine(StringBuilder sb, ICommandLineOption option)
                {
                    // --- 1. Формируем название опции (ShortName/LongName) ---
                    var nameBuilder = new StringBuilder();
                    if (!string.IsNullOrEmpty(option.ShortName))
                        nameBuilder.Append($"-{option.ShortName}");

                    if (!string.IsNullOrEmpty(option.LongName))
                    {
                        if (nameBuilder.Length > 0)
                            nameBuilder.Append(", ");
                        nameBuilder.Append($"--{option.LongName}");
                    }

                    // Добавляем признак обязательности
                    if (option.IsRequired)
                        nameBuilder.Append(" (required)");

                    var optionName = nameBuilder.ToString();
                    var description = option.Description ?? "No description.";

                    // --- 2. Выравнивание строк ---
                    // Первая строка: название опции + описание (если помещается)
                    int paddingNeeded = Math.Max(0, _optionNamePadding - optionName.Length);
                    string paddedName = optionName + new string(' ', paddingNeeded);

                    // Формируем итоговую строку
                    sb.Append($"  {paddedName}  {description}");

                    sb.AppendLine();
                }
            }

        }

        public static int Main(string[] args)
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Настройка парсера
            var p = new FluentCommandLineParser<Options>();
            p.SetupHelp("?", "help")
              .WithCustomFormatter(new Formater())
              .Callback(text => Console.WriteLine(text));

            p.Setup(arg => arg.OutputFilePath)
             .As('o', "output")
             .WithDescription(OutputFilePath_d);

            p.Setup(arg => arg.StartDate)
             .As('s', "start-date")
             .WithDescription(StartDate_d);

            p.Setup(arg => arg.EndDate)
             .As('e', "end-date")
             .WithDescription(EndDate_d);

            p.Setup(arg => arg.InputOcrImgesDirectory)
             .As('f', "from-images")
             .WithDescription(InputOcrImgesDirectory_d);

            p.Setup(arg => arg.VoiceRecognition)
             .As("voice")
             .WithDescription(VoiceRecognition_d)
             .SetDefault(false);

            p.Setup(arg => arg.IsCurrentMonth)
             .As("current-month")
             .WithDescription(IsCurrentMonth_d)
             .SetDefault(false);

            p.Setup(arg => arg.CurrentBudget)
             .As('b', "current-budget")
             .WithDescription(CurrentBudget_d)
             .Required();

            p.Setup(arg => arg.CurDate)
             .As('t', "today")
             .WithDescription(CurDate_d)
             .SetDefault(DateOnly.FromDateTime(DateTime.Now.ToUniversalTime()).ToString("yyyy-MM-dd"));

            try
            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // Чтение конфига
                string json = File.ReadAllText( Path.Combine(AppContext.BaseDirectory, ".\\.venv\\config.json") );
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;
                Config.i().SetConfig(
                    root.GetProperty("Expense").Deserialize<ExpenseConfig>() ?? throw new Exception("config can't be read"),
                    root.GetProperty("Options").Deserialize<OptionsConfig>() ?? throw new Exception("config can't be read"),
                    root.GetProperty("OCRReader").Deserialize<OCRReaderConfig>() ?? throw new Exception("config can't be read"),
                    root.GetProperty("VoiceReader").Deserialize<VoiceReaderConfig>() ?? throw new Exception("config can't be read")
                );

                p.Setup(arg => arg.InputExpenseFilePath)
                 .As('i', "input")
                 .WithDescription(InputExpenseFilePath_d)
                 .SetDefault(Config.i().ExpenseConfig.Path);

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // Парсинг параметров
                var result = p.Parse(args);

                if (result.HasErrors)
                {
                    p.HelpOption.ShowHelp(p.Options);
                    return 1;
                }

                if (result.HelpCalled)
                    return 0;

                var resCode = OptionsHandler(p.Object);

                if (resCode != 0)
                    p.HelpOption.ShowHelp(p.Options);

                return resCode;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                p.HelpOption.ShowHelp(p.Options);
                return 1;
            }
        }

        /// <summary>
        /// Обработчик переданных аргументов. Запускает стратегии(сервисы) реалищующие аналитику
        /// </summary>
        /// <param name="opts"></param>
        /// <returns></returns>
        public static int OptionsHandler(Options opts)
        {
            int resultCode = 1;//возвращаемый код
            try
            {
                //Реализация без Microsoft.Extensions.DependencyInjection
                var controller = new Controller();

                //Регистрация сервисов
                controller.addService(ExpenseRateServiceFactory.CreateService(opts, Config.i()));

                //Запуск
                controller.Run();
                WriterConfigurator.GetWriter()?.DoWrite();//Вывод пограммы
                resultCode = 0;
            }
            catch (AggregateException ex)
            {
                // Обработка всех исключений
                foreach (var innerEx in ex.InnerExceptions)
                {
                    Console.WriteLine($"Error: {innerEx.Message}");
                }
                Console.Error.WriteLine("");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine("");
            }
            return resultCode;
        }
    }
}
