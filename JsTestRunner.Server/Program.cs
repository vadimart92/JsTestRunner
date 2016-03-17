using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using Microsoft.Win32;
using NDesk.Options;

namespace JsTestRunner.Server
{
	class Program
	{
		static string MyName {
			get { return Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName); }
		}
		public static void Main(string[] args) {
			RunnerOptions options = null;

			try {
				options = RunnerOptions.Parse(args);
			} catch (OptionException e) {
				Console.Write("{0}: ", MyName);
				Console.WriteLine(e.Message);
				Console.WriteLine("Try `{0} --help' for more information.", MyName);
				return;
			}

			if (options.Help) {
				ShowHelp(options.OptionSet);
				return;
			}
			var url = new UriBuilder(Uri.UriSchemeHttp, options.Host, options.Port).ToString();
			using (WebApp.Start(url)) {
				Console.WriteLine("Server running on {0}", url);
				if (options.AutoStartClients) {
					System.Diagnostics.Process.Start(options.BrowserPath, options.Url);
				}
				Console.WriteLine("Press enter to stop.");
				Console.ReadLine();
			}
		}

		static void ShowHelp(OptionSet p) {
			Console.WriteLine("Usage: {0} [options]", MyName);
			Console.WriteLine("Starts new test runner server");
			Console.WriteLine();
			Console.WriteLine("Options:");
			p.WriteOptionDescriptions(Console.Out);
		}

		class RunnerOptions {
			private string _browserPath;
			private string _url;
			public string Host { get; set; }
			public int Port { get; set; }
			public int Clients { get; set; }
			public bool AutoStartClients { get; set; }
			public string Browser { get; set; }

			public string Url {
				get {
					return _url ?? string.Empty;
				}
				set { _url = value; }
			}

			public string BrowserPath {
				get {
					return _browserPath ?? Convert.ToString(Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe", string.Empty, string.Empty));
				}
				set { _browserPath = value; }
			}

			public bool Help { get; set; }
			public OptionSet OptionSet { get; set; }


			public RunnerOptions() {
				AutoStartClients = true;
				Port = -1;
				Clients = 1;
			}
			
			public static RunnerOptions Parse(string[] args) {
				var options = new RunnerOptions();
				var p = new OptionSet {
					{ "h|host=", "the {host} for binding", v => options.Host = v },
					{ "p|port=", "the {port} for binding", v => options.Port = int.Parse(v) },
					{ "help=", "display {help}", v => options.Help = bool.Parse(v) },
					{ "u|url=", "relative {url} to test page", v => options.Url = v },
					{ "a|start=", "if {autostart} defined as true, the browser will be opened after server started", v => {
						int val;
						bool valBool;
						if (int.TryParse(v, out val)) {
							options.AutoStartClients = val > 0;
						} else if (bool.TryParse(v, out valBool)) {
							options.AutoStartClients = valBool;
						}
					}
					},
					{ "c|clients=", "the count of {clients} to start.", v => options.Clients = int.Parse(v) },
					{ "b|browser=", "the {browser} to start.", v => options.Browser = v },
					{ "path=", "the count of {clients} to start.", v => {
						if (Directory.Exists(v)) {
							options.BrowserPath = v;
						} else {
							throw  new DirectoryNotFoundException();
						}
					} }
				};
				p.Parse(args);
				if (!options.Help) {
					if (string.IsNullOrWhiteSpace(options.Host)) {
						Console.WriteLine("Host is not set, using localhost");
						options.Host = "localhost";
					}
					if (options.Port == -1) {
						Console.WriteLine("Port is not set, using random");
						options.Port = FreeTcpPort();
						Console.WriteLine("Port is {0}", options.Port);
					}
				}
				
				options.OptionSet = p;
                return options;
			}

			private static int FreeTcpPort() {
				TcpListener l = new TcpListener(IPAddress.Loopback, 0);
				l.Start();
				int port = ((IPEndPoint)l.LocalEndpoint).Port;
				l.Stop();
				return port;
			}
		}
	}
}
