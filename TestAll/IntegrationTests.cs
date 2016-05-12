using System;
using System.Net;
using System.Threading.Tasks;
using JsTestRunner.Client.Core;
using JsTestRunner.Core.Interfaces;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using NUnit.Framework;

namespace TestAll
{
	[TestFixture]
	public class IntegrationTests
	{
		private IDisposable _server;
		Client _consoleClient;
		HubConnection _jsHubConnection;
		IHubProxy<ITestRunnerBroker, ITestRunnerClientContract> _jsProxy;
		private string _serverUrl;
		TimeSpan _timeout = TimeSpan.FromSeconds(13);

		[SetUp]
		public void Setup() {
			_serverUrl = new UriBuilder(Uri.UriSchemeHttp, "localhost", 50000).ToString();
			_server = WebApp.Start(_serverUrl);
			_consoleClient = new Client(_serverUrl, (s, b) => {
				Console.WriteLine(s);
			});
			_consoleClient.Connect(_timeout);
			var queryString = "clientType=JsTestRunner&browserInfo=" +
							JsonConvert.SerializeObject(new BrowserInfo {Name = "test", Version = "0.1"});
			_jsHubConnection = new HubConnection(_serverUrl, queryString);
			ServicePointManager.DefaultConnectionLimit = 10;
		}

		private void InitJsConnection(Action<IHubProxy<ITestRunnerBroker, ITestRunnerClientContract>> proxyInit) {
			_jsProxy = _jsHubConnection.CreateHubProxy<ITestRunnerBroker, ITestRunnerClientContract>("TestRunnerBroker");
			proxyInit(_jsProxy);
			_jsHubConnection.Start().Wait(_timeout);
		}

		[TearDown]
		public void TearDown() {
			_consoleClient.Dispose();
			_jsProxy.Dispose();
			if (_server != null) {
				_server.Dispose();
				_server = null;
			}
		}

		[Test]
		public void Ping() {
			bool pingCalled = false;
			InitJsConnection((jsProxy) => {
				jsProxy.SubscribeOn(h => h.Ping, () => {
					pingCalled = true;
				});
			});
			_consoleClient.Ping();
			Assert.IsTrue(pingCalled);
		}

		[Test]
		public void RunNotExistTest_If_RunnerNotRespond() {
			string testName = string.Empty;
			InitJsConnection((jsProxy) => {
				jsProxy.SubscribeOn<string>(h => h.RunTest, (test) => {
					testName = test;
				});
			});
			_consoleClient.RunTest("hello");
			_consoleClient.WaitRunToComplete(TimeSpan.FromSeconds(2));
			Assert.AreEqual("hello", testName);
		}

		[Test]
		public void RunNotExistTe() {
			string testName = string.Empty;
			InitJsConnection((jsProxy) => {
				jsProxy.SubscribeOn<string>(h => h.RunTest, (test) => {
					testName = test;
				});
			});
			_consoleClient.RunTest("hello");
			_consoleClient.WaitRunToComplete(TimeSpan.FromSeconds(2));
			Assert.AreEqual("hello", testName);
		}
	}
}
