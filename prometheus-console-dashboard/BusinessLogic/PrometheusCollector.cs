using System;
using System.Net.Http;
using System.Threading;
using prometheus_console_dashboard.BusinessLogic.Events;

namespace prometheus_console_dashboard.BusinessLogic
{
    internal sealed class PrometheusCollector : IDisposable
    {
        private readonly Uri _prometheusServer;
        private readonly int _pollingIntervalAsSecond;
        private readonly Timer _pollingTimer;

        public PrometheusCollector(Uri prometheusServer, int pollingInterval)
        {
            if(prometheusServer == null)
            {
                throw new ArgumentNullException(nameof(prometheusServer));
            }

            if(pollingInterval <= 0)
            {
                throw new ArgumentException("Polling interval need to be positive", nameof(pollingInterval));
            }

            _prometheusServer = prometheusServer;
            _pollingTimer = new Timer(OnTimerElappsed, null, 0, _pollingIntervalAsSecond * 1000);
        }

        /// <summary>
        /// Event that is triggered when new value is available
        /// </summary>
        public event EventHandler<PrometheusEventArgs> MetricsChanged;
        
        /// <summary>
        /// Event that is triggered when any error araises
        /// </summary>
        public event EventHandler<ErrorEventArgs> ErrorOccured;

        /// <inheritdoc />
        public void Dispose()
        {
            _pollingTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _pollingTimer.Dispose();
        }

        private void OnTimerElappsed(object state)
        {
            using var client = new HttpClient();
            try
            {
                var response = client.GetAsync(_prometheusServer).ConfigureAwait(true).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                var payload = response.Content.ReadAsStringAsync().ConfigureAwait(true).GetAwaiter().GetResult();
                var metrics = MetricsParser.Parse(payload);
                
                MetricsChanged?.Invoke(this, new PrometheusEventArgs{
                    Metrics = metrics
                });
            }
            catch (Exception ex)
            {
                ErrorOccured?.Invoke(this, new ErrorEventArgs{
                    Exception = ex
                });
            }
        }
    }
}