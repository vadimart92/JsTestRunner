var Level = {
	ERROR: 0,
	SUCCESS: 1,
	INFO: 2,
	WARNING: 3
}
var State = {
    READY: 1,
    ERROR: -1,
    WAITING: 2
};

var Ext = window.Ext;
Ext.define("JsTestRunner.BaseObject", {
	mixins: {
		observable: "Ext.util.Observable"
	},
	listeners: null,
	destroyed: false,
	logEnabled: false,
	console: window.console,
	constructor: function (config) {
		var initialConfig = this.initialConfig = config || {};
		Ext.apply(this, initialConfig);
		this.callParent(arguments);
		this.mixins.observable.constructor.call(this);
		var log = this.console.log.bind(this.console);
		this.console.log = function () {
			if (this.logEnabled) {
				log(arguments);
			}
		}.bind(this);
	}
});
Ext.define("JsTestRunner.TestRunner", {
	extend: "JsTestRunner.BaseObject",
	constructor: function () {
		this.callParent(arguments);
		this.initHarness().then(this.init.bind(this));
	},
	initHarness: function () {
		var defer = jQuery.Deferred();
		this.harness = window.Siesta.my.activeHarness;
		var me = this;
		if (this.harness) {
			defer.resolve();
		} else {
			window.console.log("Harness not found, going to sleep");
			var cleared = false;
			var int = setInterval(function () {
				me.harness = window.Siesta.my.activeHarness;
				if (me.harness) {
					window.clearInterval(int);
					if (!cleared) {
						cleared = true;
						defer.resolve();
					}
				}
			}, 500);
		}
		return defer;
	},
	init: function () {
		this.harness.configure({
			listeners: {
				testsuitestart: function (event, harness) {
					this.harnessEvent("testsuitestart", null, harness.title);
				}.bind(this),
				testsuiteend: function (event, harness) {
				    this.harnessEvent("testsuiteend", null, harness.title);
				}.bind(this),
				teststart: function (event, test) {
				    var testName = this.getClassNameFromUrl(test.url);
					this.harnessEvent("teststart", testName);
				}.bind(this),
				testupdate: function (event, test, result) {
					var className = this.getClassNameFromUrl(test.url);
					var text = result.description + (result.annotation ? ", " + result.annotation : '');
					var state = result.passed === false ? Level.ERROR : Level.SUCCESS;
					this.harnessEvent("assert", className, text, state, Ext.apply(result, { url: className }));
				}.bind(this),
				testfailedwithexception: function (event, test) {
				    var className = this.getClassNameFromUrl(test.url);
					this.console.log(text);
					this.harnessEvent("testfailedwithexception", className, test.failedException, Level.ERROR, { exception: test.failedException });
				}.bind(this),
				testfinalize: function (event, test) {
				    var className = this.getClassNameFromUrl(test.url);
				    this.harnessEvent("testfinalize", className);
				}.bind(this)
			}
		});
	},
	classNameRe: /(.*)\/(\w*?).js(.*)/,
	getClassNameFromUrl: function (url) {
		var match = this.classNameRe.exec(url);
		if (match && match[2]) {
			return match[2];
		}
		return url;
	},
	harnessEvent: function (event, testId, message, state, payload) {
		if (state === undefined) {
			state = Level.INFO;
		}
		var data = { text: message, state: state };
		if (testId) {
		    data.testId = testId;
		}
		if (payload) {
			data.payload = payload;
		}
		this.fireEvent("harnessEvent", event, data);
	},
	findMatchedUrls: function (items, name, result) {
		name = name.toLowerCase();
		var me = this;
		Ext.each(items, function (item) {
			var id = item.id;
			if (id.toLowerCase().indexOf(name) > -1 && item.url) {
				result.push(item.url);
			}
			if (item.items && item.items.length > 0) {
				me.findMatchedUrls(item.items, name, result);
			}
		});
	},
	getListeners: function () {
		return ["runTest", "reload", "ping"];
	},
	ping: function () {
		this.log("Pong");
	},
	runTest: function (name) {
		var urlsToRun = [];
		var me = this;
		var match = /(.*)\.(\w*)\.js/.exec(name);
		if (match && match[2]) {
			name = match[2];
		}
		this.findMatchedUrls(this.harness.descriptors, name, urlsToRun);
		if (urlsToRun.length > 0) {
			this.log(Ext.String.format("{1} tests for {0} found", name, urlsToRun.length));
			var descriptorsToRun = [];
			Ext.each(urlsToRun, function (url) {
				var descriptor = me.harness.getScriptDescriptor(url);
				descriptorsToRun.push(descriptor);
			});
			this.harness.launch(descriptorsToRun);
		} else {
			this.log(Ext.String.format("No tests for {0} found", name));
			this.harnessEvent("testsuiteend");
		}
	},
	reload: function (forceGet) {
		this.fireEvent("log", "Page reload called.");
		window.location.reload(forceGet);
	},
	log: function (msg) {
		this.fireEvent("log", msg);
	}
});

Ext.define("JsTestRunner.Client.SignalR", {
	extend: "JsTestRunner.BaseObject",
	baseUrl: "",
	connection: null,
	testRunnerBroker: null,
	initConnection: function () {
		this.connection.hub.url = this.baseUrl;
		return this.connection.hub.start();
	},
	initHubs: function () {
		var testRunnerBroker = this.connection.TestRunnerBroker;
		this.testRunnerBroker = testRunnerBroker;
	},
	init: function () {
		this.connection = $.connection;
		this.runner = Ext.create("JsTestRunner.TestRunner");
		this.initHubs();
		this.initMessages();
		this.initConnection().done(this.onConnected.bind(this));
		setInterval(function () {
		    try {
		        this.checkConnection();
		    } catch (e) {
		        this.logEnabled = true;
		        this.log("testRunnerBroker connection check failed with exception");
		        this.logEnabled = false;
		        this.console.logException(e);
		    }
		}.bind(this), 1000);
	},
	checkConnectionFlag: true,
	checkConnection: function () {
		if (!this.checkConnectionFlag) {
			return;
		}
		var me = this;
		if (this.testRunnerBroker.connection.state === $.signalR.connectionState.disconnected) {
			this.checkConnectionFlag = false;
			var enableConnectionCheck = function () {
				me.checkConnectionFlag = true;
			}
			this.testRunnerBroker.connection.start().then(enableConnectionCheck, enableConnectionCheck);
		}
	},
	initMessages: function () {
		var listeners = this.runner.getListeners();
		var runner = this.runner;
		var broker = this.testRunnerBroker;
		var me = this;
		Ext.each(listeners, function (listener) {
			broker.client[listener] = function () {
				var args = arguments;
				try {
					runner[listener].apply(runner, args);
				} catch (e) {
					me.logError(e.message, e.stack);
				}
			};
		});
	},
	onConnected: function () {
		var bi = this.getBrowserInfo();
		this.testRunnerBroker.server.joinAsRunner(bi).done(this.postState.bind(this, State.READY));
		this.runner.on("harnessEvent", this.onHarnessEvent, this);
		this.runner.on("log", this.log, this);
		this.runner.on("state", this.postState, this);
	},
	getBrowserInfo: function () {
		var ua = navigator.userAgent, tem, matchArray = ua.match(/(opera|chrome|safari|firefox|msie|trident(?=\/))\/?\s*(\d+)/i) || [];
		if (/trident/i.test(matchArray[1])) {
			tem = /\brv[ :]+(\d+)/g.exec(ua) || [];
			return { name: 'IE ', version: (tem[1] || '') };
		}
		if (matchArray[1] === 'Chrome') {
			tem = ua.match(/\bOPR\/(\d+)/)
			if (tem != null) { return { name: 'Opera', version: tem[1] }; }
		}
		matchArray = matchArray[2] ? [matchArray[1], matchArray[2]] : [navigator.appName, navigator.appVersion, '-?'];
		if ((tem = ua.match(/version\/(\d+)/i)) != null) { matchArray.splice(1, 1, tem[1]); }
		return {
			name: matchArray[0],
			version: matchArray[1]
		};
	},
	onHarnessEvent: function (eventName, config) {
		this.testRunnerBroker.server.onHarnessEvent(eventName, config);
	},
	log: function (msg) {
		this.testRunnerBroker.server.log(msg);
	},
	postState: function (state) {
	    this.testRunnerBroker.server.postState(state);
	},
	logError: function (message, stack) {
		this.testRunnerBroker.server.logError(message, stack);
	}

});
