﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using JsTestRunner.Core.Interfaces;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json.Linq;

namespace JsTestRunner.Core
{
	[HubName("TestRunnerBroker")]
	public class TestRunnerBroker : Hub<ITestRunnerClientContract>, ITestRunnerBroker
	{
		private static readonly ConcurrentDictionary<string, BrowserInfo> _testRunners = new ConcurrentDictionary<string, BrowserInfo>();
		private const string TestRunnersGroupName = "testRunners";
		private const string ClientsGroupName = "clients";

		ITestRunnerClientContract Runners { get { return Clients.OthersInGroup(TestRunnersGroupName); } }
		ITestRunnerClientContract RunnerClients { get { return Clients.OthersInGroup(ClientsGroupName); } } 
		
		public override Task OnDisconnected(bool stopCalled) {
			if (_testRunners.ContainsKey(Context.ConnectionId)) {
				Groups.Remove(Context.ConnectionId, TestRunnersGroupName);
				BrowserInfo val;
				_testRunners.TryRemove(Context.ConnectionId, out val);
			}
			return base.OnDisconnected(stopCalled);
		}

		public override Task OnReconnected() {
			if (_testRunners.ContainsKey(Context.ConnectionId)) {
				RunnerClients.AppendLog(RunnerInfo, "Runner reconected");
			}
			return base.OnReconnected();
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
			return Groups.Add(Context.ConnectionId, ClientsGroupName);
		}

		public void OnHarnessEvent(string eventName, JObject config) {
			RunnerClients.TestEvent(RunnerInfo, eventName, config.Value<string>("text"));
		}

		public void RunTest(string name) {
			Runners.RunTest(name);
		}

		public void ReloadPage(bool forceGet) {
			Runners.Reload(forceGet);
		}

		#endregion
	}
}
