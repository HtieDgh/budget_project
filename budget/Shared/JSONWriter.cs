using budget.Services;
using System.Text.Json;

namespace budget.Shared
{
    /// <summary>
    /// Запись JSON в файл
    /// </summary>
    public class JSONWriter : IWriter
    {
        class Data
        {
            public ExpenseRateService.Report? ExpenseReport { get; set; }
        }
        private Data data_ = new Data();
        private string filePath_;

        public JSONWriter(string fileName)
        {
            filePath_ = fileName;
        }
        public IWriter AddReport(ExpenseRateService.Report r)
        {
            data_.ExpenseReport = r;
            return this;
        }

        public void DoWrite()
        {
            using (FileStream fs = new FileStream(filePath_, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(fs, data_);
                Console.WriteLine($"Data has been saved to file ({filePath_})");
            }
        }
    }
}
