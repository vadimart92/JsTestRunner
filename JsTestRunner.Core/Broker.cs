using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JsTestRunner.Core.Interfaces;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsTestRunner.Core
{
	[HubName("TestRunnerBroker")]
	public class TestRunnerBroker : Hub<ITestRunnerClientContract>, ITestRunnerBroker
	{
		private static readonly ConcurrentDictionary<string, BrowserInfo> _testRunners = new ConcurrentDictionary<string, BrowserInfo>();
		private static readonly ConcurrentBag<string> _testRunnerClients = new ConcurrentBag<string>();
		private const string TestRunnersGroupName = "testRunners";
		private const string ClientsGroupName = "clients";

		ITestRunnerClientContract Runners { get { return Clients.OthersInGroup(TestRunnersGroupName); } }
		ITestRunnerClientContract RunnerClients { get { return Clients.OthersInGroup(ClientsGroupName); } } 
		
		public override Task OnDisconnected(bool stopCalled) {
			if (_testRunners.ContainsKey(Context.ConnectionId)) {
				RunnerClients.AppendLog(RunnerInfo, "Runner disconected. " + RunnersCountMs);
				Groups.Remove(Context.ConnectionId, TestRunnersGroupName);
				BrowserInfo val;
				_testRunners.TryRemove(Context.ConnectionId, out val);
				
			}
			return base.OnDisconnected(stopCalled);
		}

		public static Task<BrowserInfo> WaitClientToConnect(CancellationToken token) {
			int maxLoopCount = 100;
			return Task<BrowserInfo>.Factory.StartNew(() => {
				while (true) {
					if (IsAnyRunnerConnected) {
						return _testRunners.Values.First();
					}
					if (token.IsCancellationRequested) {
						return null;
					}
					if (maxLoopCount == 0) {
						return null;
					}
					Thread.Sleep(TimeSpan.FromSeconds(1));
					maxLoopCount--;
				}
			});
		}

		private string RunnersCountMs {
			get { return string.Format("Active runners: {0}", _testRunners.Count); }
		}

		public override Task OnReconnected() {
			CheckClientConnected();
			return base.OnReconnected();
		}

		public override Task OnConnected() {
			CheckClientConnected();
			return base.OnConnected();
		}

		void CheckClientConnected() {
			if (_testRunners.ContainsKey(Context.ConnectionId)) {
				RunnerClients.AppendLog(RunnerInfo, "Runner reconected. " + RunnersCountMs);
			} else {
				if (Context.GetClientType() == ClientType.JsTestRunner) {
					var browserInfoString = Context.QueryString["browserInfo"];
					var bi = JsonConvert.DeserializeObject<BrowserInfo>(browserInfoString);
					JoinAsRunner(bi);
				}
			}
		}

		private string RunnerInfo {
			get {
				BrowserInfo val;
				if (_testRunners.TryGetValue(Context.ConnectionId, out val)) {
					return string.Format("{0}/v.{1}", val.Name, val.Version );
				}
				return "undefined runner";
			}
		}

		#region RunnerAPI

		public Task JoinAsRunner(BrowserInfo info) {
			_testRunners[Context.ConnectionId] = info;
			RunnerClients.AppendLog(RunnerInfo, "Runner conected. " + RunnersCountMs);
			return Groups.Add(Context.ConnectionId, TestRunnersGroupName);
		}
		
		public void Log(string log) {
			RunnerClients.AppendLog(RunnerInfo, log);
		}

		public void LogError(string message, string stackTrace) {
			var msg = string.Format("Command run error:{0}{1}Stack trace:{1}{2}", message, Environment.NewLine, stackTrace);
            RunnerClients.AppendLog(RunnerInfo, msg);
		}

		#endregion

		#region ClientAPI

		public Task JoinAsClient() {
			_testRunnerClients.Add(Context.ConnectionId);
			return Groups.Add(Context.ConnectionId, ClientsGroupName);
		}

		public void OnHarnessEvent(string eventName, JObject config) {
			RunnerClients.TestEvent(RunnerInfo, eventName, config.Value<int>("state"), config.Value<string>("text"), config.Value<JObject>("payload"));
		}

		public void RunTest(string name) {
			if (!IsAnyRunnerConnected) {
				Log("No test runner found. Waiting to connect...");
				var cts = new CancellationTokenSource();
				WaitClientToConnect(cts.Token).Wait(TimeSpan.FromSeconds(5));
				cts.Cancel();
				if (!IsAnyRunnerConnected) {
					Log("No test runner found. Tyr to restart plaese.");
					return;
				}
			}
			Runners.RunTest(name);

		}

		private static bool IsAnyRunnerConnected {
			get { return _testRunners.Count != 0; }
		}

		public void Ping() {
			Clients.Caller.AppendLog("Ping accepted. ",  RunnersCountMs);
			Runners.Ping();
		}

		public void PostState(RunnerState state) {
			RunnerClients.SendRunnerState(RunnerInfo, state);
		}

		public void RequestRunnersCount() {
			Clients.Caller.SendAnyRunnerConnected(IsAnyRunnerConnected);
		}

		public void ReloadPage(bool forceGet) {
			Runners.Reload(forceGet);
		}

		#endregion
	}
}