namespace JsTestRunner.Core.Interfaces
{
	public class BrowserInfo
	{
		public string Name { get; set; }
		public string Version { get; set; }
	}

	public enum RunnerState{
		Ready = 1,
		Error = -1,
		Waiting = 2
	}
}
