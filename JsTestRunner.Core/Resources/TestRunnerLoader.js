//lets write our own amd loader :)

var baseUri = "http://localhost:53853/";

loadfiles([
	"Resources/jquery.signalR-2.2.0.js",
	"signalr/hubs",
	"Resources/JsTestRunner.js",
	"Resources/Bootstrap.js"
]);

function loadfiles(filenames) {
	var start;
	var queue = start = jQuery.Deferred();
	for (var index in filenames) {
		if (filenames.hasOwnProperty(index)) {
			queue = addToQueue(queue, filenames[index]);;
		}
	}
	start.resolve();
}

function addToQueue(queue, url) {
	var next = jQuery.Deferred();
	queue.then(function () {
		var src = baseUri + url;
		jQuery.getScript(src, function () {
			next.resolve();
		});
	});
	return next;
}

