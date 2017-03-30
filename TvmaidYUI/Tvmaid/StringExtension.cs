using System;

namespace Tvmaid
{
	internal static class StringExtension
	{
		public static string Formatex(this string format, params object[] values)
		{
			return string.Format(format, values);
		}

		public static bool CompareNC(this string str1, string str2)
		{
			return string.Compare(str1, str2, true) == 0;
		}

		public static int ToInt(this string s)
		{
			return int.Parse(s);
		}

		public static long ToLong(this string s)
		{
			return long.Parse(s);
		}

		public static DateTime ToDateTime(this string s)
		{
			return DateTime.Parse(s);
		}
	}
}
