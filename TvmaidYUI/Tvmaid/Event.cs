using System;

namespace Tvmaid
{
	internal class Event
	{
		public int Id = -1;

		public long Fsid;

		public int Eid;

		public DateTime Start;

		public int Duration;

		public string Title;

		public string Desc;

		public string LongDesc;

		public long Genre;

		public DateTime End
		{
			get
			{
				long num = (long)this.Duration;
				return new DateTime(this.Start.Ticks + num * 10L * 1000L * 1000L);
			}
		}

		public string GenreText
		{
			get
			{
				return GenreConv.GetInstance().GetText(this.Genre);
			}
		}

		public int Week
		{
			get
			{
				return (int)this.Start.DayOfWeek;
			}
		}

		public Event()
		{
		}

		public Event(DataTable t)
		{
			this.Init(t);
		}

		private void Init(DataTable t)
		{
			this.Id = t.GetInt("id");
			this.Fsid = t.GetLong("fsid");
			this.Eid = t.GetInt("eid");
			this.Start = new DateTime(t.GetLong("start"));
			this.Duration = t.GetInt("duration");
			this.Title = t.GetStr("title");
			this.Desc = t.GetStr("desc");
			this.LongDesc = t.GetStr("longdesc");
			this.Genre = t.GetLong("genre");
		}

		public Event(Sql sql, long fsid, int eid)
		{
			sql.Text = "select * from event where fsid = {0} and eid = {1}".Formatex(new object[]
			{
				fsid,
				eid
			});
			using (DataTable table = sql.GetTable())
			{
				if (!table.Read())
				{
					throw new Exception("番組が見つかりません。");
				}
				this.Init(table);
			}
		}

		public Event(Sql sql, int id)
		{
			sql.Text = "select * from event where id = " + id;
			using (DataTable table = sql.GetTable())
			{
				if (!table.Read())
				{
					throw new Exception("番組が見つかりません。");
				}
				this.Init(table);
			}
		}

		public void Add(Sql sql)
		{
			sql.BeginTrans();
			try
			{
				this._Add(sql);
			}
			finally
			{
				sql.Commit();
			}
		}

		private void Remove(Sql sql)
		{
			sql.Text = "delete from event where id = " + this.Id;
			sql.Execute();
		}

		private void _Add(Sql sql)
		{
			if (this.Id == -1)
			{
				this.Id = sql.GetNextId("event");
			}
			else
			{
				this.Remove(sql);
			}
			TextConv instance = TextConv.GetInstance();
			sql.Text = "insert into event values(\r\n                        {0}, {1}, {2}, \r\n                        {3}, {4}, {5},\r\n                        '{6}', '{7}', '{8}', {9}, {10},\r\n                        {11}, '{12}');".Formatex(new object[]
			{
				this.Id,
				this.Fsid,
				this.Eid,
				this.Start.Ticks,
				this.End.Ticks,
				this.Duration,
				Sql.SqlEncode(instance.Convert(this.Title)),
				Sql.SqlEncode(instance.Convert(this.Desc)),
				Sql.SqlEncode(instance.Convert(this.LongDesc)),
				this.Genre,
				0,
				this.Week,
				this.GenreText
			});
			sql.Execute();
		}
	}
}
