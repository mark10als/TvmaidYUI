using System;

namespace Tvmaid
{
	internal class AutoRecord
	{
		public enum AutoRecStatus
		{
			Enable = 1
		}

		public static bool IsUpdate;

		public int Id = -1;

		public string Name = "未定";

		public string Folder = "";

		public string Query = "";

		public string Option = "";

		public int Status = 34;

		public AutoRecord()
		{
		}

		public AutoRecord(DataTable t)
		{
			this.Init(t);
		}

		private void Init(DataTable t)
		{
			this.Id = t.GetInt("id");
			this.Name = t.GetStr("name");
			this.Folder = t.GetStr("folder");
			this.Query = t.GetStr("query");
			this.Option = t.GetStr("option");
			this.Status = t.GetInt("status");
		}

		public AutoRecord(Sql sql, int id)
		{
			sql.Text = "select * from auto_record where id = " + id;
			using (DataTable table = sql.GetTable())
			{
				if (!table.Read())
				{
					throw new Exception("自動予約が見つかりません。");
				}
				this.Init(table);
			}
		}

		public void Remove(Sql sql)
		{
			sql.Text = "delete from record where auto = " + this.Id;
			sql.Execute();
			sql.Text = "delete from auto_record where id = " + this.Id;
			sql.Execute();
			Record.SetDuplication(sql);
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

		public void _Add(Sql sql)
		{
			if (this.Name == "")
			{
				throw new Exception("名前を入力してください。");
			}
			if (this.Id == -1)
			{
				sql.Text = "select id from auto_record where name = '{0}'".Formatex(new object[]
				{
					this.Name
				});
				if (sql.GetData() != null)
				{
					throw new Exception("同じ名前の自動予約があります。登録できませんでした。 - " + this.Name);
				}
				this.Id = sql.GetNextId("auto_record");
			}
			else
			{
				sql.Text = "select id from auto_record where name = '{0}' and not id = {1}".Formatex(new object[]
				{
					this.Name,
					this.Id
				});
				if (sql.GetData() != null)
				{
					throw new Exception("同じ名前の自動予約があります。登録できませんでした。 - " + this.Name);
				}
				this.Remove(sql);
			}
			sql.Text = "insert into auto_record values(\r\n                            {0}, '{1}', '{2}', '{3}', '{4}', {5});".Formatex(new object[]
			{
				this.Id,
				Sql.SqlEncode(this.Name),
				Sql.SqlEncode(this.Folder),
				Sql.SqlEncode(this.Query),
				Sql.SqlEncode(this.Option),
				this.Status
			});
			sql.Execute();
			AutoRecord.IsUpdate = true;
		}

		public void SetEnable(Sql sql, bool enable)
		{
			this.Status &= 255;
			this.Status += (enable ? 1 : 0);
			this.Add(sql);
		}
	}
}
