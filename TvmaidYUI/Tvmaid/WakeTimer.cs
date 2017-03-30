using System;
using System.Runtime.InteropServices;

namespace Tvmaid
{
	internal class WakeTimer
	{
		private IntPtr handle = IntPtr.Zero;

		public void SetTimer(DateTime wake)
		{
			this.Cancel();
			this.handle = WakeTimer.CreateWaitableTimer(IntPtr.Zero, true, "WaitableTimer");
			if (this.handle.ToInt32() == 0)
			{
				throw new Exception("復帰タイマーの設定に失敗しました。エラーコード = " + Marshal.GetLastWin32Error().ToString());
			}
			long num = (wake - DateTime.Now).Ticks * -1L;
			if (!WakeTimer.SetWaitableTimer(this.handle, ref num, 0, IntPtr.Zero, IntPtr.Zero, true))
			{
				throw new Exception("復帰タイマーの設定に失敗しました。エラーコード = " + Marshal.GetLastWin32Error().ToString());
			}
		}

		public void Cancel()
		{
			if (this.handle != IntPtr.Zero)
			{
				WakeTimer.CancelWaitableTimer(this.handle);
				WakeTimer.CloseHandle(this.handle);
				this.handle = IntPtr.Zero;
			}
		}

		[DllImport("kernel32.dll")]
		private static extern IntPtr CreateWaitableTimer(IntPtr lpTimerAttributes, bool bManualReset, string lpTimerName);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetWaitableTimer(IntPtr hTimer, [In] ref long pDueTime, int lPeriod, IntPtr pfnCompletionRoutine, IntPtr lpArgToCompletionRoutine, bool fResume);

		[DllImport("kernel32.dll")]
		private static extern bool CancelWaitableTimer(IntPtr hTimer);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool CloseHandle(IntPtr hObject);
	}
}
