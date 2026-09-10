using budget.Models;
using budget.Services;

namespace budget.Shared
{
    public class Repository
    {
        protected static List<Expense>? expenses_ = null;
        public static List<Expense> GetExpenses(string infilepath , string[] expenseHeaderShema, string[] expenseDateFormats)
        {
            if (expenses_ is null)
            {
                expenses_ = new ExpenseParser(new CsvReader(infilepath), expenseHeaderShema, expenseDateFormats).GetAll();
            }
            return expenses_;
        }
    }
}
