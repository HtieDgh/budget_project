using budget.Models;
using budget.Shared;
namespace budget.Services
{
    /// <summary>
    /// Сервис, расчитывающий текущий и оптимальный расход в день, делая вывод о перерасходе бюджета
    /// </summary>
    public class ExpenseRateService : AnalyticsServiceBase
    {
        protected readonly DateOnly startDate_;
        protected readonly DateOnly endDate_;
        protected readonly DateOnly curDate_;
        protected readonly List<Expense> expenses_;
        protected readonly long currentBudget_;
        public ExpenseRateService(List<Expense> expenses, IWriter writer, long currentBudget, DateOnly start, DateOnly end, DateOnly cur) : base(writer)
        {
            startDate_ = start;
            endDate_ = end;
            curDate_ = cur;
            expenses_ = expenses;
            currentBudget_ = currentBudget;
        }
        public record Difference(decimal Value, decimal CurrentRate);
        /// <summary>
        /// Результат-отчет работы сервиса, передаваемый в writer_
        /// </summary>
        public record Report
        {
            public decimal OptimalRate;
            public Difference Difference;
            public Difference? Difference3day;
            public Difference? Difference5day;
            public bool Conclusion;
            public DateOnly StartDate;
            public DateOnly EndDate;
            public DateOnly CurDate;
            public decimal CurentBudget;

            public Report(decimal optimalRate, decimal difference, bool conclusion, decimal currentRate, DateOnly startDate, DateOnly endDate, DateOnly curDate, decimal curentBudget)
            {
                OptimalRate = optimalRate;
                Difference = new(difference, currentRate);
                Conclusion = conclusion;
                StartDate = startDate;
                EndDate = endDate;
                CurDate = curDate;
                CurentBudget = curentBudget;
            }
        }

        /// <summary>
        /// Реализация синхронного и параллельного режима
        /// </summary>
        /// <returns></returns>
        public override void Run()
        {
            var tmplinq = expenses_
                .Where(
                    e => e.Date.CompareTo(startDate_) >= 0 && e.Date.CompareTo(endDate_) == -1
                );
            var cRate = tmplinq
                .Sum(e => e.Date.CompareTo(curDate_) <= 0 ? e.Sum : 0.0m);//Сумма всех расходов

            cRate = cRate / (curDate_.DayNumber - startDate_.DayNumber);//Текущий расход с учетом кол-ва дней

            var dayCount = endDate_.DayNumber - startDate_.DayNumber;//Общее кол-во дней

            var optimalRate = currentBudget_ / (decimal)dayCount; //Оптимальный средний расход за период (оптимальный значит такой расход который оставит ноль в конце периода)

            var diff = optimalRate - cRate;
            var report = new Report(
                    optimalRate,
                    diff,
                    optimalRate >= cRate,
                    cRate,
                    startDate_,
                    endDate_,
                    curDate_,
                    currentBudget_
                );
            if (diff < 0.0m)//Если бюджета все же не хватит , расчет если не хавать 3 и 5 дней
            {
                var newDate3 = curDate_.AddDays(3);//TODO учесть что возможен выход за endDate_
                var newDate5 = curDate_.AddDays(5);//TODO учесть что возможен выход за endDate_

                var newRate3 = tmplinq.Sum(e => e.Date.CompareTo(newDate3) <= 0 ? e.Sum : 0.0m) / ( newDate3.DayNumber - startDate_.DayNumber );
                var newRate5 = tmplinq.Sum(e => e.Date.CompareTo(newDate5) <= 0 ? e.Sum : 0.0m) / ( newDate5.DayNumber - startDate_.DayNumber );

                report.Difference3day = new(optimalRate - newRate3, newRate3);
                report.Difference5day = new(optimalRate - newRate5, newRate5);
            }

            writer_.AddReport(
                report
            );
        }

    }
}

