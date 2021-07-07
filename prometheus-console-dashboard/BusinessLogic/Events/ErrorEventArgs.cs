using System;

namespace prometheus_console_dashboard.BusinessLogic.Events
{
    public class ErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Error occured while polling server or parsing metrics
        /// </summary>
        public Exception Exception { get; set; }
    }
}