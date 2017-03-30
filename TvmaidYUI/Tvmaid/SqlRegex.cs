using System;
using System.Data.SQLite;
using System.Text.RegularExpressions;

namespace Tvmaid
{
	[SQLiteFunction(Name = "regexp", Arguments = 3, FuncType = FunctionType.Scalar)]
	internal class SqlRegex : SQLiteFunction
	{
		public override object Invoke(object[] args)
		{
			object result;
			try
			{
				result = Regex.IsMatch((string)args[0], (string)args[1], (RegexOptions)Convert.ToInt32(args[2]));
			}
			catch
			{
				result = false;
			}
			return result;
		}
	}
}
