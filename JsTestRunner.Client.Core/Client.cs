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

    public class Client {
	    readonly string _url;
	    private IHubProxy<ITestRunnerBroker, ITestRunnerClientContract> _hubProxy;
	    private readonly Action<string, bool?> _logger;

	    public Client(string ulr, Action<string, bool?> logger) {
		    _url = ulr;
	        _logger = logger;
        }

	    public void Init() {
			var hubConnection = new HubConnection(_url);
			_hubProxy = hubConnection.CreateHubProxy<ITestRunnerBroker, ITestRunnerClientContract>("TestRunnerBroker");
			InitSubscriptions();
			ServicePointManager.DefaultConnectionLimit = 10;
		    hubConnection.Start().Wait();
		    _hubProxy.Call(h => h.JoinAsClient());
	    }
		
	    private void InitSubscriptions() {
			_hubProxy.SubscribeOn<string, string, int, string, JObject>(h => h.TestEvent, OnTestEventAccepted);
			_hubProxy.SubscribeOn<string, string>(h => h.AppendLog, (runner, log) => {
				_logger(string.Format("Runner: {2}{1}{0}", log, Environment.NewLine, runner), null);
			});
			_hubProxy.SubscribeOn<string, RunnerState>(h => h.SendRunnerState, (runnerInfo, state) => {
				_lastRunnerState = state;
				//todo: set state corectly
			});
		}

	    private volatile RunnerState _lastRunnerState = RunnerState.Waiting;

		volatile int _runState = -1;
		private void OnTestEventAccepted(string runner, string eventName, int state, string text, JObject payload) {
			bool? stateRes = null;
			if (state != 2) {
				stateRes = (state == 1);
			}
			_logger(string.Format("Runner: {2}{1}Event: {0}", eventName, Environment.NewLine, runner), stateRes);
			if (text != null) {
				_logger(text, stateRes);
			}
			if (eventName == "testsuiteend") {
				lock (this) {
					_runState = 0;
				}
			}
		}

		public Task RunTest(string name) {
			return Task.Run(() => {
				lock (this) {
					_runState = 1;
				}
				_hubProxy.Call(broker => broker.RunTest(name));
				while (true) {
					lock (this) {
						if (_runState == 0) {
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
