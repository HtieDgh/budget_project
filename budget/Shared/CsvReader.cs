using System.Text;

namespace budget.Shared
{
    public class CsvReader : CsvReaderBase
    {
        public class CsvConfig
        {
            public char Delimiter { get; private set; }//разделитель
            public char QuotationMark { get; private set; }//Занак ковычки, который позволяет не обрабатывать запятые

            public CsvConfig(char delimiter, char quotationMark)
            {
                Delimiter = delimiter;
                QuotationMark = quotationMark;
            }

            // Конфиг по умолчанию
            public static CsvConfig Default
            {
                get { return new CsvConfig(';', '"'); }
            }
        }

        private CsvConfig m_config;
        //Упрощенные конструкторы
        public CsvReader(string path) : this(new StreamReader(path), CsvConfig.Default)
        { }
        //
        public CsvReader(TextReader reader) : this(reader, CsvConfig.Default)
        { }
        /// <summary>
        /// Конструктор принимает TextReader и CsvConfig
        /// </summary>
        /// <param name="reader">Провайдер входных строк</param>
        /// <param name="config">Описание параметров csv парсера такие как  разделитель, последовательность новой строки и знака ковычки(<see cref="CsvConfig">)</param>
        public CsvReader(TextReader reader, CsvConfig config) : base(reader)
        {
            m_config = config;
        }

        public override IEnumerable<string[]> Read()
        {
            while (true)
            {
                string? line = reader_.ReadLine();
                if (line is null)
                    break;
                yield return ParseLine(line);
            }
            yield break;
        }
       
        /// <summary>
        /// Возвращает массив ячеек из строки
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string[] ParseLine(string line)
        {
            Stack<string> result = new Stack<string>();

            int i = 0;
            while (true)
            {
                string? cell = ParseNextCell(line, ref i);
                if (cell == null)
                    break;
                result.Push(cell);
            }

            // Удалить последние пустые элементы
            string? tmp = null;
            while (result.TryPeek(out tmp) && string.IsNullOrEmpty(result.Peek()))
            {
                result.Pop();
            }

            var resultAsArray = result.ToArray();
            Array.Reverse(resultAsArray);
            return resultAsArray;
        }

        /// <summary>
        /// Проверяет возможность продалжения парсинга, выбирает как парсить след ячейку (с cfg_.QuotationMark или без)
        /// </summary>
        /// <param name="line"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private string? ParseNextCell(string line, ref int i)
        {
            if (i >= line.Length)
                return null;

            if (line[i] != m_config.QuotationMark)
                return ParseNotEscapedCell(line, ref i);
            else
                return ParseEscapedCell(line, ref i);
        }

        /// <summary>
        /// Получить ячейку, которая не экранирована символом cfg_.QuotationMark
        /// </summary>
        /// <param name="line"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private string ParseNotEscapedCell(string line, ref int i)
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                if (i >= line.Length) // return iterator after end of string
                    break;
                if (line[i] == m_config.Delimiter)
                {
                    i++; // return iterator after delimiter
                    break;
                }
                if (line[i] == m_config.QuotationMark)
                {
                    sb.Append(ParseEscapedCell(line, ref i));
                }
                else
                {
                    sb.Append(line[i]);
                    i++;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Возвращает экранированую часть ячейки, не учитывая разделители до закрывающего символа <see cref="CsvConfig.QuotationMark">
        /// </summary>
        private string ParseEscapedCell(string line, ref int i)
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                if (i >= line.Length)
                    break;

                sb.Append(line[i]);
                i++;
                if (line[i] == m_config.QuotationMark)
                {
                    sb.Append(line[i]);
                    i++;
                    if (i >= line.Length)
                    {
                        // Ковычка была последним символом в строке
                        // Просто вернуть итератор
                        break;
                    }
                    if (line[i] == m_config.Delimiter)
                    {
                        // За ковычкой следует разделитель, значит ячейка кончилась
                        // Вернуть итератор после разделителя
                        i++;
                        break;
                    }
                }
            }

            return sb.ToString();
        }

    }
}
