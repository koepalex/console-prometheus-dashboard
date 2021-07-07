using System;
using System.Collections.Generic;
using System.Linq;
using prometheus_console_dashboard.BusinessLogic;
using prometheus_console_dashboard.Model;
using Xunit;

namespace prometheus_console_dashboard_test
{
    public class PrometheusFormatTests
    {
        private const string dotnet_memory_name = "dotnet_memory";
        private const string dotnet_memory_help = "This is a help text";
        private const MetricsType dotnet_memory_type = MetricsType.Gauge;
        private const string prom_warning_name = "prom_warning";
        private const MetricsType prom_warning_type = MetricsType.Counter;
        private const double prom_warning_valueLong = 450;
        private const double prom_warning_valueFloat = 7.99;
        private const string prom_errors_name = "prom_errors";
        private const MetricsType prom_errors_type = MetricsType.Histogram;
        private const double prom_errors_valueLong = 12;
        private const double prom_errors_valueFloat = 3.14;
        private const string dotnet_colection_name = "dotnet_collection_count_total";
        private const string tag_generation = "generation";

        [Fact]
        public void Test_MetricWithHelp_Expect_HelpMessage()
        {
            const string input = "# HELP dotnet_memory This is a help text";

            var metrics = MetricsParser.Parse(input);
            Assert.NotEmpty(metrics);
            var metric = metrics.First();
            Assert.Equal(dotnet_memory_name, metric.Identifier);
            Assert.Equal(dotnet_memory_help, metric.Help);
            Assert.Equal(MetricsType.None, metric.Type);
        }

        [Theory]
        [InlineData("# TYPE dotnet_memory gauge", dotnet_memory_name, dotnet_memory_type)]
        [InlineData("# TYPE prom_warning counter", prom_warning_name, prom_warning_type)]
        [InlineData("# TYPE prom_errors histogram", prom_errors_name, prom_errors_type)]
        public void Test_MetricWithType_Expect_CorrectType(string input, string name, MetricsType type)
        {
            var metrics = MetricsParser.Parse(input);
            Assert.NotEmpty(metrics);
            var metric = metrics.First();
            Assert.Equal(name, metric.Identifier);
            Assert.Null(metric.Help);
            Assert.Equal(type, metric.Type);
        }

        //metric value simple long
        //metric value simple floating point
        [Theory]
        [InlineData("prom_warning 450", prom_warning_name, prom_warning_valueLong)]
        [InlineData("prom_warning 7.99", prom_warning_name, prom_warning_valueFloat)]
        [InlineData("prom_errors 12", prom_errors_name, prom_errors_valueLong)]
        [InlineData("prom_errors 3.14", prom_errors_name, prom_errors_valueFloat)]
        [InlineData("prom_errors 1.112293682e+09", prom_errors_name, 1112293682.0)]
        public void Test_SimpleValueLine(string input, string name, double value)
        {
            var metrics = MetricsParser.Parse(input);
            Assert.NotEmpty(metrics);
            var metric = metrics.First();
            Assert.Equal(name, metric.Identifier);
            Assert.Null(metric.Help);
            Assert.Equal(value, metric.GetTypedValue<double>());
        }

        //metric value with one tag
        [Theory]
        [InlineData("dotnet_collection_count_total{generation=\"0\"} 0", dotnet_colection_name, tag_generation, "0", 0)]
        [InlineData("dotnet_collection_count_total{generation=\"1\"} 0", dotnet_colection_name, tag_generation, "1", 0)]
        [InlineData("dotnet_collection_count_total{generation=\"2\"} 0", dotnet_colection_name, tag_generation, "2", 0)]
        public void Test_SimpleValueWithTag(string input, string name, string tagName, string tagValue, double value)
        {
            var metrics = MetricsParser.Parse(input);
            Assert.NotEmpty(metrics);
            var metric = metrics.First();
            Assert.Equal(name, metric.Identifier);
            Assert.Null(metric.Help);
            var tags = metric.Tags;
            Assert.NotEmpty(tags);
            var tag = tags.First();
            Assert.Equal(tagName, tag.Key);
            Assert.Equal(tagValue, tag.Value);
            Assert.Equal(value, metric.GetTypedValue<double>());
        }

        //metric value with multiple tags
        [Theory]
        [InlineData("api_http_requests_total{method=\"POST\", handler=\"/messages\"} 0", new [] {"method", "handler"}, new [] {"POST", "/messages"})]
        [InlineData("metric{t1=\"v1\",t2=\"v2\", t3 = \"v3\"} 0", new [] {"t1", "t2", "t3"}, new [] {"v1", "v2", "v3"})]
        public void Test_SimpleValueWithMultipleTags(string input, string[] tags, string[] values)
        {
            var metrics = MetricsParser.Parse(input);
            Assert.NotEmpty(metrics);
            var metric = metrics.First();

            int index = 0;
            foreach(var kvp in metric.Tags)
            {
                Assert.Equal(tags[index], kvp.Key);
                Assert.Equal(values[index], kvp.Value);
                ++index;
            }
        }

        [Fact]
        public void Test_FullGaugeMetric()
        {
            const string input = "# HELP my_metric 42\r\n" 
            + "# TYPE my_metric gauge\r\n"
            + "my_metric 3.14\r\n";

            var metrics = MetricsParser.Parse(input);
            Assert.NotEmpty(metrics);
            var metric = metrics.First();

            Assert.Equal("my_metric", metric.Identifier);
            Assert.Equal("42", metric.Help);
            Assert.Equal(MetricsType.Gauge, metric.Type);
            Assert.Equal(3.14, metric.GetTypedValue<double>());
        }

        [Fact]
        public void Test_FullCounterMetric()
        {
            string input = $"# HELP my_new_metric a b c d{Environment.NewLine}" 
            + $"# TYPE my_new_metric counter{Environment.NewLine}"
            + $"my_new_metric 444{Environment.NewLine}";

            var metrics = MetricsParser.Parse(input);
            Assert.NotEmpty(metrics);
            var metric = metrics.First();

            Assert.Equal("my_new_metric", metric.Identifier);
            Assert.Equal("a b c d", metric.Help);
            Assert.Equal(MetricsType.Counter, metric.Type);
            Assert.Equal(444, metric.GetTypedValue<double>());
        }

        [Fact]
        public void Test_MultipleMetricsDistingedByTags()
        {
            string input = $"# HELP gc_gen gc gen{Environment.NewLine}"
            + $"# TYPE gc_gen counter{Environment.NewLine}" 
            + $"gc_gen{{g=\"0\"}} 10000{Environment.NewLine}"
            + $"gc_gen{{g=\"1\"}} 5000{Environment.NewLine}"
            + $"gc_gen{{g=\"2\"}} 400{Environment.NewLine}";

            var metrics = MetricsParser.Parse(input);
            Assert.NotEmpty(metrics);
            Assert.Equal(3, metrics.Count());

            var metricOne = metrics.First();
            var metricTwo = metrics.Skip(1).First();
            var metricThree = metrics.Skip(2).First();

            var expectedIdentifier = "gc_gen";
            Assert.Equal(expectedIdentifier, metricOne.Identifier);
            Assert.Equal(expectedIdentifier, metricTwo.Identifier);
            Assert.Equal(expectedIdentifier, metricThree.Identifier);

            var expectedHelp = "gc gen";
            Assert.Equal(expectedHelp, metricOne.Help);
            Assert.Equal(expectedHelp, metricTwo.Help);
            Assert.Equal(expectedHelp, metricThree.Help);

            Assert.Equal(MetricsType.Counter, metricOne.Type);
            Assert.Equal(MetricsType.Counter, metricTwo.Type);
            Assert.Equal(MetricsType.Counter, metricThree.Type);

            Assert.Equal(10000, metricOne.GetTypedValue<double>());
            Assert.Equal(5000, metricTwo.GetTypedValue<double>());
            Assert.Equal(400, metricThree.GetTypedValue<double>());
        }

        [Fact]
        public void Test_MetricWithSum()
        {
            string input = $"# HELP prom_warning very helpful{Environment.NewLine}"
            + $"# TYPE prom_warning counter{Environment.NewLine}"
            + $"prom_warning 1{Environment.NewLine}"
            + $"prom_warning_sum 1.6238{Environment.NewLine}";

            var metrics = MetricsParser.Parse(input);
            Assert.NotEmpty(metrics);
            var metric = metrics.First();

            Assert.Equal(MetricsType.Counter, metric.Type);
            Assert.Equal(1, metric.GetTypedValue<double>());
            Assert.Equal(1.6238, metric.Sum);
        }

        [Fact]
        public void Test_MetricWithCount()
        {
            string input = $"# HELP prom_warning very helpful{Environment.NewLine}"
            + $"# TYPE prom_warning counter{Environment.NewLine}"
            + $"prom_warning 1{Environment.NewLine}"
            + $"prom_warning_count 1.11{Environment.NewLine}";

            var metrics = MetricsParser.Parse(input);
            Assert.NotEmpty(metrics);
            var metric = metrics.First();

            Assert.Equal(MetricsType.Counter, metric.Type);
            Assert.Equal(1, metric.GetTypedValue<double>());
            Assert.Equal(1.11, metric.Count);
        }

        [Fact]
        public void Test_MetricWithSumAndCount()
        {
            string input = $"# HELP prom_warning very helpful{Environment.NewLine}"
            + $"# TYPE prom_warning counter{Environment.NewLine}"
            + $"prom_warning 1{Environment.NewLine}"
            + $"prom_warning_sum 1.23456789{Environment.NewLine}"
            + $"prom_warning_count 1.11{Environment.NewLine}";

            var metrics = MetricsParser.Parse(input);
            Assert.NotEmpty(metrics);
            var metric = metrics.First();

            Assert.Equal(MetricsType.Counter, metric.Type);
            Assert.Equal(1, metric.GetTypedValue<double>());
            Assert.Equal(1.11, metric.Count);
            Assert.Equal(1.23456789, metric.Sum);
        }
        
        [Fact]
        public void Test_Historgramm()
        {
            string input = $"# HELP prometheus_http_request_duration_seconds Histogram of latencies for HTTP requests.{Environment.NewLine}"
            + $"# TYPE prometheus_http_request_duration_seconds histogram{Environment.NewLine}"
            + $"prometheus_http_request_duration_seconds_bucket{{handler=\"/\",le=\"0.1\"}} 25547{Environment.NewLine}"
            + $"prometheus_http_request_duration_seconds_bucket{{handler=\"/\",le=\"0.2\"}} 26688{Environment.NewLine}"
            + $"prometheus_http_request_duration_seconds_bucket{{handler=\"/\",le=\"0.4\"}} 27760{Environment.NewLine}"
            + $"prometheus_http_request_duration_seconds_bucket{{handler=\"/\",le=\"1\"}} 28641{Environment.NewLine}"
            + $"prometheus_http_request_duration_seconds_bucket{{handler=\"/\",le=\"3\"}} 28782{Environment.NewLine}"
            + $"prometheus_http_request_duration_seconds_bucket{{handler=\"/\",le=\"8\"}} 28844{Environment.NewLine}"
            + $"prometheus_http_request_duration_seconds_bucket{{handler=\"/\",le=\"20\"}} 28855{Environment.NewLine}"
            + $"prometheus_http_request_duration_seconds_bucket{{handler=\"/\",le=\"60\"}} 28860{Environment.NewLine}"
            + $"prometheus_http_request_duration_seconds_bucket{{handler=\"/\",le=\"120\"}} 28860{Environment.NewLine}"
            + $"prometheus_http_request_duration_seconds_bucket{{handler=\"/\",le=\"+Inf\"}} 28860{Environment.NewLine}"
            + $"prometheus_http_request_duration_seconds_sum{{handler=\"/\"}} 1863.80491025699{Environment.NewLine}"
            + $"prometheus_http_request_duration_seconds_count{{handler=\"/\"}} 28860{Environment.NewLine}";

            var metrics = MetricsParser.Parse(input);
            Assert.NotEmpty(metrics);
            Assert.Equal(1, metrics.Count());
            var metric = metrics.First();

            Assert.Equal(MetricsType.Histogram, metric.Type);
            var values = metric.GetTypedValue<IDictionary<string, long>>();
            Assert.NotNull(values);
            Assert.Equal(10, values.Count());
            Assert.Equal(1863.80491025699, metric.Sum);
            Assert.Equal(28860, metric.Count);
            Assert.Equal(25547, values["0.1"]);
            Assert.Equal(26688, values["0.2"]);
            Assert.Equal(27760, values["0.4"]);
            Assert.Equal(28641, values["1"]);
            Assert.Equal(28782, values["3"]);
            Assert.Equal(28844, values["8"]);
            Assert.Equal(28855, values["20"]);
            Assert.Equal(28860, values["60"]);
            Assert.Equal(28860, values["120"]);
            Assert.Equal(28860, values["+Inf"]);
            Assert.Equal(metric.Count, (double)values.Last().Value);
        }
    }
}
