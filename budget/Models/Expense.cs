namespace budget.Models
{
    /// <summary>
    /// DTO для данных о расходах
    /// </summary>
    public record Expense(
        decimal Sum,
        DateOnly Date,
        string Category
    );

}