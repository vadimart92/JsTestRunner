using System;
using JsTestRunner.Client.Console;
using NUnit.Framework;

namespace TestAll
{
	[TestFixture]
	public class ClientReplTest
	{
		[Test]
		public void Test0() {
			var repl = new Repl();
			repl.Parse("ping");
			Assert.AreEqual(repl.Command, Command.Ping);
		}
		[Test]
		public void Test1() {
			var repl = new Repl();
			repl.Parse("r");
			Assert.AreEqual(repl.Command, Command.RefreshPage);
			repl.Parse("refresh");
			Assert.AreEqual(repl.Command, Command.RefreshPage);
		}
		[Test]
		public void Test2() {
			var repl = new Repl();
			repl.Parse("t=run.js");
			Assert.AreEqual(repl.Command, Command.RunTest);
		}
		[Test]
		public void Test4() {
			var repl = new Repl();
			repl.Parse("h");
			Assert.AreEqual(Command.Help, repl.Command);
			repl.Parse("help");
			Assert.AreEqual(Command.Help, repl.Command);
		}
		[Test]
		public void Test5() {
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
