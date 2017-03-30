using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tvmaid
{
	public class PairList : List<KeyValuePair<string, string>>
	{
		private string path;

		public string this[string key]
		{
			get
			{
				foreach (KeyValuePair<string, string> current in this)
				{
					if (current.Key == key)
					{
						return current.Value;
					}
				}
				return null;
			}
			set
			{
				for (int i = 0; i < base.Count; i++)
				{
					if (base[i].Key == key)
					{
						base[i] = new KeyValuePair<string, string>(key, value);
						return;
					}
				}
				base.Add(new KeyValuePair<string, string>(key, value));
			}
		}

		public PairList(string path)
		{
			this.path = path;
		}

		public int GetInt(string key)
		{
			return this[key].ToInt();
		}

		public float GetFloat(string key)
		{
			return (float)Convert.ToDouble(this[key]);
		}

		public void Save()
		{
			using (StreamWriter streamWriter = new StreamWriter(this.path, false, Encoding.GetEncoding("utf-8")))
			{
				foreach (KeyValuePair<string, string> current in this)
				{
					streamWriter.WriteLine(current.Key + "=" + current.Value);
				}
			}
		}

		public void Load()
		{
			using (StreamReader streamReader = new StreamReader(this.path, Encoding.GetEncoding("utf-8")))
			{
				string[] array = streamReader.ReadToEnd().Split(new string[]
				{
					"\r\n"
				}, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < array.Length; i++)
				{
					string text = array[i];
					if (text.Length < 2 || !text.StartsWith("//"))
					{
						int num = text.IndexOf('=');
						if (num != -1)
						{
							string key = text.Substring(0, num);
							string value = "";
							if (num + 1 < text.Length)
							{
								value = text.Substring(num + 1);
							}
							this[key] = value;
						}
					}
				}
			}
		}

		public bool IsDefined(string key)
		{
			foreach (KeyValuePair<string, string> current in this)
			{
				if (current.Key == key)
				{
					return true;
				}
			}
			return false;
		}
	}
}
