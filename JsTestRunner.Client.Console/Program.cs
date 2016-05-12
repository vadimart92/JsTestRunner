using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NDesk.Options;

namespace JsTestRunner.Client.Console
{
	class Program
	{
		static readonly ConsoleColor DefColor = System.Console.ForegroundColor;

		static void Main(string[] args) {
			string url = null;
			string testName = null;
			var opts = new OptionSet {
				{ "u|url=", "the {URL} to connect", v => url = v },
				{ "r|run=", "the name of {TEST} to run", v => testName = v }
			};
			opts.Parse(args);
			if (string.IsNullOrWhiteSpace(url)) {
				url =  Properties.Settings.Default.ServerUrl;
			}
			var client = new Core.Client(url, Log);
			try {
				InitConnection(client);
			} catch (Exception ex) {
				System.Console.WriteLine("Connection initialization error");
				System.Console.WriteLine(ex.Message);
				TryStartJsRunnerServer();
				InitConnection(client);
				System.Console.WriteLine("Waiting for runner...");
				client.WaitForRunner();
			}
			System.Console.WriteLine("Initialized.");
			if (!string.IsNullOrWhiteSpace(testName)) {
				client.RunTest(testName);
				client.WaitRunToComplete(Properties.Settings.Default.TestRunTimeout);
				return;
			}
			CommandLoop(client);
		}

		private static void TryStartJsRunnerServer() {
			var binPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) ?? string.Empty;
			var executablePath = Path.Combine(binPath, Properties.Settings.Default.TestRunnerServerAppName);
			System.Console.ForegroundColor = ConsoleColor.Green;
			System.Console.WriteLine("May be server app located in: {0}", executablePath);
			if (File.Exists(executablePath)) {
				var startArgs = GetServerStartArgs();
				var startInfo = new System.Diagnostics.ProcessStartInfo(executablePath, startArgs);
				startInfo.WorkingDirectory = Path.GetDirectoryName(executablePath);
				startInfo.UseShellExecute = true;
				var process = System.Diagnostics.Process.Start(startInfo);
				if (process != null && !process.HasExited) {
					return;
				}
			}
			throw new InvalidOperationException("can't start js runner server");
		}

		private static string GetServerStartArgs() {
			return  string.Format("-p={0} -url=\"{1}\"", Properties.Settings.Default.ServerPort, 
				Properties.Settings.Default.TestUrl);
		}

		private static void InitConnection(Core.Client client) {
			client.Connect(Properties.Settings.Default.ConnectionTimeout);
		}


		private static void CommandLoop(Core.Client client) {
			bool cmdEmpty;
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
								client.Connect(Properties.Settings.Default.ConnectionTimeout);
								break;
							case Command.RunTest:
								System.Console.WriteLine("Running test {0}.", repl.TestName);
								client.RunTest(repl.TestName);
								client.WaitRunToComplete(Properties.Settings.Default.TestRunTimeout);
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

		static void Log (string t, bool? s) {
			if (s.HasValue && s.Value) {
				System.Console.ForegroundColor = ConsoleColor.Green;
			}
			else if (s.HasValue) {
				System.Console.ForegroundColor = ConsoleColor.Red;
			}
			System.Console.WriteLine(t);
			System.Console.ForegroundColor = DefColor;
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

		public Command Command {
			get {
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
