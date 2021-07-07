using System.Collections.Generic;

namespace prometheus_console_dashboard.Model
{
    public class Metric
    {
        public Metric()
        {

        }

        public Metric(Metric other)
        {
            Identifier = other.Identifier;
            Help = other.Help;
            Type = other.Type;
        }

        /// <Summary>
        /// The Name for the metric
        /// </Summary>
        public string Identifier { get; set; }
        
        /// <Summary>
        /// The Help Text for the metric
        /// </Summary>
        public string Help { get; set; }
        
        /// <Summary>
        /// The kind of metric
        /// </Summary>
        public MetricsType Type { get; set; }

        /// <Summary>
        /// The Value of the metric
        /// </Summary>
        public object Value { get; set; }

        /// <Summary>
        /// The '_sum' element of the metric 
        /// </Summary>
        public double Sum  { get; set; }

        /// <Summary>
        /// The '_count' element of the metric 
        /// </Summary>
        public double Count { get; set; }

        /// <Summary>
        /// The Value of the metric
        /// </Summary>
        public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

        /// <Summary>
        /// Access the value of metric with type
        /// </Summary>
        public T GetTypedValue<T>() 
        {
            return (T) Value;
        }
    }
}