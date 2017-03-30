using System;
using System.IO;
using System.Reflection;

namespace Tvmaid
{
	public static class Util
	{
		public static string GetBasePath()
		{
			return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		}

		public static string GetBasePath(string file)
		{
			return Path.Combine(Util.GetBasePath(), file);
		}

		public static string GetWwwRootPath()
		{
			return Path.Combine(Util.GetBasePath(), "wwwroot");
		}

		public static string GetUserPath()
		{
			return Path.Combine(Util.GetBasePath(), "user");
		}

		public static string GetUserPath(string file)
		{
			return Path.Combine(Util.GetUserPath(), file);
		}

		public static string GetThumbPath()
		{
			return Path.Combine(Util.GetUserPath(), "thumb");
		}

		public static string GetThumbPath(long id, string size)
		{
			return Path.Combine(Util.GetThumbPath(), size, (id / 1000L).ToString(), id.ToString() + ".jpg");
		}

		public static void CopyUserFile()
		{
			string userPath = Util.GetUserPath();
			if (!Directory.Exists(userPath))
			{
				Directory.CreateDirectory(userPath);
			}
			FileInfo[] files = new DirectoryInfo(Path.Combine(Util.GetBasePath(), "original")).GetFiles();
			for (int i = 0; i < files.Length; i++)
			{
				FileInfo fileInfo = files[i];
				string text = Path.Combine(userPath, fileInfo.Name);
				if (!File.Exists(text))
				{
					fileInfo.CopyTo(text);
				}
			}
		}
	}
}
