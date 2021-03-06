﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using JsTestRunner.Core.Interfaces;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using Newtonsoft.Json.Linq;

namespace JsTestRunner.Client.Core
{
	public enum LoggingLevel
	{
		Min = 1,
		Full = 2
	}

	public class Client: IDisposable
	{
		readonly string _url;
		private IHubProxy<ITestRunnerBroker, ITestRunnerClientContract> _hubProxy;
		private readonly Action<string, bool?> _logger;
		private readonly TestRunState _runState;

		public Client(string ulr, Action<string, bool?> logger) {
			_url = ulr;
			_logger = logger;
			_runState = new TestRunState();
		}

		public void Connect(TimeSpan timeout) {
			_hubConnection = new HubConnection(_url);
			_hubProxy = _hubConnection.CreateHubProxy<ITestRunnerBroker, ITestRunnerClientContract>("TestRunnerBroker");
			InitSubscriptions();
			_hubConnection.Start().Wait(timeout);
			_hubProxy.Call(h => h.JoinAsClient());
		}

		private volatile bool _waitingForRunner = false;
		private HubConnection _hubConnection;

		public void WaitForRunner() {
			_waitingForRunner = true;
			int attempsCount = 0;
			var retryCount = TimeSpan.FromMinutes(2).TotalSeconds;
			while (true) {
				attempsCount++;
				if (_hubConnection.State == ConnectionState.Connected) {
					_hubProxy.Call(h => h.RequestRunnersCount());
				} else {
					_hubConnection.Start();
				}
				
				Thread.Sleep(TimeSpan.FromSeconds(1));
				if (!_waitingForRunner) {
					return;
				}
				if (attempsCount > retryCount) {
					throw new InvalidOperationException("Cant get runners count");
				}
			}
		}
		
		private void InitSubscriptions() {
			_hubProxy.SubscribeOn<string, string, int, string, JObject>(h => h.TestEvent, (runner, eventName, state, text, payload) => {
				bool? stateRes = null;
				if (state != 2) {
					stateRes = state == 1;
				}
				_logger(string.Format("Runner: {2}{1}Event: {0}", eventName, Environment.NewLine, runner), stateRes);
				if (text != null) {
					_logger(text, stateRes);
				}
				if (eventName == "testsuiteend") {
					_runState.Stop();
				}
			});
			_hubProxy.SubscribeOn<string, string>(h => h.AppendLog, (runner, log) => {
				_logger(string.Format("Runner: {2}{1}{0}", log, Environment.NewLine, runner), null);
			});
			_hubProxy.SubscribeOn<string, RunnerState>(h => h.SendRunnerState, (runnerInfo, state) => {
				//todo: handle state and info
				if (state == RunnerState.Ready) {
					_waitingForRunner = false;
				}
			});
			_hubProxy.SubscribeOn<bool>(h => h.SendAnyRunnerConnected, (isConnected) => {
				if (isConnected) {
					_waitingForRunner = false;
				}
			});
		}

		public void RunTest(string name) {
			_hubProxy.Call(broker => broker.RunTest(name));
			_runState.Run();
		}

		public void WaitRunToComplete(TimeSpan testRunTimeout) {
			Task.Run(() => {
				while (true) {
					if (!_runState.IsRunning(testRunTimeout)) {
						return;
					}
				}
			}).Wait();
		}

		public void ReloadPage() {
			_hubProxy.Call(broker => broker.ReloadPage(true));
		}

		public void Ping() {
			_hubProxy.Call(broker => broker.Ping());
		}

		public void Dispose() {
			_hubProxy.Dispose();
		}
	}
}
