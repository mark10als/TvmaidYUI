using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tvmaid
{
	internal class RecTimer : IDisposable
	{
		private EpgQueue epgQueue = new EpgQueue();

		private DateTime nextEpgTime;

		private Sql sql;

		private bool stop;

		private static RecTimer singleObj;

		public DateTime NextEpgTime
		{
			get
			{
				return this.nextEpgTime;
			}
		}

		private RecTimer()
		{
		}

		public static RecTimer GetInstance()
		{
			if (RecTimer.singleObj == null)
			{
				RecTimer.singleObj = new RecTimer();
			}
			return RecTimer.singleObj;
		}

		public void Dispose()
		{
			this.stop = true;
			if (this.sql != null)
			{
				this.sql.Dispose();
			}
		}

		public bool Stopped()
		{
			return this.stop;
		}

		public void Run()
		{
			DateTime dateTime = DateTime.Now;
			try
			{
				this.sql = new Sql(true);
				this.CreateTunerTask();
				this.SetNextEpgTime();
				goto IL_116;
			}
			catch (Exception ex)
			{
				string expr_34 = "録画管理スレッドの初期化に失敗しました。チューナが使用できない状態です。アプリケーションを終了してください。[詳細] " + ex.Message;
				Log.Write(expr_34);
				Log.Write(1, ex.StackTrace);
				MessageBox.Show(expr_34, AppData.AppName);
				return;
			}
			IL_56:
			try
			{
				if (this.nextEpgTime <= DateTime.Now)
				{
					Log.Write("古い予約と番組情報を削除します.");
					this.CleanupRecord(this.sql);
					this.CleanupEvent(this.sql);
					if (this.nextEpgTime.Hour == DateTime.Now.Hour)
					{
						this.StartEpg();
					}
					this.SetNextEpgTime();
				}
				if (AutoRecord.IsUpdate || dateTime <= DateTime.Now)
				{
					AutoRecord.IsUpdate = false;
					dateTime += new TimeSpan(0, 1, 0);
					this.StartAutoRecord(this.sql);
				}
				Thread.Sleep(1000);
			}
			catch (Exception ex2)
			{
				Log.Write("エラーが発生しました。[詳細] " + ex2.Message);
				Log.Write(1, ex2.StackTrace);
			}
			IL_116:
			if (!this.stop)
			{
				goto IL_56;
			}
		}

		private void SetNextEpgTime()
		{
			int hour = DateTime.Now.Hour;
			string[] arg_34_0 = MainDef.GetInstance()["epg.hour"].Split(new char[]
			{
				','
			}, StringSplitOptions.RemoveEmptyEntries);
			List<int> list = new List<int>();
			string[] array = arg_34_0;
			for (int i = 0; i < array.Length; i++)
			{
				string s = array[i];
				list.Add(s.ToInt());
			}
			list.Sort();
			list.Add(list[0] + 24);
			int num = 0;
			foreach (int current in list)
			{
				if (hour < current)
				{
					num = current;
					break;
				}
			}
			this.nextEpgTime = DateTime.Now.Date.AddHours((double)num);
			Log.Write("次回番組表取得: " + this.nextEpgTime.ToString("M/d HH:mm"));
		}

		private void CreateTunerTask()
		{
			this.sql.Text = "select * from tuner";
			using (DataTable table = this.sql.GetTable())
			{
				while (table.Read())
				{
					Tuner tuner = new Tuner(table);
					Task.Factory.StartNew(delegate
					{
						try
						{
							this.RunTunerTask(tuner);
						}
						catch (Exception ex)
						{
							string expr_24 = "チューナスレッドで回復できないエラーが発生しました。チューナが使用できない状態です。アプリケーションを終了してください。[詳細] " + ex.Message;
							Log.Write(expr_24);
							Log.Write(1, ex.StackTrace);
							MessageBox.Show(expr_24, AppData.AppName);
						}
					}, TaskCreationOptions.AttachedToParent);
				}
			}
		}

		private void RunTunerTask(Tuner tuner)
		{
			Sql sql = new Sql(true);
			while (!this.stop)
			{
				Record nextRecord = Record.GetNextRecord(tuner, sql);
				if (nextRecord != null)
				{
					new RecTask(tuner, nextRecord).Run();
				}
				else if (this.epgQueue.Enable && this.epgQueue.Peek(tuner) != null)
				{
					new EpgTask(tuner, this.epgQueue).Run();
				}
				else
				{
					Thread.Sleep(1000);
				}
			}
		}

		private void CleanupEvent(Sql sql)
		{
			sql.Text = "delete from event where end < {0}".Formatex(new object[]
			{
				(DateTime.Now - new TimeSpan(24, 0, 0)).Ticks
			});
			sql.Execute();
		}

		private void CleanupRecord(Sql sql)
		{
			sql.Text = "delete from record where end < {0}".Formatex(new object[]
			{
				(DateTime.Now - new TimeSpan(1, 0, 0)).Ticks
			});
			sql.Execute();
		}

		private void StartAutoRecord(Sql sql)
		{
			List<AutoRecord> list = new List<AutoRecord>();
			sql.Text = "select * from auto_record where status = 1 and query <> ''";
			using (DataTable table = sql.GetTable())
			{
				while (table.Read())
				{
					list.Add(new AutoRecord(table));
				}
			}
			foreach (AutoRecord current in list)
			{
				List<Event> list2 = new List<Event>();
				int @int = MainDef.GetInstance().GetInt("record.margin.end");
				sql.Text = "select * from event\r\n                        left join record on event.fsid = record.fsid and event.eid = record.eid\r\n                        where\r\n                        event.end > {0} and record.id is null and event.id in ({1})".Formatex(new object[]
				{
					(DateTime.Now + new TimeSpan(0, 0, @int)).Ticks,
					current.Query
				});
				using (DataTable table2 = sql.GetTable())
				{
					while (table2.Read())
					{
						list2.Add(new Event(table2));
					}
				}
				if (list2.Count > 50)
				{
					current.SetEnable(sql, false);
					Log.Write("自動予約 '{0}' を無効にしました。{1} 件以上ヒットします。条件を見なおしてください。".Formatex(new object[]
					{
						current.Name,
						50
					}));
				}
				else
				{
					foreach (Event current2 in list2)
					{
							// mark10als  最小と最大の自動録画時間のチェック
							bool flag2 = false;
							string text = MainDef.GetInstance()["record.minimal.minute"];
							bool flag = text != "";
							if (flag)
							{
								int num = int.Parse(MainDef.GetInstance()["record.minimal.minute"]);
								num = num * 60;
								flag2 = current2.Duration < num;
							}
							else
							{
								flag2 = false;
							}

							bool flag4 = false;
							string text2 = MainDef.GetInstance()["record.maximum.minute"];
							bool flag3 = text2 != "";
							if (flag3)
							{
								int num2 = int.Parse(MainDef.GetInstance()["record.maximum.minute"]);
								num2 = num2 * 60;
								flag4 = current2.Duration > num2;
							}
							else
							{
								flag4 = false;
							}
							int disable = 3;
							bool flag5 = flag2 | flag4;
							if ( flag5 )
							{
								disable = 0;
							}

						new Record
						{

							Status = disable,
							Fsid = current2.Fsid,
							Eid = current2.Eid,
							StartTime = current2.Start,
							Duration = current2.Duration,
							Title = current2.Title,
							Auto = current.Id
						}.Add(sql);
					}
				}
			}
		}

		public void StartEpg()
		{
			if (this.epgQueue.Count == 0)
			{
				this.epgQueue.Enqueue();
				Log.Write("番組表取得を開始します。");
			}
		}

		public void StartEpg(Service service)
		{
			this.epgQueue.Enqueue(service);
		}

		public void StopEpg()
		{
			this.epgQueue.Clear();
			Log.Write("番組表取得を中止しました。");
		}

		public EpgQueue GetEpgQueue()
		{
			return this.epgQueue;
		}
	}
}
