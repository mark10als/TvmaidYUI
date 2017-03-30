using System;
using System.Collections.Generic;

namespace Tvmaid
{
	public class Log
	{
		private List<string> list = new List<string>();

		private int level;

		private static Log singleObj;

		public static int Level
		{
			set
			{
				Log.GetInstance().SetLevel(value);
			}
		}

		private Log()
		{
		}

		public static Log GetInstance()
		{
			if (Log.singleObj == null)
			{
				Log.singleObj = new Log();
			}
			return Log.singleObj;
		}

		public void WriteLog(int level, string text)
		{
			if (level > this.level)
			{
				return;
			}
			List<string> obj = this.list;
			lock (obj)
			{
				text = DateTime.Now.ToLongTimeString() + " " + text;
				this.list.Add(text);
			}
		}

		public void SetLevel(int level)
		{
			this.level = level;
		}

		public static void Write(string text)
		{
			Log.GetInstance().WriteLog(0, text);
		}

		public static void Write(int level, string text)
		{
			Log.GetInstance().WriteLog(level, text);
		}

		public string ReadLog()
		{
			List<string> obj = this.list;
			string result;
			lock (obj)
			{
				if (this.list.Count == 0)
				{
					result = null;
				}
				else
				{
					string arg_3A_0 = this.list[0];
					this.list.RemoveAt(0);
					result = arg_3A_0;
				}
			}
			return result;
		}

		public static string Read()
		{
			return Log.GetInstance().ReadLog();
		}
	}
}
