using System;
using System.Threading;

namespace Tvmaid
{
	internal class Ticket : IDisposable
	{
		private Mutex mutex;

		public Ticket(string name)
		{
			this.mutex = new Mutex(false, name);
		}

		public bool GetOwner(int timeout)
		{
			bool result;
			try
			{
				result = this.mutex.WaitOne(timeout);
			}
			catch (AbandonedMutexException)
			{
				result = true;
			}
			return result;
		}

		public void Release()
		{
			this.mutex.ReleaseMutex();
		}

		public void Dispose()
		{
			this.mutex.ReleaseMutex();
			this.mutex.Dispose();
		}
	}
}
