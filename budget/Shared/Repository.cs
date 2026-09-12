using budget.Models;
using budget.Services;
using System.Diagnostics.CodeAnalysis;
using static budget.Program;

namespace budget.Shared
{
    public class Repository
    {
        public enum ReadFrom
        {
            FILE,
            OCR,
            VOICE
        }
        protected static List<Expense>? expenses_ = null;
        public static List<Expense> GetExpenses( ReadFrom readFrom, string readFromArgument,Config cfg)
        {
            if (expenses_ is null)
            {
                IReader? reader = null;
                switch (readFrom)
                {
                    case ReadFrom.FILE:
                        reader = new CsvReader(readFromArgument);
                        break;
                    case ReadFrom.OCR:
                        reader = new OCRReader(readFromArgument, cfg.OcrConfig.DefaultTessdataDirectory, "rus", "6", cfg.ExpenseConfig.HeaderShema);
                        break;
                    case ReadFrom.VOICE:
                        reader = new VoiceReader(readFromArgument,cfg.ExpenseConfig.HeaderShema);
                        break;
                }
                expenses_ = new ExpenseParser(reader!, cfg.ExpenseConfig.HeaderShema, cfg.ExpenseConfig.DateFormats).GetAll();
            }
            return expenses_;
        }
    }
}
