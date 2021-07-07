using System;

namespace prometheus_console_dashboard.ViewModel
{
    public class PrometheusViewModel
    {
        public int PollingInterval { get; set; } = -1;
		public  Uri PrometheusServer { get; set; }
    }
}