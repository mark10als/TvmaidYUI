using System;
using System.Windows.Forms;

namespace Tvmaid
{
	public class SleepMan : IDisposable
	{
		private WakeTimer wake = new WakeTimer();

		private Timer sleepTimer = new Timer();

		public SleepMan()
		{
			this.sleepTimer.Tick += new EventHandler(this.Tick);
			this.sleepTimer.Interval = 1000;
		}

		public void Dispose()
		{
			this.wake.Cancel();
		}

		private DateTime GetNextTime()
		{
			DateTime nextEpgTime = RecTimer.GetInstance().NextEpgTime;
			long nextRecordTime = this.GetNextRecordTime();
			DateTime dateTime;
			if (nextRecordTime == -1L)
			{
				dateTime = new DateTime(9999, 1, 1);
			}
			else
			{
				dateTime = new DateTime(nextRecordTime);
			}
			if (!(dateTime < nextEpgTime))
			{
				return nextEpgTime;
			}
			return dateTime;
		}

		public void OnSuspend()
		{
			Log.Write("スリープ状態に入ります。");
			DateTime dateTime = this.GetNextTime();
			dateTime -= new TimeSpan(0, 2, 0);
			if (dateTime < DateTime.Now)
			{
				dateTime = DateTime.Now + new TimeSpan(0, 0, 30);
			}
			this.wake.SetTimer(dateTime);
			Log.Write("復帰タイマーを次の時間にセットしました。" + dateTime.ToString("MM/dd HH:mm:ss"));
		}

		public void OnResume()
		{
			Log.Write("スリープから復帰しました。");
			this.wake.Cancel();
			if (MainDef.GetInstance()["autosleep"] != "on")
			{
				return;
			}
			if (this.GetNextTime() - DateTime.Now < new TimeSpan(0, 3, 0))
			{
				this.sleepTimer.Start();
				Log.Write("スリープモードで自動復帰したため、録画後再スリープします。");
				return;
			}
			Log.Write("自動復帰でないため、再スリープしません。");
		}

		private void Tick(object sender, EventArgs e)
		{
			if (SleepState.IsStop())
			{
				return;
			}
			EpgQueue epgQueue = RecTimer.GetInstance().GetEpgQueue();
			if (epgQueue.Enable && epgQueue.Count > 0)
			{
				return;
			}
			if (this.GetNextTime() - DateTime.Now > new TimeSpan(0, 10, 0))
			{
				this.Sleep();
			}
		}

		private void Sleep()
		{
			this.sleepTimer.Stop();
			if (new SleepCountdown(this.GetNextTime()).ShowDialog() == DialogResult.OK)
			{
				this.OnSuspend();
				Application.SetSuspendState(PowerState.Suspend, false, false);
				return;
			}
			Log.Write("スリープをキャンセルしました。");
		}

		private long GetNextRecordTime()
		{
			long result;
			using (Sql sql = new Sql(true))
			{
				sql.Text = "select start from record where status & {0} and start > {1} order by start".Formatex(new object[]
				{
					1,
					DateTime.Now.Ticks
				});
				using (DataTable table = sql.GetTable())
				{
					result = (table.Read() ? table.GetLong(0) : -1L);
				}
			}
			return result;
		}

		public void SetSleep()
		{
			Log.Write("ユーザの手動操作で、スリープモードにしました。");
			this.sleepTimer.Start();
			this.Tick(null, null);
		}
	}
}
