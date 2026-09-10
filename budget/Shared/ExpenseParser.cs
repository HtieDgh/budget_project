using budget.Models;
using System.Globalization;

namespace budget.Shared
{
    public class ExpenseParser : ICsvParser<Expense>
    {
        protected IReader reader_;
        protected readonly string[] expenseHeaderShema_;
        protected readonly string[] expenseDateFormats_;
        public ExpenseParser(IReader reader, string[] expenseHeaderShema, string[] expenseDateFormats)
        {
            reader_ = reader;
            expenseHeaderShema_ = expenseHeaderShema;
            expenseDateFormats_ = expenseDateFormats;
        }
        public List<Expense> GetAll()
        {
            var res = new List<Expense>();

            var headerChecked = false;//Чтение и проверка заголовка     

            foreach (var cells in reader_.Read())
            {
                if (!headerChecked)
                {
                    if (!cells.SequenceEqual(expenseHeaderShema_))
                    {
                        throw new ArgumentException("CSV cells can't be parsed: Unacceptable header");//заголовок не соответствует схеме
                    }
                    else
                    {
                        headerChecked = true;
                        continue;
                    }
                }

                res.Add(Parse(cells));
            }
            return res;
        }
        /// <summary>
        /// Пыпытка получить Expense из массива ячеек
        /// </summary>
        /// <param name="cells"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Если ячеек не 3</exception>
        protected Expense Parse(string[] cells)
        {
            if(cells is null)
                throw new ArgumentException($"CSV cells can't be parsed, found (0) in line");

            if (cells.Length != 3)
                throw new ArgumentException($"CSV cells can't be parsed, found ({cells.Length}) in line");

            return new Expense(
                decimal.Parse(cells[0]),                              //sum
                                                                      //date
                DateOnly.ParseExact(cells[1], expenseDateFormats_, CultureInfo.InvariantCulture, DateTimeStyles.None),
                cells[2]                                              //category
            );
        }

    }
}
