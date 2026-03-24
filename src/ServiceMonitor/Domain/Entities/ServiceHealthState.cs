using Ardalis.GuardClauses;

namespace ServiceMonitor.Domain.Entities
{
    public class ServiceHealthState
    {
        public Uri Url { get; }
        public bool IsHealthy { get; private set; }
        public bool AlertSent { get; private set; }
        public DateTime LastChange { get; private set; }

        public ServiceHealthState(Uri url, bool isHealthy)
        {
            Guard.Against.Null(url);

            Url = url;
            IsHealthy = isHealthy;
            LastChange = DateTime.UtcNow;
            AlertSent = false;
        }

        public void Update(bool isHealthy)
        {
            if (IsHealthy != isHealthy)
            {
                LastChange = DateTime.UtcNow;
                AlertSent = false; // Reset bei Statuswechsel
            }

            IsHealthy = isHealthy;
        }

        public void MarkAlertSent()
        {
            AlertSent = true;
        }
    }

}
