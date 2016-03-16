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
			System.Console.ReadLine();
			var client = new Core.Client(@"http://localhost:53852/signalr", System.Console.WriteLine);
			client.Init();
            bool cmdEmpty;
			do {
				var cmd = System.Console.ReadLine();
				cmdEmpty = string.IsNullOrWhiteSpace(cmd);
				if (!cmdEmpty) {
					client.RunTest(cmd);
				}
			} while (!cmdEmpty);

		}
	}
}
