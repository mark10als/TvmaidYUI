using System;
using System.Collections.Generic;

namespace Tvmaid
{
	internal class Record
	{
		public enum RecStatus
		{
			Enable = 1,
			EventMode,
			Duplication = 32,
			Recoding = 64,
			Complete = 128
		}

		public int Id = -1;

		public long Fsid;

		public int Eid = -1;

		public DateTime StartTime = DateTime.Now;

		public int Duration;

		public int Auto = -1;

		public int Status = 3;

		public int rec_view = 0;

		public string Title = "未定";

		public string TunerName = "";

		public DateTime EndTime
		{
			get
			{
				long num = (long)this.Duration;
				return new DateTime(this.StartTime.Ticks + num * 10L * 1000L * 1000L);
			}
		}

		public Record()
		{
		}

		public Record(DataTable t)
		{
			this.Init(t);
		}

		private void Init(DataTable t)
		{
			this.Id = t.GetInt("id");
			this.Fsid = t.GetLong("fsid");
			this.Eid = t.GetInt("eid");
			this.StartTime = new DateTime(t.GetLong("start"));
			this.Duration = t.GetInt("duration");
			this.Auto = t.GetInt("auto");
			this.Status = t.GetInt("status");
			this.rec_view = t.GetInt("rec_view");
			this.Title = t.GetStr("title");
			this.TunerName = t.GetStr("tuner");
		}

		public Record(Sql sql, int id)
		{
			sql.Text = "select * from record where id = " + id;
			using (DataTable table = sql.GetTable())
			{
				if (!table.Read())
				{
					throw new Exception("予約が見つかりません。");
				}
				this.Init(table);
			}
		}

		public void Add(Sql sql)
		{
			if (this.EndTime < DateTime.Now)
			{
				throw new Exception("過去の番組は予約できません。");
			}
			if (this.TunerName != "")
			{
				sql.Text = "select name from tuner\r\n                        where\r\n                        name = '{0}'\r\n                        and\r\n                        driver in (select driver from service where fsid = {1})".Formatex(new object[]
				{
					Sql.SqlEncode(this.TunerName),
					this.Fsid
				});
				if ((string)sql.GetData() == null)
				{
					throw new Exception("指定されたチューナ間違っています。チューナを確認してください。");
				}
			}
			else
			{
				this.GetFreeTuner(sql);
			}
			bool arg_90_0 = this.Id == -1;
			this.AddRec(sql);
			if (arg_90_0)
			{
				// mark10als  最小と最大の自動録画時間のチェックで無効なら[無効]と表示
				// Log.Write("予約しました。" + this.Title);
				if (this.Status == 0)
				{
					Log.Write("予約しました。[無効]" + this.Title);
				}
				else
				{
					Log.Write("予約しました。" + this.Title);
				}
				return;
			}
			Log.Write("予約を変更しました。" + this.Title);
		}

		public static void ResetTuner(Sql sql)
		{
			try
			{
				sql.BeginTrans();
				sql.Text = "update record set tuner = '' where status & {0} = 0 and status & {1} > 0".Formatex(new object[]
				{
					64,
					1
				});
				sql.Execute();
				List<Record> list = new List<Record>();
				sql.Text = "select * from record where tuner = ''";
				using (DataTable table = sql.GetTable())
				{
					while (table.Read())
					{
						list.Add(new Record(table));
					}
				}
				foreach (Record current in list)
				{
					current.GetFreeTuner(sql);
					sql.Text = "update record set tuner = '{0}' where id = {1}".Formatex(new object[]
					{
						Sql.SqlEncode(current.TunerName),
						current.Id
					});
					sql.Execute();
				}
				sql.Commit();
				Record.SetDuplication(sql);
			}
			catch
			{
				sql.Rollback();
				throw;
			}
		}

		private void GetFreeTuner(Sql sql)
		{
			// mark10als
			int @int = MainDef.GetInstance().GetInt("record.margin.start");
			int @int2 = MainDef.GetInstance().GetInt("record.margin.end");
			string text = MainDef.GetInstance()["record.margin.overlap"];
			if (text != "0")
			{
				sql.Text = "select name from tuner\r\n                    where\r\n                    driver in (select driver from service where fsid = {0}) \r\n                    and\r\n                    name not in (select tuner from record where {1} < end and {2} > start and status & {3})\r\n                    order by id".Formatex(new object[]
				{
					this.Fsid,
					( this.StartTime.Ticks - @int ),
					( this.EndTime.Ticks - @int2 ),
					1
				});
			}
			else
			{
				sql.Text = "select name from tuner\r\n                    where\r\n                    driver in (select driver from service where fsid = {0}) \r\n                    and\r\n                    name not in (select tuner from record where {1} < end and {2} > start and status & {3})\r\n                    order by id".Formatex(new object[]
				{
					this.Fsid,
					this.StartTime.Ticks,
					this.EndTime.Ticks,
					1
				});
			}
			object data = sql.GetData();
			if (data != null)
			{
				this.TunerName = (string)data;
				return;
			}
			sql.Text = "select name from tuner\r\n                        where\r\n                        driver in (select driver from service where fsid = {0}) \r\n                        order by id".Formatex(new object[]
			{
				this.Fsid
			});
			this.TunerName = (string)sql.GetData();
		}

		public void Remove(Sql sql, bool removeOnly = false)
		{
			sql.Text = "delete from record where id = " + this.Id;
			sql.Execute();
			if (!removeOnly)
			{
				Record.SetDuplication(sql);
				Log.Write("予約を取り消しました。" + this.Title);
			}
		}

		public static Record GetNextRecord(Tuner tuner, Sql sql)
		{
			int @int = MainDef.GetInstance().GetInt("record.margin.start");
			DateTime dateTime = DateTime.Now + new TimeSpan(0, 0, @int);
			sql.Text = "select * from record where tuner = '{0}' and status & {2} and start <= {1} and end > {2} order by start limit 1".Formatex(new object[]
			{
				tuner.Name,
				dateTime.Ticks,
				1
			});
			using (DataTable table = sql.GetTable())
			{
				if (table.Read())
				{
					return new Record(table);
				}
			}
			return null;
		}

		public static void UpdateRecordTime(Sql sql)
		{
			sql.Text = "update record set\r\n                        start = (select start from event where record.fsid = event.fsid and record.eid = event.eid),\r\n                        end = (select end from event where record.fsid = event.fsid and record.eid = event.eid),\r\n                        duration = (select duration from event where record.fsid = event.fsid and record.eid = event.eid)\r\n                        where \r\n                        status & {0} > 0\r\n                        and\r\n                        id in (\r\n                        select record.id from record left join event\r\n                        on record.fsid = event.fsid and record.eid = event.eid\r\n                        where record.start <> event.start or record.duration <> event.duration)".Formatex(new object[]
			{
				2
			});
			sql.Execute();
			Record.SetDuplication(sql);
		}

		private void SetStatus(Sql sql, Record.RecStatus status, bool flag)
		{
			if (flag)
			{
				sql.Text = "update record set status = status | {0} where id = {1}".Formatex(new object[]
				{
					(int)status,
					this.Id
				});
			}
			else
			{
				sql.Text = "update record set status = status & ~{0} where id = {1}".Formatex(new object[]
				{
					(int)status,
					this.Id
				});
			}
			sql.Execute();
		}

		public void SetEnable(Sql sql, bool flag)
		{
			this.SetStatus(sql, Record.RecStatus.Enable, flag);
			Record.SetDuplication(sql);
		}

		public void SetRecoding(Sql sql, bool flag)
		{
			this.SetStatus(sql, Record.RecStatus.Recoding, flag);
		}

		public void SetComplete(Sql sql)
		{
			this.SetEnable(sql, false);
			this.SetRecoding(sql, false);
			this.SetStatus(sql, Record.RecStatus.Complete, true);
		}

		public static void SetDuplication(Sql sql)
		{
			try
			{
				sql.BeginTrans();
				sql.Text = "update record set status = status & ~{0}".Formatex(new object[]
				{
					32
				});
				sql.Execute();
				sql.Text = "update record set status = status | {0}\r\n                        where id in (\r\n                        select r1.id from record r1\r\n                        join record r2\r\n                        on r1.id <> r2.id\r\n                        where\r\n                        r1.tuner = r2.tuner\r\n                        and r1.start < r2.end\r\n                        and r1.end > r2.start\r\n                        and r1.status & {1}\r\n                        and r2.status & {1})".Formatex(new object[]
				{
					32,
					1
				});
				sql.Execute();
				sql.Commit();
			}
			catch
			{
				sql.Rollback();
				throw;
			}
		}

		public void AddRec(Sql sql)
		{
			sql.BeginTrans();
			try
			{
				this._AddRec(sql);
			}
			finally
			{
				sql.Commit();
			}
		}

		private void _AddRec(Sql sql)
		{
			if (this.Id == -1)
			{
				this.Id = sql.GetNextId("record");
			}
			else
			{
				this.Remove(sql, true);
			}
			sql.Text = "insert into record values(\r\n                            {0}, {1}, {2},\r\n                            {3}, {4}, {5},\r\n                            {6}, {7}, {8}, '{9}', '{10}');".Formatex(new object[]
			{
				this.Id,
				this.Fsid,
				this.Eid,
				this.StartTime.Ticks,
				this.EndTime.Ticks,
				this.Duration,
				this.Auto,
				this.Status,
				this.rec_view,	// mark10als
				Sql.SqlEncode(this.Title),
				Sql.SqlEncode(this.TunerName)
			});
			sql.Execute();
			Record.SetDuplication(sql);
		}
	}
}
