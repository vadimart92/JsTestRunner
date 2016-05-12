$(function () {
	var runner = window.runner = Ext.create("JsTestRunner.Client.SignalR", {
		baseUrl: "http://localhost:53853/signalr"
	});
	runner.init();
});
