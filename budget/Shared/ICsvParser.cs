using budget.Models;
using Microsoft.VisualBasic;
namespace budget.Shared
{
    public interface ICsvParser<TOut>
    {
        /// <summary>
        /// Возвращает список распаршеных элементов
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public List<TOut> GetAll();
    }
}