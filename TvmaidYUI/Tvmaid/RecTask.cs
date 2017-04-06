using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Tvmaid
{
	internal class RecTask
	{
		// mark10als
		private PairList list;

		private Tuner tuner;

		private Sql sql;

		private Record record;

		private Result result;

		private TsStatus tsStatus;

		private Service service;

		private string recPath;

		private bool eventTimeError;

		private bool recContinue;

		public RecTask(Tuner tuner, Record record)
		{
			this.tuner = tuner;
			this.record = record;
		}

		private bool StoppdApp()
		{
			return RecTimer.GetInstance().Stopped();
		}

		public void Run()
		{
			try
			{
				SleepState.Stop(true);
				this.sql = new Sql(true);
				this.service = new Service(this.sql, this.record.Fsid);
				Log.Write(this.tuner.Name + ": 録画を開始します。" + this.record.Title);
				this.StartRec();
				if (this.record.EndTime < DateTime.Now)
				{
					throw new Exception("終了時刻が過ぎています。");
				}
				int @int = MainDef.GetInstance().GetInt("record.margin.end");
				while (this.record.EndTime - new TimeSpan(0, 0, @int) > DateTime.Now)
				{
					if (this.StoppdApp())
					{
						throw new Exception("アプリケーション終了のため、録画を中断します。");
					}
					this.CheckCancel();
					this.CheckEvent();
					Thread.Sleep(1000);
				}
			}
			catch (Exception ex)
			{
				string text = "{0}: 録画に失敗しました。{1}".Formatex(new object[]
				{
					this.tuner.Name,
					ex.Message
				});
				Log.Write(text + " - " + this.record.Title);
				Log.Write(1, ex.StackTrace);
				if (this.result != null)
				{
					this.result.Code = 1;
					this.result.Message = text;
				}
			}
			finally
			{
				try
				{
					this.StopRec();
				}
				catch (Exception ex2)
				{
					Log.Write("{0}: 録画終了処理に失敗しました。{1}".Formatex(new object[]
					{
						this.tuner.Name,
						ex2.Message
					}));
					Log.Write(1, ex2.StackTrace);
				}
				SleepState.Stop(false);
				this.sql.Dispose();
				Log.Write("{0}: 録画終了しました。 - {1}".Formatex(new object[]
				{
					this.tuner.Name,
					this.record.Title
				}));
				this.PostProcess();
			}
		}

		private void PostProcess()
		{
			try
			{
				string text = MainDef.GetInstance()["postprocess"];
				if (this.result.Code == 0 && text != "")
				{
					Log.Write("録画後プロセス実行.");
					// mark10als 
					//Process.Start(text, "\"" + this.recPath + "\"");
					ProcessStartInfo ppInfo = new ProcessStartInfo();
					ppInfo.FileName = text; // 実行するファイル
					ppInfo.Arguments = "\"" + this.recPath + "\"";
					ppInfo.CreateNoWindow = false; // コンソール・ウィンドウを開かない
					ppInfo.UseShellExecute = false; // シェル機能を使用しない
					Process.Start(ppInfo);
				}
			}
			catch (Exception ex)
			{
				Log.Write("録画後プロセスの実行に失敗しました。[詳細] " + ex.Message);
				Log.Write(1, ex.StackTrace);
			}
		}

		private Event GetEventTime()
		{
			Event @event;
			try
			{
				Event arg_50_0 = this.tuner.GetEventTime(this.service, this.record.Eid);
				if (this.eventTimeError)
				{
					Log.Write(this.tuner.Name + ": 番組時間の取得成功。" + this.record.Title);
					this.eventTimeError = false;
				}
				@event = arg_50_0;
			}
			catch (TunerServerExceotion arg_53_0)
			{
				if (arg_53_0.Code != 10u)
				{
					throw;
				}
				if (!this.eventTimeError)
				{
					Log.Write(this.tuner.Name + ": 番組時間の取得に失敗しました。番組がなくなった可能性があります。録画は続行します。" + this.record.Title);
					this.eventTimeError = true;
				}
				@event = null;
			}
			return @event;
		}

		private void CheckEvent()
		{
			if (this.record == null)
			{
				return;
			}
			if ((this.record.Status & 2) == 0)
			{
				return;
			}
			Event eventTime = this.GetEventTime();
			if (eventTime == null)
			{
				return;
			}
			if (eventTime.Start != this.record.StartTime || eventTime.Duration != this.record.Duration)
			{
				Log.Write(this.tuner.Name + ": 番組時間が変更されました。" + this.record.Title);
				this.record.StartTime = eventTime.Start;
				this.record.Duration = eventTime.Duration;
				this.tuner.GetEvents(this.sql, this.service);
				Record.UpdateRecordTime(this.sql);
				Record.ResetTuner(this.sql);
				Log.Write("チューナの再割り当てを行いました。");
				if (this.record.StartTime - DateTime.Now > TimeSpan.FromMinutes(1.0))
				{
					this.recContinue = true;
					throw new Exception("番組の開始時間が遅れているため、録画を中断します。");
				}
			}
		}

		private void CheckCancel()
		{
			if (this.record == null)
			{
				return;
			}
			this.tsStatus = this.tuner.GetTsStatus();
			this.sql.Text = "select status from record where id = " + this.record.Id;
			object data = this.sql.GetData();
			if (data == null)
			{
				throw new Exception("予約が取り消されたため、録画を中断します。");
			}
			if (((int)((long)data) & 1) == 0)
			{
				throw new Exception("予約が無効にされたため、録画を中断します。");
			}
			if (this.tuner.GetState() != Tuner.State.Recoding)
			{
				throw new Exception(" 録画が中断しました。");
			}
		}

		private void StartRec()
		{
			this.recContinue = false;
			this.eventTimeError = false;
			string TunerName = this.tuner.Name;		    //Tuner名の取得を追加
			string text = this.GetRecPath(TunerName);   //GetRecPathにTiner名を渡すよう変更
			this.recPath = text;
			this.InitResult(text);
			this.tsStatus = new TsStatus();
			this.record.SetRecoding(this.sql, true);
			this.tuner.Open(false);
			this.tuner.SetService(this.service);
			this.tuner.StartRec(text);
			Thread.Sleep(1000);
		}

		private void InitResult(string path)
		{
			this.result = new Result();
			this.result.Title = this.record.Title;
			this.result.ServiceName = this.service.Name;
			this.result.File = Path.GetFileName(path);
			this.result.SchStart = this.record.StartTime;
			this.result.SchEnd = this.record.EndTime;
			this.result.Start = DateTime.Now;
		}

		private string GetRecPath(string tunername) //引数Tuner名を追加
		{
			//main.defのTuner名毎のrecord.tunerfolderを取得する処理を追加
			string arg_26_0;
			this.list = new PairList(Util.GetUserPath("main.def"));
			this.list.Load();
			if (this.list.IsDefined("record.tunerfolder." + tunername))
			{
				arg_26_0 = MainDef.GetInstance()["record.tunerfolder." + tunername];
			}
			else
			{
				arg_26_0 = MainDef.GetInstance()["record.folder"];
			}
/*
			Log.Write("保存フォルダー0:" + tunername);
			string arg_26_0 = MainDef.GetInstance()["record.tunerfolder." + tunername];
			int iLength = arg_26_0.Length;
			bool flag3 = iLength == 0;
			Log.Write("保存フォルダー1:" + arg_26_0);
			Log.Write("保存フォルダー2:" + iLength);
			if (flag3)
			{
				arg_26_0 = MainDef.GetInstance()["record.folder"];
			}
			Log.Write("保存フォルダー3:" + arg_26_0);
*/
			// mark10als
			string text2 = MainDef.GetInstance()["record.folder.spare"];
			bool flag = text2 != "";
			if (flag)
			{
				ulong num;
				ulong num2;
				ulong num3;
				bool diskFreeSpaceEx = RecTask.GetDiskFreeSpaceEx(arg_26_0, out num, out num2, out num3);
				if (diskFreeSpaceEx)
				{
					ulong num4 = ulong.Parse(MainDef.GetInstance()["record.folder.spare.use"]);
					num4 = num4 * 1024uL * 1024uL * 1024uL;
					bool flag2 = num < num4;
					if (flag2)
					{
						arg_26_0 = text2;
					}
				}
				else
				{
					Log.Write("空きディスク容量が取得できませんでした。");
				}
			}

			string path = this.ConvertFileMacro(MainDef.GetInstance()["record.file"]);
			string src = Path.Combine(arg_26_0, path);
			return this.CheckFilePath(src);
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

		private void StopRec()
		{
			if (this.recContinue)
			{
				this.record.SetRecoding(this.sql, false);
			}
			else
			{
				this.record.SetComplete(this.sql);
			}
			try
			{
				Tuner.State state = this.tuner.GetState();
				if (state == Tuner.State.Recoding || state == Tuner.State.Paused)
				{
					this.tuner.StopRec();
				}
			}
			catch
			{
			}
			try
			{
				if (this.tuner.IsOpen())
				{
					this.tuner.Close();
				}
			}
			catch
			{
			}
			this.SetResult();
		}

		private void SetResult()
		{
			this.result.Error = this.tsStatus.Error;
			this.result.Drop = this.tsStatus.Drop;
			this.result.Scramble = this.tsStatus.Scramble;
			this.result.End = DateTime.Now;
			if (this.record.Eid != -1)
			{
				try
				{
					Event @event = new Event(this.sql, this.record.Fsid, this.record.Eid);
					this.result.Desc = @event.Desc;
					this.result.LongDesc = @event.LongDesc;
					this.result.GenreText = @event.GenreText;
				}
				catch
				{
				}
			}
			this.result.Add(this.sql);
		}

		private string ConvertFileName(string name)
		{
			char[] array = new char[]
			{
				'\\',
				'/',
				':',
				'*',
				'?',
				'"',
				'<',
				'>',
				'|'
			};
			for (int i = 0; i < array.Length; i++)
			{
				char oldChar = array[i];
				name = name.Replace(oldChar, '_');
			}
			// mark10als
			string text2 = MainDef.GetInstance()["extend.name.change"];
			bool flag = text2 != "0";
			if (flag)
			{
				name = name.Replace(' ', '_');
				name = name.Replace(',', '、');
				name = name.Replace('　', '_');
				name = name.Replace('&', '＆');
				name = name.Replace('%', '％');
			}
			return name;
		}

		public string ConvertFileMacro(string name)
		{
			Dictionary<string, string> expr_05 = new Dictionary<string, string>();
			expr_05.Add("{title}", this.record.Title);
			expr_05.Add("{service}", this.service.Name);
			expr_05.Add("{nid}", this.service.Nid.ToString("x"));
			expr_05.Add("{tsid}", this.service.Tsid.ToString("x"));
			expr_05.Add("{sid}", this.service.Sid.ToString("x"));
			expr_05.Add("{eid}", this.record.Eid.ToString("x"));
			expr_05.Add("{start-yyyy}", this.record.StartTime.ToString("yyyy"));
			expr_05.Add("{start-yy}", this.record.StartTime.ToString("yy"));
			expr_05.Add("{start-MM}", this.record.StartTime.ToString("MM"));
			expr_05.Add("{start-M}", this.record.StartTime.ToString("%M"));
			expr_05.Add("{start-dd}", this.record.StartTime.ToString("dd"));
			expr_05.Add("{start-d}", this.record.StartTime.ToString("%d"));
			expr_05.Add("{start-week}", this.record.StartTime.ToString("ddd"));
			expr_05.Add("{start-hh}", this.record.StartTime.ToString("HH"));
			expr_05.Add("{start-h}", this.record.StartTime.ToString("%H"));
			expr_05.Add("{start-mm}", this.record.StartTime.ToString("mm"));
			expr_05.Add("{start-m}", this.record.StartTime.ToString("%m"));
			expr_05.Add("{end-yyyy}", this.record.EndTime.ToString("yyyy"));
			expr_05.Add("{end-yy}", this.record.EndTime.ToString("yy"));
			expr_05.Add("{end-MM}", this.record.EndTime.ToString("MM"));
			expr_05.Add("{end-M}", this.record.EndTime.ToString("%M"));
			expr_05.Add("{end-dd}", this.record.EndTime.ToString("dd"));
			expr_05.Add("{end-d}", this.record.EndTime.ToString("%d"));
			expr_05.Add("{end-week}", this.record.EndTime.ToString("ddd"));
			expr_05.Add("{end-hh}", this.record.EndTime.ToString("HH"));
			expr_05.Add("{end-h}", this.record.EndTime.ToString("%H"));
			expr_05.Add("{end-mm}", this.record.EndTime.ToString("mm"));
			expr_05.Add("{end-m}", this.record.EndTime.ToString("%m"));
			TimeSpan timeSpan = new TimeSpan(0, 0, this.record.Duration);
			expr_05.Add("{duration-hh}", timeSpan.ToString("hh"));
			expr_05.Add("{duration-h}", timeSpan.ToString("%h"));
			expr_05.Add("{duration-mm}", timeSpan.ToString("mm"));
			expr_05.Add("{duration-m}", timeSpan.ToString("%m"));
			foreach (KeyValuePair<string, string> current in expr_05)
			{
				name = name.Replace(current.Key, current.Value);
			}
			return this.ConvertFileName(name);
		}

		private string CheckFilePath(string src)
		{
			string path = src;
			int num = 2;
			while (File.Exists(path))
			{
				path = "{0}({1}){2}".Formatex(new object[]
				{
					Path.Combine(Path.GetDirectoryName(src), Path.GetFileNameWithoutExtension(src)),
					num,
					Path.GetExtension(src)
				});
				num++;
			}
			return path;
		}
	}
}
