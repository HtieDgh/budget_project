using budget.Models;
using budget.Services;
using System.Diagnostics.CodeAnalysis;

namespace budget.Shared
{
    public class Repository
    {
        protected static List<Expense>? expenses_ = null;
        public static List<Expense> GetExpenses(string infilepath, string[] expenseHeaderShema, string[] expenseDateFormats, string? ocrImgdir = null)
        {
            if (expenses_ is null)
            {
                IReader? reader = null;
                if (ocrImgdir is null)
                    reader = new CsvReader(infilepath);
                else
                    reader = new OCRReader(ocrImgdir);

                expenses_ = new ExpenseParser(reader, expenseHeaderShema, expenseDateFormats).GetAll();
            }
            return expenses_;
        }
    }
}
