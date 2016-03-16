Ext.define("Terrasoft.core.BaseObject", {
    alternateClassName: "Terrasoft.BaseObject",
    mixins: {
        observable: "Ext.util.Observable"
    },
    listeners: null,
    destroyed: false,
    console: window.console,
    constructor: function(config) {
        var initialConfig = this.initialConfig = config || {};
        Ext.apply(this, initialConfig);
        this.callParent(arguments);
        this.mixins.observable.constructor.call(this);

    }
});
Ext.define("Terrasoft.TestRunner.Runner", {
    extend: "Terrasoft.core.BaseObject",
    constructor: function () {
        this.initHarness();
        this.init();
        this.addEvents(
			"harnessEvent"
		);
    },
    initHarness: function() {
        this.harness = window.Siesta.my.activeHarness;
        if (!this.harness) {
            throw new "Harness not found";
        }
    },
    init: function() {
        this.harness.configure({
            listeners: {
                testsuitestart: function (event, harness) {
                    var texst = 'Test suite is starting: ' + harness.title;
                    this.console.log(text);
                    this.fireEvent("harnessEvent", "testsuitestart", { text: text });
                },
                testsuiteend: function (event, harness) {
                    var texst = 'Test suite is finishing: ' + harness.title;
                    this.console.log(text);
                    this.fireEvent("harnessEvent", "testsuiteend", { text: text });
                },
                teststart: function (event, test) {
                    console.log('Test case is starting: ' + test.url);
                },
                testupdate: function (event, test, result) {
                    console.log('Test case [' + test.url + '] has been updated: ' + result.description + (result.annotation ? ', ' + result.annotation : ''))
                },
                testfailedwithexception: function (event, test) {
                    console.log('Test case [' + test.url + '] has failed with exception: ' + test.failedException);
                },
                testfinalize: function (event, test) {
                    console.log('Test case [' + test.url + '] has completed');
                }
            }
        });
    },
    runTest: function (name) {
        debugger;
        var descriptors = [];
        Ext.each(this.harness.descriptors, function (descriptors) {
            Ext.each(descriptors.items, function(item) {
                var id = item.Id;
                if (id.indexOf(name)) {
                    var descriptor = this.harness.getScriptDescriptor(id);
                    descriptors.push(descriptor);
                }
            });
        });
        
        this.harness.launch(descriptors);
    }
});

Ext.define("TestRunner.Client.SignalR", {
    extend: "Terrasoft.core.BaseObject",
    baseUrl: "",
    connection: null,
    testRunnerBroker: null,
    initConnection: function() {
        this.connection.hub.url = this.baseUrl;
        return this.connection.hub.start();
    },
    initHubs: function() {
        var testRunnerBroker = this.connection.TestRunnerBroker;
        this.testRunnerBroker = testRunnerBroker;
    },
    init: function() {
        this.connection = $.connection;
        this.runner = Ext.create("Terrasoft.TestRunner.Runner");
        this.initHubs();
        this.initMessages();
        this.initConnection().done(this.onConnected.bind(this));
    },
    initMessages: function() {
        this.testRunnerBroker.client.addMessage = function (action, config) {
            console.log("[remote message]");
            switch (action) {
            case "runTest":
                this.runner.runTest(config);
            default:
            }
        };  
    },
    onConnected: function() {
        this.testRunnerBroker.server.joinGroup("testRunners");
        this.runner.on("harnessEvent", this.onHarnessEvent, this);
    },
    onHarnessEvent: function(eventName, config) {
        this.testRunnerBroker.onHarnessEvent(eventName, config);
    }

});
