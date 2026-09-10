
using budget.Services;
using System.Globalization;
using static budget.Program;

namespace budget.Shared
{

    /// <summary>
    /// Интерфейс фбрики для сервисов. Возвращающает сервис, используемый в ServiceCollection
    /// </summary>
    public interface IAnalyticsServiceFactory<TService,TConfig>
    {
        public TService CreateService(Options opts, TConfig ecfg);
    }
    /// <summary>
    /// Набор конкретных файбрик
    /// </summary>
    public class ExpenseRateServiceFactory : IAnalyticsServiceFactory<ExpenseRateService, Config>
    {
        protected static ExpenseRateServiceFactory? instance_;
        ExpenseRateServiceFactory() { }
        public static ExpenseRateServiceFactory i() {
            if (instance_ == null) { 
                instance_ = new ExpenseRateServiceFactory();
            }
            return instance_;
        }
        public ExpenseRateService CreateService(Options opts, Config cfg)
        {
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

            if (!long.TryParse(opts.CurrentBudget, out var curBudget)) {
                throw new ArgumentException("current-budget option is wrong, please try again");
            }

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

            return new ExpenseRateService(
                maxPossibleStrategies: cfg.ExpenseConfig.MaxPossibleStrategies,
                expenses: Repository.GetExpenses(
                    opts.InputExpenseFilePath ?? throw new ArgumentException("No input file path provided, see --help"),
                    cfg.ExpenseConfig.HeaderShema,
                    cfg.ExpenseConfig.DateFormats,
                    opts.InputOcrImgesDirectory
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
