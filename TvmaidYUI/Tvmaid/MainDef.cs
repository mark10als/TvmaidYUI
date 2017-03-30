using System;
using System.IO;

namespace Tvmaid
{
	public class MainDef
	{
		private PairList list;

		private static MainDef singleObj;

		public string this[string key]
		{
			get
			{
				return this.list[key];
			}
			set
			{
				this.list[key] = value;
			}
		}

		public static MainDef GetInstance()
		{
			if (MainDef.singleObj == null)
			{
				MainDef.singleObj = new MainDef();
			}
			return MainDef.singleObj;
		}

		public int GetInt(string key)
		{
			return this.list.GetInt(key);
		}

		public float GetFloat(string key)
		{
			return this.list.GetFloat(key);
		}

		private void SetDefault(string key, string defaultVal)
		{
			if (!this.list.IsDefined(key) || this.list[key] == "")
			{
				this.list[key] = defaultVal;
			}
		}

		private MainDef()
		{
			this.list = new PairList(Util.GetUserPath("main.def"));
			this.list.Load();
		}

		public void Save()
		{
			this.Check();
			this.list.Save();
		}

		public void Check()
		{
			this.Check1();
			this.Check2();
			this.Check3();
			this.Check4();
			this.Check5();
			this.Check6();
			// mark10als
			this.Check7(); // 予備録画フォルダの設定
			this.Check8(); // 最小と最大の自動録画時間の設定
		}

		// mark10als
		private void Check8() // 最小と最大の自動録画時間の設定
		{
			if (this.list.IsDefined("record.minimal.minute"))
			{
				string text = this.list["record.minimal.minute"];
				int num;
				bool flag4 = !int.TryParse(text, out num);
				if (flag4)
				{
					throw new Exception("最小自動録画時間が不正な値か、設定されていません。" + text);
				}
				Log.Write("最小自動録画時間: {0} 分以上".Formatex(new object[]
				{
					num
				}));
			}
			else
			{
				Log.Write("最小自動録画時間: なし");
			}

			if (this.list.IsDefined("record.maximum.minute"))
			{
				string text2 = this.list["record.maximum.minute"];
				int num;
				bool flag4 = !int.TryParse(text2, out num);
				if (flag4)
				{
					throw new Exception("最大自動録画時間が不正な値か、設定されていません。" + text2);
				}
				Log.Write("最大自動録画時間: {0} 分未満".Formatex(new object[]
				{
					num
				}));
			}
			else
			{
				Log.Write("最大自動録画時間: なし");
			}
		}

		private void Check7() // 予備録画フォルダの設定
		{
			if (this.list.IsDefined("record.folder.spare"))
			{
				string text = this.list["record.folder.spare"];
				if (!Directory.Exists(text))
				{
					throw new Exception("予備録画フォルダが見つかりません。設定を確認してください。");
				}
				string text2 = this.list["record.folder.spare.use"];
				ulong num;
				bool flag4 = !ulong.TryParse(text2, out num);
				if (flag4)
				{
					throw new Exception("予備録画フォルダ使用条件が不正な値か、設定されていません。" + text2);
				}
				Log.Write("予備録画フォルダ: " + text);
				Log.Write("予備録画フォルダ使用条件: {0} GB".Formatex(new object[]
				{
					num
				}));
			}
			else
			{
				Log.Write("予備録画フォルダ: なし");
			}
		}

		private void Check6()
		{
			this.SetDefault("epg.basic", "");
			string text = this.list["epg.basic"];
			if (text != "")
			{
				string[] array = text.Split(new char[]
				{
					','
				});
				for (int i = 0; i < array.Length; i++)
				{
					string text2 = array[i];
					int num;
					if (!int.TryParse(text2, out num))
					{
						throw new Exception("番組表 基本情報取得NIDが不正な値です。" + text2);
					}
				}
			}
			Log.Write("番組表 基本情報取得NID: " + ((text == "") ? "なし" : text));
		}

		private void Check5()
		{
			this.SetDefault("debug", "0");
			string text = this.list["debug"];
			if (text == "1")
			{
				Log.Write("debug mode: " + text);
			}
		}

		private void Check4()
		{
			this.SetDefault("postprocess", "");
			string text = this.list["postprocess"];
			if (text != "" && !File.Exists(text))
			{
				throw new Exception("録画後プロセスが見つかりません。設定を確認してください。");
			}
			Log.Write("録画後プロセス: " + ((this.list["postprocess"] == "") ? "なし" : this.list["postprocess"]));
			this.SetDefault("autosleep", "on");
			if (this.list["postprocess"] != "")
			{
				this.list["autosleep"] = "off";
			}
			Log.Write("自動スリープ: " + ((this.list["autosleep"] == "on") ? "on" : "off"));
			this.SetDefault("epgurl", "http://localhost:20001/maid/epg.html");
			Log.Write("番組表URL: " + this.list["epgurl"]);
			if (!this.list.IsDefined("url"))
			{
				this.list["url"] = "http://+:20000/";
			}
		}

		private void Check3()
		{
			this.SetDefault("epg.hour", "9");
			string[] array = this.list["epg.hour"].Split(new char[]
			{
				','
			}, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i];
				int num;
				if (!int.TryParse(text, out num))
				{
					throw new Exception("番組表取得時刻が不正な値です。" + text);
				}
				num = text.ToInt();
				if (num < 0 || num > 23)
				{
					throw new Exception("番組表取得時刻が不正な値です。" + num);
				}
			}
			Log.Write("番組表取得時刻: " + this.list["epg.hour"] + " 時");
		}

		private void Check2()
		{
			this.SetDefault("record.file", "{title}-{start-yy}{start-MM}{start-dd}-{start-hh}{start-mm}.ts");
			Log.Write("録画ファイル: " + this.list["record.file"]);
			this.SetDefault("record.margin.start", "10");
			this.list.GetInt("record.margin.start");
			Log.Write("開始マージン: " + this.list["record.margin.start"] + "秒");
			this.SetDefault("record.margin.end", "10");
			this.list.GetInt("record.margin.end");
			Log.Write("終了マージン: " + this.list["record.margin.end"] + "秒");
			// mark10als
			this.SetDefault("record.margin.overlap", "0");
			this.list.GetInt("record.margin.overlap");
			Log.Write("マージンのオーバーラップ許可: " + this.list["record.margin.overlap"]);
			this.SetDefault("extend.name.change", "0");
			this.list.GetInt("extend.name.change");
			Log.Write("ファイル名の拡張変更: " + this.list["extend.name.change"]);
		}

		private void Check1()
		{
			if (!this.list.IsDefined("tvtest"))
			{
				throw new Exception("TVTestのパスを設定してください。");
			}
			string text = this.list["tvtest"];
			Log.Write("TVTest: " + text);
			if (!File.Exists(text))
			{
				throw new Exception("TVTestが見つかりません。設定を確認してください。");
			}
			if (!this.list.IsDefined("record.folder"))
			{
				throw new Exception("録画フォルダのパスを設定してください。");
			}
			string text2 = this.list["record.folder"];
			Log.Write("録画フォルダ: " + text2);
			if (!Directory.Exists(text2))
			{
				throw new Exception("録画フォルダが見つかりません。");
			}
		}
	}
}
