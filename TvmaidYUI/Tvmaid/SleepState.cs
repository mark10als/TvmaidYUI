using System;
using System.Runtime.InteropServices;

namespace Tvmaid
{
	internal class SleepState
	{
		[Flags]
		private enum ExecutionState : uint
		{
			SystemRequired = 1u,
			DisplayRequired = 2u,
			Continuous = 2147483648u
		}

		private static int count = 0;

		private static object lockObj = 0;

		public static bool IsStop()
		{
			object obj = SleepState.lockObj;
			bool result;
			lock (obj)
			{
				result = (SleepState.count > 0);
			}
			return result;
		}

		public static void Stop(bool flug)
		{
			object obj = SleepState.lockObj;
			lock (obj)
			{
				if (flug)
				{
					SleepState.count++;
					if (SleepState.count == 1)
					{
						SleepState.SetState(true);
					}
				}
				else
				{
					SleepState.count--;
					if (SleepState.count == 0)
					{
						SleepState.SetState(false);
					}
				}
			}
		}

		private static void SetState(bool stop)
		{
			if (stop)
			{
				SleepState.SetThreadExecutionState((SleepState.ExecutionState)2147483649u);
				return;
			}
			SleepState.SetThreadExecutionState((SleepState.ExecutionState)2147483648u);
		}

		[DllImport("kernel32.dll")]
		private static extern SleepState.ExecutionState SetThreadExecutionState(SleepState.ExecutionState esFlags);
	}
}
