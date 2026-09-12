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
        protected readonly decimal currentBudget_;
        protected readonly int maxPossibleStrategies_;
        public ExpenseRateService(int maxPossibleStrategies, List<Expense> expenses, IWriter writer, decimal currentBudget, DateOnly start, DateOnly end, DateOnly cur) : base(writer)
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
            public decimal optimalRate;     //Оптимальный ср. расход (р/день)
            public Difference difference;   //Текущая разница на основе текущего ср. расхода (р/день)
            public List<Difference> possibleStrategies;//Возможные стратегии в случае нехватки бюджета
            public DateOnly startDate;      //Начало периода
            public DateOnly endDate;        //Конец периода
            public DateOnly curDate;        //Текущий момент. Обязательно должен быть больше startDate и меньше endDate
            public decimal curentBudget;    //Текущий бюджет
            public decimal newBudget;       //Остаток в конце месяца
            public decimal newOptimalRate;  //Оптимальный расход  (р/день) с учетом кол-ва оставшихся дней и текущего остатка в конце месяца
            public List<Expense> expenses;

            public Report(List<Expense> expenses, decimal optimalRate, decimal difference, decimal currentRate, DateOnly startDate, DateOnly endDate, DateOnly curDate, decimal curentBudget, decimal newBudget = 0.0m, decimal newOptimalRate = 0.0m)
            {
                this.optimalRate = optimalRate;
                this.difference = new(difference, currentRate);
                this.startDate = startDate;
                this.endDate = endDate;
                this.curDate = curDate;
                this.curentBudget = curentBudget;
                possibleStrategies = new List<Difference>();
                this.newOptimalRate = newOptimalRate;
                this.newBudget = newBudget;
                this.expenses = expenses;
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

            var intermediateSum = tmplinq.Sum(e => e.Date.CompareTo(curDate_) <= 0 ? e.Sum : 0.0m);//Сумма всех расходов

            var cRate = intermediateSum / (curDate_.DayNumber - startDate_.DayNumber);//Текущий расход с учетом кол-ва дней

            var dayCount = endDate_.DayNumber - startDate_.DayNumber;//Общее кол-во дней

            var optimalRate = currentBudget_ / dayCount; //Оптимальный средний расход за период (оптимальный значит такой расход который оставит ноль в конце периода)

            var diff = optimalRate - cRate;

            decimal newBudget = currentBudget_ - intermediateSum;//Остаток в конце месяца

            var report = new Report(
                      expenses: expenses_,
                   optimalRate: optimalRate,
                    difference: diff,
                   currentRate: cRate,
                     startDate: startDate_,
                       endDate: endDate_,
                       curDate: curDate_,
                  curentBudget: currentBudget_,
                     newBudget: newBudget
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
                    report.possibleStrategies.Add(new(intermediateDiff, newRate));
                }

                report.newOptimalRate = newBudget / (endDate_.DayNumber - curDate_.DayNumber);
            }

            writer_.AddReport(
                report
            );
        }

    }
}

