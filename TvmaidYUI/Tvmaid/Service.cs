using System;

namespace Tvmaid
{
	internal class Service
	{
		public int Id = -1;

		public string Driver;

		public int Nid;

		public int Tsid;

		public int Sid;

		public string Name;

		public bool EpgBasic;

		public long Fsid
		{
			get
			{
				return ((long)this.Nid << 32) + (long)((long)this.Tsid << 16) + (long)this.Sid;
			}
			set
			{
				this.Nid = (int)(value >> 32 & 65535L);
				this.Tsid = (int)(value >> 16 & 65535L);
				this.Sid = (int)(value & 65535L);
			}
		}

		public Service()
		{
		}

		public Service(DataTable t)
		{
			this.Init(t);
		}

		private void Init(DataTable t)
		{
			this.Id = t.GetInt("id");
			this.Driver = t.GetStr("driver");
			this.Fsid = t.GetLong("fsid");
			this.Name = t.GetStr("name");
		}

		public Service(Sql sql, long fsid)
		{
			sql.Text = "select * from service where fsid = {0}".Formatex(new object[]
			{
				fsid
			});
			using (DataTable table = sql.GetTable())
			{
				if (!table.Read())
				{
					throw new Exception("サービスが見つかりません。" + fsid);
				}
				this.Init(table);
			}
		}

		public Service(Sql sql, int id)
		{
			sql.Text = "select * from service where id = " + id;
			using (DataTable table = sql.GetTable())
			{
				if (!table.Read())
				{
					throw new Exception("サービスが見つかりません。");
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

		private void _Add(Sql sql)
		{
			if (this.Id == -1)
			{
				this.Id = sql.GetNextId("service");
			}
			sql.Text = "insert into service values({0}, '{1}', {2}, '{3}');".Formatex(new object[]
			{
				this.Id,
				Sql.SqlEncode(this.Driver),
				this.Fsid,
				Sql.SqlEncode(this.Name)
			});
			sql.Execute();
		}
	}
}
