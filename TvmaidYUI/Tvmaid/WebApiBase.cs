using Codeplex.Data;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;

namespace Tvmaid
{
	internal class WebApiBase
	{
		private NameValueCollection query;

		protected HttpListenerRequest req;

		protected HttpListenerResponse res;

		protected WebRet ret = new WebRet();

		protected WebApiBase(HttpListenerRequest req, HttpListenerResponse res)
		{
			this.req = req;
			this.res = res;
			int num = req.RawUrl.IndexOf('?');
			if (num != -1)
			{
				string text = req.RawUrl.Substring(num);
				this.query = HttpUtility.ParseQueryString(text);
			}
			this.ret.SetCode(0, "");
		}

		public void Exec(string func)
		{
			try
			{
				base.GetType().InvokeMember(func, BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, this, null);
			}
			catch (TargetInvocationException ex)
			{
				this.ret.SetCode(1, ex.InnerException.Message);
			}
			catch (MissingMethodException)
			{
				this.ret.SetCode(1, "指定されたWeb Apiはありません。" + func);
			}
			catch (Exception ex2)
			{
				Log.Write(ex2.Message);
				this.ret.SetCode(1, ex2.Message);
			}
			string s = DynamicJson.Serialize(this.ret);
			byte[] bytes = Encoding.UTF8.GetBytes(s);
			this.res.ContentEncoding = new UTF8Encoding();
			this.res.ContentType = "application/json";
			try
			{
				this.res.OutputStream.Write(bytes, 0, bytes.Length);
			}
			catch (Exception ex3)
			{
				Log.Write(ex3.Message);
				Log.Write(1, ex3.StackTrace);
			}
		}

		protected string GetQuery(string name)
		{
			if (this.query[name] == null)
			{
				throw new Exception("必須のパラメータがありません。 - " + name);
			}
			return this.query[name];
		}

		protected string GetQuery(string name, string defaultVal)
		{
			if (this.query[name] != null)
			{
				return this.query[name];
			}
			return defaultVal;
		}

		protected int GetQuery(string name, int defaultVal)
		{
			if (this.query[name] != null)
			{
				return this.query[name].ToInt();
			}
			return defaultVal;
		}

		protected long GetQuery(string name, long defaultVal)
		{
			if (this.query[name] != null)
			{
				return this.query[name].ToLong();
			}
			return defaultVal;
		}
	}
}
