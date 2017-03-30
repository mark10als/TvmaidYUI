using System;
using System.Data;

namespace Tvmaid
{
	public class DataTable : IDisposable
	{
		protected IDataReader reader;

		public int FieldCount
		{
			get
			{
				return this.reader.FieldCount;
			}
		}

		public DataTable(IDataReader dr)
		{
			this.reader = dr;
		}

		public string GetStr(int i)
		{
			return (string)this.reader[i];
		}

		public int GetInt(int i)
		{
			return (int)((long)this.reader[i]);
		}

		public long GetLong(int i)
		{
			return (long)this.reader[i];
		}

		public string GetStr(string name)
		{
			return (string)this.reader[name];
		}

		public int GetInt(string name)
		{
			return (int)((long)this.reader[name]);
		}

		public long GetLong(string name)
		{
			return (long)this.reader[name];
		}

		public bool Read()
		{
			return this.reader.Read();
		}

		public void Dispose()
		{
			this.reader.Dispose();
		}

		public bool IsNull(int i)
		{
			return this.reader.IsDBNull(i);
		}

		internal void GetValues(object[] values)
		{
			this.reader.GetValues(values);
		}
	}
}
