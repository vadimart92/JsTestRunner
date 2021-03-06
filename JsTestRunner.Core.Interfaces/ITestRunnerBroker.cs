﻿using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace JsTestRunner.Core.Interfaces
{
	public interface ITestRunnerBroker
	{
		void Log(string log);
		void LogError(string message, string stackTrace);

		Task JoinAsClient();
		void OnHarnessEvent(string eventName, JObject config);
		void ReloadPage(bool forceGet);
		void RunTest(string name);
		void Ping();
		void PostState(RunnerState state);
		void RequestRunnersCount();
	}
}