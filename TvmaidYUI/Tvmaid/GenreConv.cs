using System;
using System.Collections.Generic;

namespace Tvmaid
{
	internal class GenreConv
	{
		private Dictionary<int, string> genres1 = new Dictionary<int, string>();

		private Dictionary<int, string> genres2 = new Dictionary<int, string>();

		private static GenreConv singleObj;

		public static GenreConv GetInstance()
		{
			if (GenreConv.singleObj == null)
			{
				GenreConv.singleObj = new GenreConv();
			}
			return GenreConv.singleObj;
		}

		private GenreConv()
		{
			this.LoadFile(this.genres1, "genre1.def");
			this.LoadFile(this.genres2, "genre2.def");
		}

		public void LoadFile(Dictionary<int, string> dic, string file)
		{
			PairList expr_0B = new PairList(Util.GetUserPath(file));
			expr_0B.Load();
			foreach (KeyValuePair<string, string> current in expr_0B)
			{
				int key = Convert.ToInt32(current.Key, 16);
				dic[key] = current.Value;
			}
		}

		public string GetText(long data)
		{
			string text = "";
			for (int i = 0; i < 4; i++)
			{
				int num = (int)(data >> i * 8 & 255L);
				if (num != 255 && this.genres1.ContainsKey(num >> 4))
				{
					string text2 = this.genres1[num >> 4];
					if (this.genres2.ContainsKey(num))
					{
						text = string.Concat(new string[]
						{
							text,
							text2,
							"/",
							this.genres2[num],
							"\n"
						});
					}
					else
					{
						text = text + text2 + "/\n";
					}
				}
			}
			return text;
		}
	}
}
