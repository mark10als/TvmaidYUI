using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace Tvmaid
{
	public class Sql : IDisposable
	{
		private IDbCommand command;

		private int transCount;

		public string Text
		{
			get
			{
				return this.command.CommandText;
			}
			set
			{
				this.command.CommandText = value;
			}
		}

		public Sql()
		{
		}

		public Sql(bool open)
		{
			if (open)
			{
				this.Open();
			}
		}

		public void Open()
		{
			SQLiteConnectionStringBuilder sQLiteConnectionStringBuilder = new SQLiteConnectionStringBuilder
			{
				DataSource = Path.Combine(Util.GetUserPath(), "tvmaid-3.db"),
				Version = 3,
				LegacyFormat = false,
				SyncMode = SynchronizationModes.Normal,
				JournalMode = SQLiteJournalModeEnum.Wal
			};
			this.command = new SQLiteCommand();
			this.command.Connection = new SQLiteConnection(sQLiteConnectionStringBuilder.ToString());
			this.command.Connection.Open();
		}

		public void Dispose()
		{
			this.command.Connection.Dispose();
			this.command.Dispose();
		}

		public void Execute()
		{
			this.command.ExecuteNonQuery();
		}

		public object GetData()
		{
			return this.command.ExecuteScalar();
		}

		public DataTable GetTable()
		{
			return new DataTable(this.command.ExecuteReader());
		}

		public void BeginTrans()
		{
			if (this.transCount == 0)
			{
				this.command.Transaction = this.command.Connection.BeginTransaction();
			}
			this.transCount++;
		}

		public void Rollback()
		{
			if (this.transCount == 1)
			{
				this.command.Transaction.Rollback();
			}
			this.transCount--;
		}

		public void Commit()
		{
			if (this.transCount == 1)
			{
				this.command.Transaction.Commit();
			}
			this.transCount--;
		}

		public static string SqlEncode(string text)
		{
			return text.Replace("'", "''");
		}

		public int GetNextId(string table)
		{
			this.Text = "select max(id) as maxid from " + table;
			object data = this.GetData();
			if (DBNull.Value.Equals(data))
			{
				return 0;
			}
			return (int)((long)data) + 1;
		}

		public List<object[]> GetList()
		{
			List<object[]> result;
			using (DataTable table = this.GetTable())
			{
				List<object[]> list = new List<object[]>();
				while (table.Read())
				{
					object[] array = new object[table.FieldCount];
					table.GetValues(array);
					list.Add(array);
				}
				result = list;
			}
			return result;
		}
	}
}
