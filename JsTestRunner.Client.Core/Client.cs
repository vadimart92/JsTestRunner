using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using JsTestRunner.Core.Interfaces;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json.Linq;
using System.Reactive;

namespace JsTestRunner.Client.Core
{
	public enum LoggingLevel
	{
		Min = 1,
		Full = 2
	}

	public class Client
	{
		readonly string _url;
		private IHubProxy<ITestRunnerBroker, ITestRunnerClientContract> _hubProxy;
		private readonly Action<string, bool?> _logger;
		private readonly Dictionary<LoggingLevel, List<string>> _loggingLevelMap;
		public LoggingLevel CurrentLoggingLevel {
			get;
			private set;
		}

		public Client(string ulr, Action<string, bool?> logger) {
			_url = ulr;
			_logger = logger;
			CurrentLoggingLevel = LoggingLevel.Min;
		}

		public void Connect(TimeSpan timeout) {
			var hubConnection = new HubConnection(_url);
			_hubProxy = hubConnection.CreateHubProxy<ITestRunnerBroker, ITestRunnerClientContract>("TestRunnerBroker");
			InitSubscriptions();
			ServicePointManager.DefaultConnectionLimit = 10;
			hubConnection.Start().Wait(timeout);
			_hubProxy.Call(h => h.JoinAsClient());
		}
		volatile int RunState = -1;
		private void InitSubscriptions() {
			_hubProxy.SubscribeOn<string, string, int, string, JObject>(h => h.TestEvent, (runner, eventName, state, text, payload) => {
				if (!CheckLogTestEvent(eventName, state)) {
					return;
				}
				bool? stateRes = null;
				if (state != 2) {
					stateRes = state == 1;
				}
				_logger(string.Format("Runner: {2}{1}Event: {0}", eventName, Environment.NewLine, runner), stateRes);
				if (text != null) {
					_logger(text, stateRes);
				}
				if (eventName == "testsuiteend") {
					lock (this) {
						RunState = 1;
					}
				}
			});
			_hubProxy.SubscribeOn<string, string>(h => h.AppendLog, (runner, log) => {
				_logger(string.Format("Runner: {2}{1}{0}", log, Environment.NewLine, runner), null);
			});
		}

		private bool CheckLogTestEvent(string name, int state) {
			return true;
		}

		public Task RunTest(string name) {
			return Task.Run(() => {
				lock (this) {
					RunState = 0;
				}
				_hubProxy.Call(broker => broker.RunTest(name));
				while (true) {
					lock (this) {
						if (RunState == 1) {
							return;
						}
					}
				}
			});
		}

		public void ReloadPage() {
			_hubProxy.Call(broker => broker.ReloadPage(true));
		}

		public void Ping() {
			_hubProxy.Call(h => h.Ping());
		}
	}
}
