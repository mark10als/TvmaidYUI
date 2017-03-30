using System;

namespace Tvmaid
{
	internal class Result
	{
		public int Id = -1;

		public string Title;

		public string ServiceName;

		public string File;

		public DateTime Start;

		public DateTime End;

		public DateTime SchStart;

		public DateTime SchEnd;

		public int Code;

		public int Error;

		public int Drop;

		public int Scramble;

		public string Message = "";

		public string Desc = "";

		public string LongDesc = "";

		public string GenreText = "";

		public void Remove(Sql sql)
		{
			sql.Text = "delete from result where id = " + this.Id;
			sql.Execute();
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
				this.Id = sql.GetNextId("result");
			}
			sql.Text = "insert into result values(\r\n                            {0}, '{1}', '{2}', '{3}',\r\n                            {4}, {5}, {6}, {7},\r\n                            {8}, {9}, {10}, {11}, '{12}',\r\n                            '{13}', '{14}', '{15}');".Formatex(new object[]
			{
				this.Id,
				Sql.SqlEncode(this.Title),
				Sql.SqlEncode(this.ServiceName),
				Sql.SqlEncode(this.File),
				this.Start.Ticks,
				this.End.Ticks,
				this.SchStart.Ticks,
				this.SchEnd.Ticks,
				this.Code,
				this.Error,
				this.Drop,
				this.Scramble,
				Sql.SqlEncode(this.Message),
				Sql.SqlEncode(this.Desc),
				Sql.SqlEncode(this.LongDesc),
				Sql.SqlEncode(this.GenreText)
			});
			sql.Execute();
		}
	}
}
