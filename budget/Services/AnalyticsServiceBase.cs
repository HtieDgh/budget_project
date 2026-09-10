using budget.Models;
using budget.Shared;

namespace budget.Services
{
    public abstract class AnalyticsServiceBase
    {
        protected readonly IWriter writer_;
        
        public AnalyticsServiceBase(IWriter writer)
        {
            writer_ = writer;
        }

        public abstract void Run();
    }
}
