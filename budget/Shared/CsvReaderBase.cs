namespace budget.Shared
{
    /// <summary>
    /// Базовый класс для классов CsvReader
    /// </summary>
    public abstract class CsvReaderBase : IReader
    {
        protected TextReader reader_;
        public abstract IEnumerable<string[]> Read();
        public CsvReaderBase(TextReader reader)
        {
            reader_ = reader;
            if(reader_.Peek() == -1)
            {
                throw new FileNotFoundException("reader is empty");
            }
        }
    }
}
