using System;

namespace JsTestRunner.Client.Core
{
	public class TestRunState {
		private volatile bool _running;
		private readonly object _locker = new object();
		private DateTime _timeStamp;
		
		public void Check() {
			lock (_locker) {
				_timeStamp = DateTime.Now;
			}
		}

		public bool IsRunning(TimeSpan timeout) {
			if (!_running) {
				return false;
			}
			bool isTimeOut;
			lock (_locker) {
				isTimeOut = DateTime.Now > _timeStamp + timeout;
				if (isTimeOut) {
					_running = false;
				}
			}
			return !isTimeOut;
		}

		public void Run() {
			if (_running) {
				throw new InvalidOperationException("already running");
			}
			Check();
			_running = true;
		}

		public void Stop() {
			_running = false;
		}
	}
}