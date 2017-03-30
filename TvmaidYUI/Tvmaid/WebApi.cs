using System;
using System.Net;
using System.Runtime.InteropServices;

namespace Tvmaid
{
	internal class WebApi : WebApiBase
	{
		public WebApi(HttpListenerRequest req, HttpListenerResponse res) : base(req, res)
		{
		}

		public void GetTable()
		{
			using (Sql sql = new Sql(true))
			{
				sql.Text = base.GetQuery("sql");
				this.ret.data1 = sql.GetList();
			}
		}

		public void RemoveRecord()
		{
			string query = base.GetQuery("id");
			using (Sql sql = new Sql(true))
			{
				Record record = new Record(sql, query.ToInt());
				if (record.Auto == -1 || record.EndTime < DateTime.Now)
				{
					record.Remove(sql, false);
				}
				else
				{
					record.SetEnable(sql, false);
				}
			}
		}

		public void AddRecord()
		{
			Record record = this.SetRecordQuery();
			using (Sql sql = new Sql(true))
			{
				if ((record.Status & 2) > 0)
				{
					Event @event = new Event(sql, record.Fsid, record.Eid);
					record.Fsid = @event.Fsid;
					record.Eid = @event.Eid;
					record.StartTime = @event.Start;
					record.Duration = @event.Duration;
					record.Title = base.GetQuery("title", @event.Title);
				}
				record.Add(sql);
			}
			this.ret.data1 = record.Id;
		}

		private Record SetRecordQuery()
		{
			int query = base.GetQuery("id", -1);
			Record record;
			if (query == -1)
			{
				record = new Record();
			}
			else
			{
				using (Sql sql = new Sql(true))
				{
					record = new Record(sql, query);
				}
			}
			record.Fsid = base.GetQuery("fsid", record.Fsid);
			record.Eid = base.GetQuery("eid", record.Eid);
			record.StartTime = new DateTime(base.GetQuery("start", record.StartTime.Ticks));
			record.Duration = base.GetQuery("duration", record.Duration);
			record.Title = base.GetQuery("title", record.Title);
			record.TunerName = base.GetQuery("tuner", record.TunerName);
			int num = base.GetQuery("status", -1);
			if (num != -1)
			{
				num &= 3;
				record.Status &= 252;
				record.Status |= num;
			}
			return record;
		}

		public void ResetRecordTuner()
		{
			using (Sql sql = new Sql(true))
			{
				Record.ResetTuner(sql);
			}
		}

		public void RemoveAutoRecord()
		{
			string query = base.GetQuery("id");
			using (Sql sql = new Sql(true))
			{
				new AutoRecord(sql, query.ToInt()).Remove(sql);
			}
		}

		public void AddAutoRecord()
		{
			AutoRecord autoRecord = this.SetAutoRecordQuery();
			using (Sql sql = new Sql(true))
			{
				autoRecord.Add(sql);
			}
			this.ret.data1 = autoRecord.Id;
		}

		private AutoRecord SetAutoRecordQuery()
		{
			int query = base.GetQuery("id", -1);
			AutoRecord autoRecord;
			if (query == -1)
			{
				autoRecord = new AutoRecord();
			}
			else
			{
				using (Sql sql = new Sql(true))
				{
					autoRecord = new AutoRecord(sql, query);
				}
			}
			autoRecord.Name = base.GetQuery("name", autoRecord.Name);
			autoRecord.Folder = base.GetQuery("folder", autoRecord.Folder);
			autoRecord.Query = base.GetQuery("query", autoRecord.Query);
			autoRecord.Option = base.GetQuery("option", autoRecord.Option);
			autoRecord.Status = base.GetQuery("status", autoRecord.Status);
			return autoRecord;
		}

		public void GetRecordFolderFree()
		{
			ulong num;
			ulong num2;
			ulong num3;
			if (WebApi.GetDiskFreeSpaceEx(MainDef.GetInstance()["record.folder"], out num, out num2, out num3))
			{
				this.ret.data1 = num;
				return;
			}
			throw new Exception("録画フォルダの空き容量が取得できませんでした。");
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

		public void SetUserEpg()
		{
			int num = base.GetQuery("id").ToInt();
			string[] array = base.GetQuery("fsid").Split(new char[]
			{
				','
			}, StringSplitOptions.RemoveEmptyEntries);
			using (Sql sql = new Sql(true))
			{
				try
				{
					sql.BeginTrans();
					sql.Text = "delete from user_epg where id = " + num;
					sql.Execute();
					for (int i = 0; i < array.Length; i++)
					{
						long num2 = array[i].ToLong();
						sql.Text = "insert into user_epg values({0}, {1}, {2});".Formatex(new object[]
						{
							num,
							num2,
							i
						});
						sql.Execute();
					}
					sql.Commit();
				}
				catch
				{
					sql.Rollback();
					throw;
				}
			}
		}
	}
}
