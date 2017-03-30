using System;
using System.Collections.Generic;
using System.Text;

namespace Tvmaid
{
	internal class TextConv
	{
		private PairList list;

		private static TextConv singleObj;

		public static TextConv GetInstance()
		{
			if (TextConv.singleObj == null)
			{
				TextConv.singleObj = new TextConv();
			}
			return TextConv.singleObj;
		}

		private TextConv()
		{
			this.list = new PairList(Util.GetUserPath("convert.def"));
			this.list.Load();
		}

		public string Convert(string src)
		{
			StringBuilder stringBuilder = new StringBuilder(src);
			foreach (KeyValuePair<string, string> current in this.list)
			{
				stringBuilder = stringBuilder.Replace(current.Key, current.Value);
			}
			return stringBuilder.ToString();
		}
	}
}
