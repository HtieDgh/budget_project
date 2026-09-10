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
            public string? startDate { get; set; }
            public string? endDate { get; set; }
            public string? curDate { get; set; }
            public string? currentBudget { get; set; }

            public static readonly string InputExpenseFilePath_d = "Input file in csv format. Default as in config file.";
            public static readonly string OutputFilePath_d = "Output file. File wil be overitten.";
            public static readonly string startDate_d = "The date from which to take statistics into account. Default is DateOnly:MinValue";
            public static readonly string endDate_d = "The date up to which statistics should be taken into account, not inclusive. Default is DateOnly:MaxValue";
            public static readonly string curDate_d = "Current date. Default is today.";
            public static readonly string currentBudget_d = "Current budget.";
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
            // create a generic parser for the ApplicationArguments type
            var p = new FluentCommandLineParser<Options>();

            // Настройка парсера
            p.Setup(arg => arg.InputExpenseFilePath)
             .As('i', "input")// Короткое и длинное имя.
             .WithDescription(InputExpenseFilePath_d)
             .SetDefault("./csv/expense.csv");//Описание к параметру

            p.Setup(arg => arg.OutputFilePath)
             .As('o', "output")
             .WithDescription(OutputFilePath_d);

            p.Setup(arg => arg.startDate)
             .As('s', "start-date")
             .WithDescription(startDate_d);

            p.Setup(arg => arg.endDate)
             .As('e', "end-date")
             .WithDescription(endDate_d);

            p.Setup(arg => arg.currentBudget)
             .As('b', "current-budget")
             .WithDescription(currentBudget_d)
             .Required();

            p.Setup(arg => arg.curDate)
             .As('t', "today")
             .WithDescription(curDate_d)
             .SetDefault(DateOnly.FromDateTime(DateTime.Now.ToUniversalTime()).ToString("yyyy-MM-dd"));

            p.SetupHelp("?", "help")
              .WithCustomFormatter(new Formater())
              .Callback(text => Console.WriteLine(text));

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

                string json = File.ReadAllText(".\\.venv\\config.json");
                using JsonDocument doc = JsonDocument.Parse(json);

                JsonElement root = doc.RootElement;

                var cfg = new Config(
                    root.GetProperty("Expense").Deserialize<ExpenseConfig>() ?? throw new Exception("config can't be read"),
                    root.GetProperty("Options").Deserialize<OptionsConfig>() ?? throw new Exception("config can't be read") 
                );
                var controller = new Controller();

                //Регистрация сервисов
                controller.addService(ExpenseRateServiceFactory.i().CreateService(opts, cfg));

                //Запуск
                controller.Run();
                WriterConfigurator.GetWriter()?.DoWrite();//Вывод пограммы
                resultCode = 0;
            }
            catch (AggregateException ex)
            {
                // Обработка всех исключений в режиме full
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
