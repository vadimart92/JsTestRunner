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
                    this.fireEvent("harnessEvent", "testsuitestart", { text: text });
                }.bind(this),
                testsuiteend: function (event, harness) {
                    var text = 'Test suite is finishing: ' + harness.title;
                    this.console.log(text);
                    this.fireEvent("harnessEvent", "testsuiteend", { text: text });
                }.bind(this),
                teststart: function (event, test) {
                    console.log('Test case is starting: ' + test.url);
                }.bind(this),
                testupdate: function (event, test, result) {
                    console.log('Test case [' + test.url + '] has been updated: ' + result.description + (result.annotation ? ', ' + result.annotation : ''))
                }.bind(this),
                testfailedwithexception: function (event, test) {
                    console.log('Test case [' + test.url + '] has failed with exception: ' + test.failedException);
                }.bind(this),
                testfinalize: function (event, test) {
                    console.log('Test case [' + test.url + '] has completed');
                }.bind(this)
            }
        });
    },
    runTest: function (name) {
        var descriptorsToRun = [];
        var me = this;
        Ext.each(this.harness.descriptors, function (descriptors) {
            Ext.each(descriptors.items, function (item) {
                var id = item.id;
                if (id.indexOf(name) > -1 && item.url) {
                    var descriptor = me.harness.getScriptDescriptor(item.url);
                    descriptorsToRun.push(descriptor);
                }
            });
        });
        this.harness.launch(descriptorsToRun);
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
    },
    initMessages: function () {
        this.testRunnerBroker.client.runTest = function (name) {
            this.runner.runTest(name);
        }.bind(this);
    },
    onConnected: function () {
        this.testRunnerBroker.server.joinGroup("testRunners");
        this.runner.on("harnessEvent", this.onHarnessEvent, this);
    },
    onHarnessEvent: function (eventName, config) {
        this.testRunnerBroker.server.onHarnessEvent(eventName, config);
    }

});
