using budget.Services;

namespace budget.Shared
{
    public class Controller
    {
        List<AnalyticsServiceBase>? services_;
        public Controller()
        {
            services_ = null;
        }
        public Controller(List<AnalyticsServiceBase> services)
        {
            addServices(services);
        }
        public void addServices(List<AnalyticsServiceBase> services)
        {
            services_ = services;
        }
        public void addService(AnalyticsServiceBase service)
        {
            if(services_ is null)
                services_ = new List<AnalyticsServiceBase>();
            services_.Add(service);
        }
        public void Run() {
            if (services_ is null)
                return;
            foreach (var service in services_)
            {
                service.Run();
            }
        }
    }
}
