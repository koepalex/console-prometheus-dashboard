using System;
using Terminal.Gui;
using NStack;
using prometheus_console_dashboard.BusinessLogic;
using prometheus_console_dashboard.BusinessLogic.Events;
using prometheus_console_dashboard.ViewModel;
using System.Linq;
using System.Collections.Generic;

namespace prometheus_console_dashboard
{
    class Program
    {
		private const int DefaultPollingInterval = 5;
		private const string DefaultPrometheusServer = "http://localhost:9010";
		private static PrometheusCollector _collector; 
		private static PrometheusViewModel _viewModel = new PrometheusViewModel();
        private static FrameView _metricFrame;
        static void Main(string[] args)
        {
			Application.Init();
			var top = Application.Top;

			// Creates the top-level window to show
			var win = new Window("Console Prometheus Metric Dashboard")
			{
				X = 0,
				Y = 1, // Leave one row for the toplevel menu

				// By using Dim.Fill(), it will automatically resize without manual intervention
				Width = Dim.Fill(),
				Height = Dim.Fill()
			};

			top.Add(win);

			// Creates a menubar, the item "New" has a help menu.
			var menu = new MenuBar(new MenuBarItem[] {
						new MenuBarItem ("_Actions", new MenuItem [] {
							new MenuItem ("_Connect", "Start polling metrics", () => OnConnect()),
							new MenuItem ("_Disconnect", "Stop polling metrics", () => OnDisconnect()),
							new MenuItem ("_Quit", "Exit the program", () => { top.Running = false; })
						})
					});
			top.Add(menu);

			var prometheusLogin = new Label("Prometheus Server: ") { X = 3, Y = 2 };
			var refresh = new Label("Refresh Interval: ")
			{
				X = Pos.Left(prometheusLogin),
				Y = Pos.Top(prometheusLogin) + 1
			};
			var loginText = new TextField(DefaultPrometheusServer)
			{
				X = Pos.Right(prometheusLogin),
				Y = Pos.Top(prometheusLogin),
				Width = 40
			};
			loginText.TextChanged += (newServer) => {
				if(Uri.TryCreate(newServer.ToString(), UriKind.Absolute, out var serverUri))
				{
					_viewModel.PrometheusServer = serverUri;
				}
				else
				{
					var n = MessageBox.ErrorQuery(50, 7, "Invalid Input", "Please enter valid prometheus server URL", "OK");
				}
			};

			var refreshIntervalText = new TextField(DefaultPollingInterval.ToString())
			{
				X = Pos.Left(loginText),
				Y = Pos.Top(loginText) + 1,
				Width = Dim.Width(loginText)
			};
			refreshIntervalText.TextChanged += (newRefreshInterval) => {
				if(int.TryParse(newRefreshInterval.ToString(), out int pollingInterval))
				{
					_viewModel.PollingInterval = pollingInterval;
				}
				else
				{
					var n = MessageBox.ErrorQuery(50, 7, "Invalid Input", "Please enter valid integer value", "OK");
				}
			};

			refreshIntervalText.GetCurrentWidth(out var textBoxWidth);
			var seconds = new Label("seconds")
			{
				X = Pos.Left(refreshIntervalText) + textBoxWidth + 1,
				Y = Pos.Top(refreshIntervalText) 
			};

			var connectButton = new Button("Connect")
			{
				X = Pos.Left(refreshIntervalText),
				Y = Pos.Bottom(refreshIntervalText) + 1,
			};
			connectButton.Clicked += () => {
				OnConnect();
			};

			connectButton.GetCurrentWidth(out var connectButtonWidth);
			var disconnectButton = new Button("Disconnect")
			{
				X = Pos.Left(refreshIntervalText) + connectButtonWidth + 1,
				Y = Pos.Bottom(refreshIntervalText) + 1,	
			};
			disconnectButton.Clicked += () => {
				OnDisconnect();
			};

            _metricFrame = new FrameView("Metrics")
            {
                X = Pos.Left(refresh),
				Y = Pos.Bottom(connectButton) + 1,
                Width = Dim.Fill(1),
                Height = Dim.Fill(1)
            };

			// Add some controls, 
			win.Add(
				// The ones with my favorite layout system, Computed
				prometheusLogin, 
				refresh, 
				loginText, 
				refreshIntervalText, 
				seconds, 
				connectButton,
				disconnectButton,
                _metricFrame
				// The ones laid out like an australopithecus, with Absolute positions:
				//new CheckBox(3, 6, "Remember me"),
				//new RadioGroup(3, 8, new ustring[] { "_Personal", "_Company" }, 0),
				
				// new Button(14, 14, "Disconnect")
				//new Label(3, 18, "Press F9 or ESC plus 9 to activate the menubar")
			);

			Application.Run();
        }

		private static void OnConnect()
		{
			_collector = new PrometheusCollector(_viewModel.PrometheusServer, _viewModel.PollingInterval);
			_collector.MetricsChanged += OnMetricsChanged;
			_collector.ErrorOccured += OnErrorOccured;
		}

		private static void OnDisconnect()
		{
			if (_collector != null)
			{
				_collector.MetricsChanged -= OnMetricsChanged;
				_collector.ErrorOccured -= OnErrorOccured;
				_collector.Dispose();
				_collector = null;
			}
		}

        private static void OnErrorOccured(object sender, ErrorEventArgs e)
        {
            var n = MessageBox.ErrorQuery(50, 7, "Error Occured", e.Exception.Message + Environment.NewLine + e.Exception.StackTrace, "OK");
        }

        private static void OnMetricsChanged(object sender, PrometheusEventArgs e)
        {
            _metricFrame.Clear();

            //todo finish up
            var text = new List<string> { "Identifier \t\t Type \t\t Value \t\t Tags"};
            text.AddRange(e.Metrics.Select(m => $"{m.Identifier} \t\t {m.Type} \t\t {m.Value} \t\t {m.Tags}"));
            _metricFrame.Add(new ListView(text));
        }
    }
}