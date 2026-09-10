namespace budget.Shared
{
    public interface IReader
    {
        public abstract IEnumerable<string[]> Read();
    }
}
