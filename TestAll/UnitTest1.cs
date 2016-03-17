using System;
using JsTestRunner.Client.Console;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestAll
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void TestMethod0() {
			var repl = new Repl();
			repl.Parse("ping");
			Assert.AreEqual(repl.Command, Command.Ping);
		}
		[TestMethod]
		public void TestMethod1() {
			var repl = new Repl();
			repl.Parse("r");
			Assert.AreEqual(repl.Command, Command.RefreshPage);
			repl.Parse("refresh");
			Assert.AreEqual(repl.Command, Command.RefreshPage);
		}
		[TestMethod]
		public void TestMethod2() {
			var repl = new Repl();
			repl.Parse("t=run.js");
			Assert.AreEqual(repl.Command, Command.RunTest);
		}
		[TestMethod]
		public void TestMethod4() {
			var repl = new Repl();
			repl.Parse("h");
			Assert.AreEqual(Command.Help, repl.Command);
			repl.Parse("help");
			Assert.AreEqual(Command.Help, repl.Command);
		}
		[TestMethod]
		public void TestMethod5() {
			var repl = new Repl();
			repl.Parse("--t=x.js");
			Assert.AreEqual(Command.RunTest, repl.Command);
			Assert.AreEqual("x.js", repl.TestName);
			repl = new Repl();
			repl.Parse("t=x.js");
			Assert.AreEqual(Command.RunTest, repl.Command);
			Assert.AreEqual("x.js", repl.TestName);
			repl = new Repl();
			repl.Parse("test.js");
			Assert.AreEqual(Command.RunTest, repl.Command);
			Assert.AreEqual("test.js", repl.TestName);
		}
	}
}
