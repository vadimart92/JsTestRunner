using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json.Linq;

namespace JsTestRunner.Client.Core
{
    public class Client {
		string _url;
	    private IHubProxy _hubProxy;
	    private Action<string> _logger;
        public Client(string ulr, Action<string> logger) {
		    _url = ulr;
	        _logger = logger;
        }

	    public void Init() {
			var hubConnection = new HubConnection(_url);
			_hubProxy = hubConnection.CreateHubProxy("TestRunnerBroker");
			_hubProxy.On("TestEvent", (string eventName, JObject config) => {
				_logger(string.Format("Event: {0}", eventName));
				var txt = config["text"];
				if (txt != null) {
					_logger(txt.ToString());
				}
			});
			ServicePointManager.DefaultConnectionLimit = 10;
		    hubConnection.Start().Wait();
			_hubProxy.Invoke("JoinGroup", "clients");
		}

	    public void RunTest(string name) {
		    _hubProxy.Invoke("RunTest", name);
	    }
    }
}
