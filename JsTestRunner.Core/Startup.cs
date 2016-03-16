using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Diagnostics;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;


[assembly: OwinStartup(typeof(JsTestRunner.Core.Startup))]
namespace JsTestRunner.Core
{
    public class Startup
    {
	    public void Configuration(IAppBuilder appBuilder) {
		    appBuilder.UseErrorPage(ErrorPageOptions.ShowAll);
		    appBuilder.UseCors(CorsOptions.AllowAll);
		    var hubConfiguration = new HubConfiguration {
			    EnableDetailedErrors = true,
			    EnableJavaScriptProxies = true,
				EnableJSONP = false
		    };
		    appBuilder.MapSignalR(hubConfiguration);
		    var resourceFileSystem = new PhysicalFileSystem(Path.Combine(GetBasePath(), "Resources"));
#if DEBUG
			resourceFileSystem = new PhysicalFileSystem(@"F:\DEV\CScharp\IISAdminOwin\WebSiteManagment\JsTestRunner\JsTestRunner.Core\Resources");
#endif 
			appBuilder.UseFileServer(new FileServerOptions() {
				RequestPath = new PathString(@"/Resources"),
				FileSystem = resourceFileSystem,
				EnableDirectoryBrowsing = true
			});
			appBuilder.UseStaticFiles(new StaticFileOptions {
			    RequestPath = new PathString(@"/Resources"),
				FileSystem = resourceFileSystem,
			});
	    }
		public static string GetBasePath() {
			return AppDomain.CurrentDomain.SetupInformation.PrivateBinPath;
		}
	}
}
