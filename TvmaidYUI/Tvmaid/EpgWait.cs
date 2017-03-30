using System;
using System.Collections.Generic;

namespace Tvmaid
{
	internal class EpgWait
	{
		private PairList list;
		private int default_wait;
		private Dictionary<int, int> wait_map;
		private Dictionary<int, int> sid_map;

		private static EpgWait singleObj;

		public static EpgWait GetInstance()
		{
			if (EpgWait.singleObj == null)
			{
				EpgWait.singleObj = new EpgWait();
			}
			return EpgWait.singleObj;
		}

		private EpgWait()
		{
			this.list = new PairList(Util.GetUserPath("epgwait.def"));
			this.list.Load();
			default_wait = 60;
			this.wait_map = new Dictionary<int, int>();
			this.sid_map = new Dictionary<int, int>();
			this.Check();
		}

		private void Check()
		{
			// default=wait or nid=wait or nid=wait,sid
			foreach (KeyValuePair<string, string> current in this.list)
			{
				int n;
				if (!current.Key.Equals("default"))
				{
					int nid;
					if (!int.TryParse(current.Key, out nid))
					{
						throw new Exception("番組表取得待ち時間のNID設定値が不正です。" + current.Key);
					}

					string wait = current.Value;
					n = wait.IndexOf(',');
					if (n != -1)
					{
						wait = current.Value.Substring(0, n);
						if (n + 1 < current.Value.Length)
						{
							string sid = current.Value.Substring(n + 1);
							if (!int.TryParse(sid, out n))
							{
								throw new Exception("番組表取得待ち時間の取得サービス(SID)設定値が不正です。" + sid);
							}
							this.sid_map[nid] = n;
						}
					}

					if (!int.TryParse(wait, out n))
					{
						throw new Exception("番組表取得待ち時間の設定値が不正です。" + wait);
					}
					this.wait_map[nid] = n;
				}
				else
				{
					if (!int.TryParse(current.Value, out n))
					{
						throw new Exception("番組表取得待ち時間のデフォルト設定値が不正です。" + current.Value);
					}
					default_wait = n;
				}
			}
		}

		public int GetWait(int nid)
		{
			if (wait_map.ContainsKey(nid))
			{
				return wait_map[nid];
			}
			return default_wait;
		}

		public int GetSid(int nid)
		{
			if (sid_map.ContainsKey(nid))
			{
				return sid_map[nid];
			}
			return -1;
		}
	}
}
