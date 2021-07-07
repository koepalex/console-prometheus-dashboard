using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using prometheus_console_dashboard.Model;

namespace prometheus_console_dashboard.BusinessLogic
{
    public class MetricsParser
    {
        private const string SpecialLineType = "TYPE";
        private const string SpecialLineHelp = "HELP";
        private const string SpecialMetricElementSum = "_sum";
        private const string SpecialMetricElementCount = "_count";
        private const string SpecialMetricElementBucket = "_bucket";

        /// <Summary>
        /// generates list of metric objects based on input
        /// </Summary>
        public static IEnumerable<Metric> Parse(string message)
        {
            var metrics = new List<Metric>(50);
            Metric lastMetric = null;
            foreach(var line in message.Split(Environment.NewLine))
            {
                var lineAsSpan = line.AsSpan();
                if (lineAsSpan.IsEmpty())
                {
                    continue;
                }

                if (lineAsSpan.IsSpecial(SpecialLineHelp))
                {
                    var name = lineAsSpan.GetNameOfSpecialLine(SpecialLineHelp);
                    var helpText = lineAsSpan.GetPayloadOfSpecialLine();

                    if (name != lastMetric?.Identifier)
                    {
                        lastMetric = new Metric
                        {
                            Identifier = name,
                            Type = MetricsType.None,
                        };

                        metrics.Add(lastMetric);
                    }
                    lastMetric.Help = helpText;
                } 
                else if(lineAsSpan.IsSpecial(SpecialLineType))
                {
                    var name = lineAsSpan.GetNameOfSpecialLine(SpecialLineType);
                    var typeStr = lineAsSpan.GetPayloadOfSpecialLine();

                    var type = Enum.Parse<MetricsType>(typeStr, true);

                    if (name != lastMetric?.Identifier)
                    {
                        lastMetric = new Metric
                        {
                            Identifier = name,
                            Type = MetricsType.None,
                        };

                        metrics.Add(lastMetric);
                    }

                    lastMetric.Type = type;
                }
                else
                {
                    (var name, var value, var tags) = lineAsSpan.GetValueLine();

                    if (name != lastMetric?.Identifier)
                    {
                        var nameAsSpan = name.AsSpan();
                        if (lastMetric != null && nameAsSpan.IsKnownMetricElement(lastMetric.Identifier, SpecialMetricElementSum))
                        {
                            lastMetric.Sum = GetDoubleValue(value);
                            continue;
                        }
                        else if (lastMetric != null && nameAsSpan.IsKnownMetricElement(lastMetric.Identifier, SpecialMetricElementCount))
                        {
                            lastMetric.Count = GetDoubleValue(value);
                            continue;
                        }
                        else if (lastMetric != null && nameAsSpan.IsKnownMetricElement(lastMetric.Identifier, SpecialMetricElementBucket))
                        {
                            if (lastMetric.Value == null)
                            {
                                lastMetric.Value = new Dictionary<string, long>();
                            }
                        }
                        else
                        {
                            lastMetric = new Metric
                            {
                                Identifier = name,
                                Type = MetricsType.None,
                            };

                            metrics.Add(lastMetric);
                        }
                    }
                    else if (lastMetric.Tags.Count > 0)
                    {
                        if(lastMetric.Type != MetricsType.Histogram)
                        {
                            lastMetric = new Metric(lastMetric);
                            metrics.Add(lastMetric);
                        }
                    }

                    lastMetric.Tags = tags;

                    switch (lastMetric.Type)
                    {
                        case MetricsType.None:
                        case MetricsType.Gauge:
                        case MetricsType.Counter:
                            lastMetric.Value = GetDoubleValue(value);
                            break;
                        case MetricsType.Histogram:
                            // if (lastMetric.Value == null)
                            // {
                            //     lastMetric.Value = new Dictionary<string, long>();
                            // }
                            if (lastMetric.Tags.ContainsKey("le"))
                            {
                                var valueAsLong = GetLongValue(value);
                                var added = ((IDictionary<string, long>)lastMetric.Value)?
                                    .TryAdd(lastMetric.Tags["le"], valueAsLong) ?? false;
                                if (!added)
                                {
                                    throw new InvalidProgramException($"Could not add element to histogram, key: {lastMetric.Tags["le"]}, value: {valueAsLong}");
                                }
                            }
                            else
                            {
                                throw new InvalidProgramException($"Found histogramm without without \"le\" tag");
                            }
                            break;
                    }
                }
            }
            return metrics;
        }

        private static double GetDoubleValue(string value)
        {
            var style = NumberStyles.Number | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;
            var culture = CultureInfo.CreateSpecificCulture("en-GB");
            if (!double.TryParse(value, style, culture, out var valueAsDouble))
            {
                throw new InvalidProgramException($"Unexpected type of value {value}");
            }

            return valueAsDouble;
        }

        private static long GetLongValue(string value)
        {
            var style = NumberStyles.Number | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;
            var culture = CultureInfo.CreateSpecificCulture("en-GB");
            if (!long.TryParse(value, style, culture, out var valueAsLong))
            {
                throw new InvalidProgramException($"Unexpected type of value {value}");
            }

            return valueAsLong;
        }
    }
}