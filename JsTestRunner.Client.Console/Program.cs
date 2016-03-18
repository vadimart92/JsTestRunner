using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NDesk.Options;

namespace JsTestRunner.Client.Console
{
	class Program
	{
		static void Main(string[] args) {
			string url = null;
			string testName = null;
			var opts = new OptionSet {
				{ "u|url=", "the {URL} to connect", v => url = v },
				{ "r|run=", "the name of {TEST} to run", v => testName = v }
			};
			opts.Parse(args);
			if (string.IsNullOrWhiteSpace(url)) {
				url =  Properties.Settings.Default.Url;
			}
			var defColor = System.Console.ForegroundColor;
			var client = new Core.Client(url, (t, s) => {
				if (s.HasValue && s.Value) {
					System.Console.ForegroundColor = ConsoleColor.Green;
				}
				else if (s.HasValue) {
					System.Console.ForegroundColor = ConsoleColor.Red;
				}
				System.Console.WriteLine(t);
				System.Console.ForegroundColor = defColor;
			});
			client.Init();
			System.Console.WriteLine("Initialized.");
			bool cmdEmpty;
			if (!string.IsNullOrWhiteSpace(testName)) {
				client.RunTest(testName).Wait();
				return;
			}
			do {
				var cmd = System.Console.ReadLine();
				System.Console.Clear();
				cmdEmpty = string.IsNullOrWhiteSpace(cmd);
				if (!cmdEmpty) {
					var repl = new Repl();
					try {
						repl.Parse(cmd);
						switch (repl.Command) {
							case Command.RefreshPage:
								System.Console.WriteLine("Reloading page.");
								client.ReloadPage();
								break;
							case Command.Ping:
								client.Ping();
								break;
							case Command.Reconnect:
								client.Init();
								break;
							case Command.RunTest:
								System.Console.WriteLine("Running test {0}.", repl.TestName);
								client.RunTest(repl.TestName);
								break;
							case Command.Help:
							default:
								break;
						}
					}
					catch (Exception ex) {
						System.Console.WriteLine(ex.Message);
					}
				}
			} while (!cmdEmpty);

		}
	}

	public enum Command
	{
		RunTest = 0,
		RefreshPage = 1,
		Help = 2,
		Ping = 3,
		Reconnect = 4
	}
	public class Repl {
		public string CommandText { get; set; }
		public string TestName { get; set; }

		private Command? _cmd;
		public Command Command {
			get {
				if (_cmd.HasValue) {
					return _cmd.Value;
				}
				switch (CommandText) {
					case "r":
					case "refresh":
						return Command.RefreshPage;
					case "h":
					case "help":
						return Command.Help;
					case "ping":
						return Command.Ping;
					case "reconnect":
						return Command.Reconnect;
					default:
						return Command.RunTest;
                }
			}
		}
		
		public void Parse(string arguments) {
			var options = new OptionSet {
				{"t|test=", "the {TEST} to find and run", v => TestName = v}
			};
			var args = arguments.Split(' ').Select(a=>a.StartsWith("--")? a : "--" + a);
			var cmd = options.Parse(args).FirstOrDefault();
			if (!string.IsNullOrWhiteSpace(cmd)) {
				CommandText = cmd.Replace("--", string.Empty);
				if (Command == Command.RunTest) {
					TestName = CommandText;
				}
			}
		}

	}
}
