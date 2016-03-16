using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace JsTestRunner.Core.Interfaces
{
	public interface ITestRunnerBroker
	{
		Task JoinAsRunner(BrowserInfo info);
		void Log(string log);
		void LogError(string message, string stackTrace);

		Task JoinAsClient();
		void OnHarnessEvent(string eventName, JObject config);
		void ReloadPage(bool forceGet);
		void RunTest(string name);
	}
}