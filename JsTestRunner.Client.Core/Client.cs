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
		Min = -1,
		Default = 0,
		Verbose = 1,
		Full = 2
	}

    public class Client {
	    readonly string _url;
	    private IHubProxy<ITestRunnerBroker, ITestRunnerClientContract> _hubProxy;
	    private readonly Action<string> _logger;
		private readonly Dictionary<LoggingLevel, List<string>> _loggingLevelMap; 
		private LoggingLevel _currentLoggingLevel = LoggingLevel.Default;

        public Client(string ulr, Action<string> logger) {
		    _url = ulr;
	        _logger = logger;
			_loggingLevelMap = new Dictionary<LoggingLevel, List<string>> {
				{LoggingLevel.Min, new List<string> { "testsuitestart", "testsuiteend" } }, 
				{LoggingLevel.Default, new List<string> { "testsuitestart", "testsuiteend", "testfailedwithexception" } },
				{LoggingLevel.Verbose, new List<string> { "testsuitestart", "testsuiteend", "testfailedwithexception", "teststart", "testfinalize" } },
				{LoggingLevel.Full, new List<string> { "testsuitestart", "testsuiteend", "testfailedwithexception", "teststart", "testfinalize", "testupdate" } }
			};
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
			_hubProxy.SubscribeOn<string, string, string>(h=>h.TestEvent, (runner, eventName, text) => {
				if (!CheckLogTestEvent(eventName)) {
					return;
				}
				_logger(string.Format("Runner: {2}{1}Event: {0}", eventName, Environment.NewLine, runner));
				if (text != null) {
					_logger(text);
				}
			});
			_hubProxy.SubscribeOn<string,string>(h=>h.AppendLog, (runner, log) => {
				_logger(string.Format("Runner: {2}{1}{0}", log, Environment.NewLine, runner));
			});
	    }

	    private bool CheckLogTestEvent(string name) {
		    var currentLoggingEvents = _loggingLevelMap[_currentLoggingLevel];
		    return currentLoggingEvents.Contains(name, StringComparer.OrdinalIgnoreCase);
	    }

	    public void RunTest(string name, LoggingLevel level = LoggingLevel.Default) {
		    _hubProxy.Call(broker => broker.RunTest(name));
	    }

	    public void ReloadPage() {
			_hubProxy.Call(broker => broker.ReloadPage(true));
	    }
    }
}
