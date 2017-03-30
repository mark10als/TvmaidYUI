using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tvmaid
{
	internal static class Program
	{
		public static int ExitMode;

		[STAThread]
		private static void Main(string[] args)
		{
			List<Task> tasks = new List<Task>();
			WebServer ws = null;
			// mark10als
			// Log.Write(AppData.AppName + " " + AppData.AppVersion);
			Log.Write(AppData.AppName + " " + AppData.AppVersion + " " + AppData.AppMod);
			Ticket ticket = new Ticket("/tvmaid/mutex/main");
			try
			{
				if (!ticket.GetOwner(10000))
				{
					ticket = null;
					throw new Exception("時間内に二重起動が解消されませんでした。");
				}
				Program.LoadDef();
				if (MainDef.GetInstance()["debug"] == "1")
				{
					Log.GetInstance().SetLevel(1);
					Log.Write("debug mode 1 に設定しました。");
				}
				Program.CopyPlugin();
				if (args.Length == 1 && args[0] == "-tunerupdate")
				{
					Program.UpdateTuner();
				}
				Task item = Task.Factory.StartNew(() =>
				{
					RecTimer.GetInstance().Run();
				}, TaskCreationOptions.AttachedToParent);
				tasks.Add(item);
				ws = new WebServer();
				item = Task.Factory.StartNew(() =>
				{
					ws.Start();
				}, TaskCreationOptions.AttachedToParent);
				tasks.Add(item);
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new TunerMon());
			}
			catch (Exception ex)
			{
				string str = ex.Message + ex.StackTrace;
				MessageBox.Show("このエラーは回復できないため、アプリケーションは終了します。[詳細]" + str, AppData.AppName);
			}
			finally
			{
				if (ws != null)
				{
					ws.Dispose();
				}
				RecTimer.GetInstance().Dispose();
				ExitForm form = new ExitForm(30);
				Task.Factory.StartNew(delegate
				{
					Task.WaitAll(tasks.ToArray(), 30000);
				}).ContinueWith(delegate(Task _)
				{
					form.Close();
				}, TaskScheduler.FromCurrentSynchronizationContext());
				form.ShowDialog();
				if (ticket != null)
				{
					ticket.Dispose();
				}
				Program.StartNextProcess();
			}
		}

		private static void StartNextProcess()
		{
			try
			{
				int exitMode = Program.ExitMode;
				if (exitMode != 1)
				{
					if (exitMode == 2)
					{
						Process.Start(Util.GetBasePath("setup.exe"));
					}
				}
				else
				{
					Process.Start(Application.ExecutablePath, "-tunerupdate");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("プログラムを起動できませんでした。[詳細]" + ex.Message, AppData.AppName);
			}
		}

		private static void UpdateTuner()
		{
			try
			{
				using (Sql sql = new Sql(true))
				{
					Log.Write("チューナを更新しています...");
					Tuner.Update(sql);
					Log.Write("サービスを更新しています...");
					bool arg_37_0 = Program.UpdateService(sql);
					Log.Write("余分なデータを削除しています...");
					Program.Cleanup(sql);
					if (arg_37_0)
					{
						MessageBox.Show("サービスが重複しています。\nこのままでも使用できますが、TVTestのチャンネルスキャンで同じ放送局を1つを残して他は無効(チェックを外す)にすることをおすすめします。", AppData.AppName);
					}
				}
				Log.Write("チューナ更新が完了しました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show("チューナの読み込みに失敗しました。[詳細]" + ex.Message, AppData.AppName);
				throw;
			}
		}

		private static void CopyPlugin()
		{
			try
			{
				string basePath = Util.GetBasePath("TvmaidPlugin.tvtp");
				string text = Path.Combine(Path.Combine(Path.GetDirectoryName(MainDef.GetInstance()["tvtest"]), "Plugins"), "TvmaidPlugin.tvtp");
				if (File.Exists(text) && File.GetLastWriteTime(basePath) == File.GetLastWriteTime(text))
				{
					Log.Write("Tvmaidプラグイン OK");
				}
				else
				{
					File.Copy(basePath, text, true);
					Log.Write("Tvmaidプラグインを更新しました。");
				}
			}
			catch (Exception ex)
			{
				throw new Exception("TVTestのプラグインフォルダに、Tvmaidプラグインをコピーできませんでした。[詳細]" + ex.Message);
			}
		}

		private static void Cleanup(Sql sql)
		{
			sql.Text = "delete from event where fsid not in (select fsid from service group by fsid)";
			sql.Execute();
			sql.Text = "delete from user_epg where fsid not in (select fsid from service group by fsid)";
			sql.Execute();
			sql.Text = "delete from record where fsid not in (select fsid from service group by fsid)";
			sql.Execute();
			sql.Text = "delete from record where tuner not in (select name from tuner)";
			sql.Execute();
		}

		private static bool UpdateService(Sql sql)
		{
			sql.Text = "delete from service";
			sql.Execute();
			List<Tuner> list = new List<Tuner>();
			sql.Text = "select * from tuner group by driver ";
			using (DataTable table = sql.GetTable())
			{
				while (table.Read())
				{
					list.Add(new Tuner(table));
				}
			}
			bool result = false;
			foreach (Tuner current in list)
			{
				try
				{
					current.GetServices(sql);
				}
				catch (DupServiceException)
				{
					result = true;
				}
			}
			return result;
		}

		private static void LoadDef()
		{
			Log.Write("初期化中...");
			Util.CopyUserFile();
			MainDef.GetInstance().Check();
			GenreConv.GetInstance();
			TextConv.GetInstance();
			EpgWait.GetInstance();
		}
	}
}
