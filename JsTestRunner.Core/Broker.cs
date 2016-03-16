using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json.Linq;

namespace JsTestRunner.Core
{
	[HubName("TestRunnerBroker")]
	public class TestRunnerBroker : Hub
	{
		public bool AgentConnected() {
			Clients.AllExcept(Clients.Caller);
			return true;
		}

		public Task JoinGroup(string groupName) {
			return Groups.Add(Context.ConnectionId, groupName);
		}

		dynamic Runners { get { return Clients.OthersInGroup("testRunners"); } } 
		dynamic RunnerClients { get { return Clients.OthersInGroup("clients"); } } 

		public void RunTest(string name) {
			Runners.runTest(name);
		}
		
		public void OnHarnessEvent(string eventName, JObject config) {
			RunnerClients.testEvent(eventName, config);
		}
	}
}
