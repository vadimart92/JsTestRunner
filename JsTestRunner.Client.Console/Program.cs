using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsTestRunner.Client.Console
{
	class Program
	{
		static void Main(string[] args) {
			System.Console.WriteLine("Press any key to init.");
			System.Console.ReadKey();
			var client = new Core.Client(@"http://localhost:53852/signalr", System.Console.WriteLine);
			client.Init();
			System.Console.WriteLine("Initialized.");
			bool cmdEmpty;
			do {
				var cmd = System.Console.ReadLine();
				cmdEmpty = string.IsNullOrWhiteSpace(cmd);
				if (!cmdEmpty) {
					switch (cmd) {
						case "r":
							System.Console.WriteLine("Reloading page.");
							client.ReloadPage();
							break;
						default:
							System.Console.WriteLine("Run test");
							client.RunTest(cmd);
							break;
					}
				}
			} while (!cmdEmpty);

		}
	}
}
