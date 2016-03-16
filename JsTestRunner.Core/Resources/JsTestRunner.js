var EventState = {
    ERROR: 0,
    SUCCESS: 1,
    WARNING: 2
}
var Ext = window.Ext;
Ext.define("JsTestRunner.BaseObject", {
    mixins: {
        observable: "Ext.util.Observable"
    },
    listeners: null,
    destroyed: false,
    console: window.console,
    constructor: function (config) {
        var initialConfig = this.initialConfig = config || {};
        Ext.apply(this, initialConfig);
        this.callParent(arguments);
        this.mixins.observable.constructor.call(this);
    }
});
Ext.define("JsTestRunner.TestRunner", {
    extend: "JsTestRunner.BaseObject",
    constructor: function () {
        this.callParent(arguments);
        this.initHarness();
        this.init();
    },
    initHarness: function () {
        this.harness = window.Siesta.my.activeHarness;
        if (!this.harness) {
            throw new "Harness not found";
        }
    },
    init: function () {
        this.harness.configure({
            listeners: {
                testsuitestart: function (event, harness) {
                    var text = 'Test suite is starting: ' + harness.title;
                    this.console.log(text);
                    this.harnessEvent("testsuitestart", text);
                }.bind(this),
                testsuiteend: function (event, harness) {
                    var text = 'Test suite is finishing: ' + harness.title;
                    this.console.log(text);
                    this.harnessEvent("testsuiteend", text);
                }.bind(this),
                teststart: function (event, test) {
                    var text = 'Test case is starting: ' + test.url;
                    this.console.log(text);
                    this.harnessEvent("teststart", text);
                }.bind(this),
                testupdate: function (event, test, result) {
                    var text = 'Test case [' + test.url + '] has been updated: ' + result.description + (result.annotation ? ', ' + result.annotation : '');
                    this.console.log(text);
                    this.harnessEvent("testupdate", text);
                }.bind(this),
                testfailedwithexception: function (event, test) {
                    var text = 'Test case [' + test.url + '] has failed with exception: ' + test.failedException;
                    this.console.log(text);
                    this.harnessEvent("testfailedwithexception", text, EventState.ERROR, { exception: test.failedException });
                }.bind(this),
                testfinalize: function (event, test) {
                    var text = 'Test case [' + test.url + '] has completed';
                    this.console.log(text);
                    this.harnessEvent("testfinalize", text);
                }.bind(this)
            }
        });
    },
    harnessEvent: function (event, message, state, payload) {
        if (Ext.isEmpty(state)) {
            state = EventState.SUCCESS;
        }
        var data = { text: message, state: state };
        if (payload) {
            data.payload = payload;
        }
        this.fireEvent("harnessEvent", event, data);
    },
    findMatchedUrls: function (items, name, result) {
        var me = this;
        Ext.each(items, function (item) {
            var id = item.id;
            if (id.indexOf(name) > -1 && item.url) {
                result.push(item.url);
            }
            if (item.items && item.items.length > 0) {
                me.findMatchedUrls(item.items, name, result);
            }
        });
    },
    getListeners: function() {
        return ["runTest", "reload"];
    },
    runTest: function (name) {
        var urlsToRun = [];
        var me = this;
        this.findMatchedUrls(this.harness.descriptors, name, urlsToRun);
        if (urlsToRun.length > 0) {
            this.log(Ext.String.format("{1} tests for {0} found", name, urlsToRun.length));
            var descriptorsToRun = [];
            Ext.each(urlsToRun, function(url) {
                var descriptor = me.harness.getScriptDescriptor(url);
                descriptorsToRun.push(descriptor);
            });
            this.harness.launch(descriptorsToRun);
        } else {
            this.log(Ext.String.format("No tests for {0} found", name));
        }
    },
    reload: function (forceGet) {
        this.fireEvent("log", "Page reload called.");
        window.location.reload(forceGet);
    },
    log: function(msg) {
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
        setInterval(this.checkConnection.bind(this), 1000);
    },
    checkConnectionFlag: true,
    checkConnection: function () {
        if (!this.checkConnectionFlag) {
            return;
        }
        var me = this;
        if (this.testRunnerBroker.connection.state === $.signalR.connectionState.disconnected) {
            this.checkConnectionFlag = false;
            var enableConnectionCheck = function() {
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
        Ext.each(listeners, function(listener) {
            broker.client[listener] = function() {
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
        this.testRunnerBroker.server.joinAsRunner(bi);
        this.runner.on("harnessEvent", this.onHarnessEvent, this);
        this.runner.on("log", this.log, this);
    },
    getBrowserInfo: function() {
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
    logError: function(message, stack) {
        this.testRunnerBroker.server.logError(message, stack);
    }

});
