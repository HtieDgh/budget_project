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
        protected readonly int maxPossibleStrategies_;
        public ExpenseRateService(int maxPossibleStrategies, List<Expense> expenses, IWriter writer, long currentBudget, DateOnly start, DateOnly end, DateOnly cur) : base(writer)
        {
            startDate_ = start;
            endDate_ = end;
            curDate_ = cur;
            expenses_ = expenses;
            currentBudget_ = currentBudget;
            maxPossibleStrategies_ = maxPossibleStrategies;
        }
        public record Difference(decimal Value, decimal CurrentRate);
        /// <summary>
        /// Результат-отчет работы сервиса, передаваемый в writer_
        /// </summary>
        public record Report
        {
            public decimal OptimalRate;     //Оптимальный ср. расход (р/день)
            public Difference Difference;   //Текущая разница на основе текущего ср. расхода (р/день)
            public List<Difference> PossibleStrategies;//Возможные стратегии в случае нехватки бюджета
            public DateOnly StartDate;      //Начало периода
            public DateOnly EndDate;        //Конец периода
            public DateOnly CurDate;        //Текущий момент
            public decimal CurentBudget;    //Текущий бюджет
            public decimal NewBudget;       //Остаток в конце месяца
            public decimal NewOptimalRate;  //Оптимальный расход  (р/день) с учетом кол-ва оставшихся дней и текущего остатка в конце месяца
            public List<Expense> Expenses;

            public Report(List<Expense> expenses, decimal optimalRate, decimal difference, decimal currentRate, DateOnly startDate, DateOnly endDate, DateOnly curDate, decimal curentBudget, decimal newBudget = 0.0m, decimal newOptimalRate = 0.0m)
            {
                OptimalRate = optimalRate;
                Difference = new(difference, currentRate);
                StartDate = startDate;
                EndDate = endDate;
                CurDate = curDate;
                CurentBudget = curentBudget;
                PossibleStrategies = new List<Difference>();
                NewOptimalRate = newOptimalRate;
                NewBudget = newBudget;
                Expenses = expenses;
            }
        }

        /// <summary>
        /// Главный метод, основная логика
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

            decimal newBudget = currentBudget_ - tmplinq.Sum(e => e.Date.CompareTo(curDate_) <= 0 ? e.Sum : 0.0m);//Остаток в конце месяца

            var report = new Report(
                    expenses_,
                    optimalRate,
                    diff,
                    cRate,
                    startDate_,
                    endDate_,
                    curDate_,
                    currentBudget_,
                    newBudget
                );

            if (diff < 0.0m)//Если бюджета все же не хватит, попробовать посчитать расходы если не тратить некоторое время или тратить не больше чем некоторое значение
            {
                var intermediateDiff = -1.0m;
                var newRate = 0.0m;
                DateOnly newDate;
                for (var i = 1; intermediateDiff < 0.0m && i < maxPossibleStrategies_; i++)
                {
                    newDate = curDate_.AddDays(i);//TODO Если произошел выход за endDate_ то это лишь означает что траты уже превысили бюджет. Следует не тратить даже после получения зарплаты?
                    newRate = tmplinq.Sum(e => e.Date.CompareTo(newDate) <= 0 ? e.Sum : 0.0m) / (newDate.DayNumber - startDate_.DayNumber);
                    intermediateDiff = optimalRate - newRate;
                    report.PossibleStrategies.Add(new(intermediateDiff, newRate));
                }

                report.NewOptimalRate = newBudget / (endDate_.DayNumber - curDate_.DayNumber);
            }

            writer_.AddReport(
                report
            );
        }

    }
}

