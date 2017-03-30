using Microsoft.Win32;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace Tvmaid
{
	internal class WebServer : IDisposable
	{
		private HttpListener listener = new HttpListener();

		private bool stop;

		public void Start()
		{
			string text = MainDef.GetInstance()["url"];
			Log.Write("Webサーバ受け入れURL: " + text);
			try
			{
				this.listener.Prefixes.Add(text);
				this.listener.Start();
				goto IL_B5;
			}
			catch (Exception ex)
			{
				string expr_58 = "Webサーバの初期化に失敗しました。この状態では、ブラウザからアクセスできません。アプリケーションを終了してください。[詳細] {0}\n[対策] web.batを実行しましたか？".Formatex(new object[]
				{
					ex.Message
				});
				Log.Write(expr_58);
				Log.Write(1, ex.StackTrace);
				MessageBox.Show(expr_58, AppData.AppName);
				return;
			}
			IL_77:
			try
			{
				HttpListenerContext context = this.listener.GetContext();
				Task.Factory.StartNew(delegate
				{
					this.Excute(context);
				}, TaskCreationOptions.AttachedToParent);
			}
			catch (HttpListenerException)
			{
				return;
			}
			catch (ObjectDisposedException)
			{
				return;
			}
			IL_B5:
			if (!this.stop)
			{
				goto IL_77;
			}
		}

		public void Dispose()
		{
			this.listener.Close();
			this.stop = true;
		}

		private void Excute(HttpListenerContext context)
		{
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;
			if (!request.HttpMethod.CompareNC("GET"))
			{
				response.StatusCode = 501;
				response.StatusDescription = "NotImplemented";
				try
				{
					response.Close();
				}
				catch
				{
				}
				return;
			}
			string text = HttpUtility.UrlDecode(request.Url.AbsolutePath);
			string[] array = text.Split(new char[]
			{
				'/'
			}, StringSplitOptions.RemoveEmptyEntries);
			string path;
			if (array.Length == 0)
			{
				path = Util.GetWwwRootPath() + "/index.html";
			}
			else
			{
				if (array[0] == "webapi")
				{
					try
					{
						WebApiBase arg_9D_0 = new WebApi(request, response);
						string fileName = Path.GetFileName(text);
						arg_9D_0.Exec(fileName);
					}
					catch (Exception ex)
					{
						Log.Write(ex.Message);
						Log.Write(1, ex.StackTrace);
					}
					try
					{
						response.Close();
					}
					catch (Exception)
					{
					}
					return;
				}
				if (array[0] == "logo")
				{
					path = Util.GetUserPath() + text;
				}
				else
				{
					path = Util.GetWwwRootPath() + text;
				}
			}
			try
			{
				if (File.Exists(path))
				{
					byte[] array2 = File.ReadAllBytes(path);
					response.ContentType = this.GetContentType(path);
					response.OutputStream.Write(array2, 0, array2.Length);
					response.Close();
				}
				else
				{
					response.StatusCode = 404;
					response.StatusDescription = "NotFound";
					byte[] array3 = File.ReadAllBytes(Util.GetUserPath("404.html"));
					response.ContentType = "text/html";
					response.OutputStream.Write(array3, 0, array3.Length);
					response.Close();
				}
			}
			catch (Exception ex2)
			{
				Log.Write("Webサーバのファイル転送時にエラーが発生しましたが、プログラムは続行します。\n[詳細]" + ex2.Message);
				Log.Write(1, ex2.StackTrace);
			}
		}

		public string GetContentType(string path)
		{
			RegistryKey expr_10 = Registry.ClassesRoot.OpenSubKey(Path.GetExtension(path));
			object value = expr_10.GetValue("Content Type");
			if (expr_10 != null && value != null)
			{
				return value.ToString();
			}
			return "";
		}
	}
}
