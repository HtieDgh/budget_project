namespace budget.Shared
{
    /// <summary>
    /// Пердставление конфигов Expense
    /// </summary>
    public class ExpenseConfig
    {
        public required string Path { get; set; }
        public required string[] HeaderShema { get; set; }
        public required string[] DateFormats { get; set; }
    }
    /// <summary>
    /// Пердставление конфигов: Options
    /// </summary>
    public class OptionsConfig
    {
        public required string[] DateFormats { get; set; }
    }
    public class Config
    {
        public ExpenseConfig expenseConfig { get; set; }
        public OptionsConfig optionsConfig { get; set; }
        public Config(ExpenseConfig expenseConfig, OptionsConfig optionsConfig)
        {
            this.expenseConfig = expenseConfig;
            this.optionsConfig = optionsConfig;
        }
    }
}
