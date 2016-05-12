using Newtonsoft.Json.Linq;

namespace JsTestRunner.Core.Interfaces {
	public interface ITestRunnerClientContract {
		void RunTest(string name);
		void Reload(bool forseGet);
		void AppendLog(string runner, string msg);
		void TestEvent(string runner, string eventName, int state, string text, JObject data);
		void Ping();
		void SendRunnerState(string runnerInfo, RunnerState state);
		void SendAnyRunnerConnected(bool connected);
	}
}