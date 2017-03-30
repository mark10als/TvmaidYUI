using System;

namespace Tvmaid
{
	internal class WebRet
	{
		public int code
		{
			get;
			set;
		}

		public string message
		{
			get;
			set;
		}

		public object data1
		{
			get;
			set;
		}

		public WebRet()
		{
			this.SetCode(0, "");
		}

		public void SetCode(int code, string message)
		{
			this.code = code;
			this.message = message;
		}
	}
}
