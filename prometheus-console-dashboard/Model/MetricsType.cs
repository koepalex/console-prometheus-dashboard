namespace prometheus_console_dashboard.Model
{
    public enum MetricsType
    {
        /// <summary>Not defined type</summary>
        None = 0,

        /// <summary>Metric of type Gauge</summary>
        Gauge = 1,

        /// <summary>Metric of type Counter</summary>
        Counter = 2,

        /// <summary>Metric of type Histogram</summary>
        Histogram = 3,
    }
}