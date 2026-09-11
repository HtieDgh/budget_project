using budget.Services;
using System.Collections.Concurrent;
using System.Text;

namespace budget.Shared
{
    /// <summary>
    /// Реализует вывод в консоль результатов обработки сервисов .
    /// </summary>
    public class ConsoleWriter : IWriter
    {
        // lines_ является критическим ресурсом, требующим внимание к потокобезопасности,
        // Поэтому используеся ConcurrentBag
        private ConcurrentBag<StringBuilder> lines_ = new ConcurrentBag<StringBuilder>();

        public IWriter AddReport(ExpenseRateService.Report r)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"## Расход на конец периода");
            sb.AppendLine($"Начало периода: {r.StartDate}");
            sb.AppendLine($"Текущий момент: {r.CurDate}");
            sb.AppendLine($"Конец периода:  {r.EndDate}");
            sb.AppendLine($"Текущий бюджет: {r.CurentBudget}");

            sb.AppendLine();
            sb.AppendLine($"Таблица - Текущие траты");
            sb.AppendLine($"| Сумма | Дата | Категория | ");
            sb.AppendLine($"|---|---|---|");
            foreach (var expense in r.Expenses) {
                sb.AppendLine($"| {Math.Round(expense.Sum, 2)} | {expense.Date} | {expense.Category} |");
            }

            sb.AppendLine();
            sb.AppendLine($"Таблица - Расход");
            sb.AppendLine($"| Текущий (р/день) | Оптимальный (р/день) | Разница (р/день) | ");
            sb.AppendLine($"|---|---|---|");

            sb.AppendLine($"| {Math.Round(r.Difference.CurrentRate, 2)} | {Math.Round(r.OptimalRate, 2)} | {Math.Round(r.Difference.Value, 2)} | ");
            sb.AppendLine($"Вывод: {(r.Difference.Value>=0 ? ":) Будет положительный остаток" : ":( Бюджета не хватит")}");
            if (r.PossibleStrategies.Count != 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Попытка посчитать расход при отсутвии трат на ближайшие дни:");
                sb.AppendLine($"Таблица - Прогноз расхода");
                sb.AppendLine($"| Без трат (дней) | Будет расход (р/день) | Разница (р/день) | Комментарий |");
                sb.AppendLine($"|---|---|---|---|");
                int i = 0;
                foreach (var ps in r.PossibleStrategies)
                {
                    sb.AppendLine($"| {++i} | {Math.Round(ps.CurrentRate, 2)} | {Math.Round(ps.Value, 2)} | {(ps.Value > 0 ? "<- можно жить |" : "|")}");
                }

                sb.AppendLine();
                sb.AppendLine($"Текущий остаток ({Math.Round(r.NewBudget, 2)})");
                sb.AppendLine($"Сегодня можно потратить не более ( {Math.Round(r.NewOptimalRate, 2)} ), а завтра можно будет больше!. Так получится протянуть до конца периода. Не забудьте добавить новую запись к расходам.");
            }

            lines_.Add(sb);
            return this;
        }
        public void DoWrite()
        {
            foreach (var sb in lines_)
            {
                Console.Out.Write(sb);
            }
        }
        public async Task DoWriteAsync(CancellationToken cancellation)
        {
            foreach (var sb in lines_)
            {
                await Console.Out.WriteAsync(sb, cancellation);
            }
        }

    }
}
