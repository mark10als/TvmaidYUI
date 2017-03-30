using System;

namespace Tvmaid
{
	internal class DupServiceException : Exception
	{
		public DupServiceException(string msg) : base(msg)
		{
		}
	}
}
