using System;
using System.Collections.Generic;
using prometheus_console_dashboard.Model;

namespace prometheus_console_dashboard.BusinessLogic.Events
{
    /// <summary>
    /// Event args for event that is triggered when new value is available
    /// </summary>
    public class PrometheusEventArgs : EventArgs
    {
        /// <summary>
        /// All Metrics that have been received
        /// </summary>
        public IEnumerable<Metric> Metrics { get; set; }
    }
}