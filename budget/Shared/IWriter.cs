using budget.Services;

namespace budget.Shared
{
    public interface IWriter
    {
        /// <summary>
        /// Добавить Report от [ExpenseRateService]
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public IWriter AddReport(ExpenseRateService.Report r);

        public void DoWrite();
    }
}
