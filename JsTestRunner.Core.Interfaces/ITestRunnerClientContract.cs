namespace JsTestRunner.Core.Interfaces {
	public interface ITestRunnerClientContract {
		void RunTest(string name);
		void Reload(bool forseGet);
		void AppendLog(string runner, string msg);
		void TestEvent(string runner, string eventName, string config);
	}
}