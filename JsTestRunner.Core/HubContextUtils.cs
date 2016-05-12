using Microsoft.AspNet.SignalR.Hubs;

namespace JsTestRunner.Core
{

	public enum ClientType
	{
		Unknown,
		JsTestRunner
	}

	public static class HubContextUtils
	{
		public static string JsTestRunner = "JsTestRunner";

		public static ClientType GetClientType(this HubCallerContext context) {
			if (context.QueryString["clientType"] == "JsTestRunner") {
				return ClientType.JsTestRunner;
			}
			return ClientType.Unknown;
		}
	}
}