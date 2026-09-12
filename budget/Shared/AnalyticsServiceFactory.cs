
using budget.Services;
using System.Globalization;
using static budget.Program;

namespace budget.Shared
{

    /// <summary>
    /// Интерфейс фбрики для сервисов. Возвращающает сервис, используемый в ServiceCollection
    /// </summary>
    public interface IAnalyticsServiceFactory<TService, TConfig>
    {
        abstract static TService CreateService(Options opts, TConfig ecfg);
    }
    /// <summary>
    /// Набор конкретных файбрик
    /// </summary>
    public class ExpenseRateServiceFactory : IAnalyticsServiceFactory<ExpenseRateService, Config>
    {
        public static ExpenseRateService CreateService(Options opts, Config cfg)
        {
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //Каков диапазон дат?
            DateOnly
                endDate = DateOnly.MaxValue,
                startDate = DateOnly.MinValue,
                curDate = DateOnly.ParseExact(opts.CurDate, cfg.OptionsConfig.DateFormats, CultureInfo.InvariantCulture);

            if (opts.EndDate is not null)
            {
                endDate = DateOnly.ParseExact(opts.EndDate, cfg.OptionsConfig.DateFormats, CultureInfo.InvariantCulture);
            }
            if (opts.StartDate is not null)
            {
                startDate = DateOnly.ParseExact(opts.StartDate, cfg.OptionsConfig.DateFormats, CultureInfo.InvariantCulture);
            }
            if (curDate.CompareTo(startDate) <= 0 || curDate.CompareTo(endDate) >= 0)
            {
                throw new ArgumentException($"Current date ({curDate}) is not between start ({startDate}) and end ({endDate}), please try again");
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //Какой сейчас бюджет?
            if (!decimal.TryParse(opts.CurrentBudget, CultureInfo.InvariantCulture, out var curBudget))
            {
                throw new ArgumentException("Current-budget option is wrong, please try again");
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //Куда выводим?
            IWriter writer;
            if (opts.OutputFilePath != null)//Проверка на доступ к записи
            {
                var res = FileAccessChecker.CheckWriteAccess(opts.OutputFilePath);
                if (res != FileAccessChecker.AccessResult.Success)
                    throw new ArgumentException($"No access to ({opts.OutputFilePath}): {res}");
            }

            if (opts.OutputFilePath != null)
                writer = WriterConfigurator.GetJsonWriter(opts.OutputFilePath);
            else
                writer = WriterConfigurator.GetConsoleWriter();

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //Откуда читаем?
            Repository.ReadFrom rf = Repository.ReadFrom.FILE;//По умолчанию чтение из файла
            string rfArgument=opts.InputExpenseFilePath ?? throw new ArgumentException("No input file path provided, see --help");
            if (opts.VoiceRecognition)
            {
                rf = Repository.ReadFrom.VOICE;
                rfArgument = $"{cfg.VoiceReaderConfig.DefaultVoiceModelDirectory}{cfg.VoiceReaderConfig.CurrentLanguageModel}";
            }
            else if(opts.InputOcrImgesDirectory is not null)
            {
                rf = Repository.ReadFrom.OCR;
                rfArgument = opts.InputOcrImgesDirectory;
            }
            
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Все параметры сервиса инициализированы
            return new ExpenseRateService(
                maxPossibleStrategies: cfg.ExpenseConfig.MaxPossibleStrategies,
                expenses: Repository.GetExpenses(
                    rf,
                    rfArgument,
                    cfg
                ),
                writer: writer,
                start: startDate,
                end: endDate,
                cur: curDate,
                currentBudget: curBudget
             );
        }
    }
}
