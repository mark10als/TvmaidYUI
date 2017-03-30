using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Tvmaid
{
	internal class EpgTask
	{
		private Tuner tuner;

		private Sql sql;

		private EpgQueue epgQueue;

		public EpgTask(Tuner tuner, EpgQueue epgQueue)
		{
			this.tuner = tuner;
			this.epgQueue = epgQueue;
		}

		private bool StoppdApp()
		{
			return RecTimer.GetInstance().Stopped();
		}

		public void Run()
		{
			try
			{
				SleepState.Stop(true);
				this.sql = new Sql(true);
				this.tuner.Open(false);
				bool flag = false;
				while (!flag)
				{
					Service service = this.epgQueue.Dequeue(this.tuner);
					if (service == null)
					{
						Log.Write(this.tuner.Name + ": このチューナの番組表取得完了。");
						break;
					}
					int wait = EpgWait.GetInstance().GetWait(service.Nid);
					Log.Write("{4}: 番組表を取得しています... {1} ({0}/{3}/{2}s)".Formatex(new object[]
					{
						this.epgQueue.Count,
						service.Name,
						wait,
						service.EpgBasic ? "基本" : "詳細",
						this.tuner.Name
					}));
					this.tuner.SetService(service);
					Stopwatch stopwatch = new Stopwatch();
					stopwatch.Start();
					while (stopwatch.ElapsedMilliseconds < (long)(wait * 1000))
					{
						Record nextRecord = Record.GetNextRecord(this.tuner, this.sql);
						flag = (this.StoppdApp() || !this.epgQueue.Enable || nextRecord != null);
						if (flag)
						{
							Log.Write(this.tuner.Name + ": 中断しました。" + service.Name);
							break;
						}
						Thread.Sleep(1000);
					}
					this.GetEvents(service);
				}
			}
			catch (Exception ex)
			{
				Log.Write(this.tuner.Name + ": 番組表取得に失敗しました。" + ex.Message);
				Log.Write(1, ex.StackTrace);
			}
			finally
			{
				try
				{
					this.sql.Dispose();
					this.tuner.Close();
				}
				catch
				{
				}
				SleepState.Stop(false);
			}
		}

		private void GetEvents(Service service)
		{
			List<Service> list = new List<Service>();
			if (service.EpgBasic)
			{
				this.sql.Text = "select * from service where driver = '{0}' and (fsid >> 32) = {1}".Formatex(new object[]
				{
					service.Driver,
					service.Nid
				});
			}
			else
			{
				this.sql.Text = "select * from service where driver = '{0}' and ((fsid >> 16) & 0xffff) = {1}".Formatex(new object[]
				{
					service.Driver,
					service.Tsid
				});
			}
			using (DataTable table = this.sql.GetTable())
			{
				while (table.Read())
				{
					list.Add(new Service(table));
				}
			}
			foreach (Service current in list)
			{
				this.tuner.GetEvents(this.sql, current);
				Record.UpdateRecordTime(this.sql);
				this.GetLogo(current);
			}
		}

		private void GetLogo(Service s)
		{
			try
			{
				string userPath = Util.GetUserPath("logo");
				if (!Directory.Exists(userPath))
				{
					Directory.CreateDirectory(userPath);
				}
				string path = Path.Combine(userPath, s.Fsid + ".bmp");
				if (!File.Exists(path))
				{
					this.tuner.GetLogo(s, path);
				}
				else
				{
					DateTime t = DateTime.Now + TimeSpan.FromDays(30.0);
					if (File.GetLastWriteTime(path) > t)
					{
						this.tuner.GetLogo(s, path);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Write(1, "ロゴの取得に失敗しました。[詳細] " + ex.Message);
			}
		}
	}
}
