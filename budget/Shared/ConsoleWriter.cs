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
            sb.AppendLine($"Таблица - расхода");
            sb.AppendLine($"| Текущий (р/день) | Оптимальный (р/день) | Разница (р/день) | ");

            sb.AppendLine($"|---|---|---|");

            sb.AppendLine($"| {Math.Round(r.Difference.CurrentRate, 2)} | {Math.Round(r.OptimalRate, 2)} | {Math.Round(r.Difference.Value, 2)} | ");
            sb.AppendLine($"Вывод: {(r.Conclusion ? ":) Будет положительный остаток" : ":( Бюджета не хватит")}");
            if (r.Difference3day is not null || r.Difference5day is not null)
            {
                sb.AppendLine();
                sb.AppendLine($"Попытка посчитать расход при отсутвии трат на ближайшие 3 и 5 дней:");
                sb.AppendLine($"Таблица - прогноз расхода");
                sb.AppendLine($"| Без трат (дней) | Будет расход (р/день) | Разница (р/день) | Комментарий |");
                sb.AppendLine($"|---|---|---|---|");
                sb.AppendLine($"| 3 дня | {Math.Round(r.Difference3day!.CurrentRate, 2)} | {Math.Round(r.Difference3day!.Value, 2)} | {(r.Difference3day!.Value > 0 ? "<- можно жить |" : "|")}");
                sb.AppendLine($"| 5 дней | {Math.Round(r.Difference5day!.CurrentRate, 2)} | {Math.Round(r.Difference5day!.Value, 2)} | {(r.Difference3day!.Value > 0 ? "<- можно жить |" : "|")}");
                if (r.Difference3day!.Value < 0 && r.Difference3day!.Value < 0) {
                    sb.AppendLine($"Ни одна из стратегий не положительна, вывод: стоит пересмотреть траты и затянуть пояса (T_T)");
                }
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
