using System;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace Tvmaid
{
	internal class Shared : IDisposable
	{
		private MemoryMappedFile map;

		private MemoryMappedViewAccessor acc;

		public Shared(string name)
		{
			this.map = MemoryMappedFile.OpenExisting(name);
			this.acc = this.map.CreateViewAccessor();
		}

		public void Dispose()
		{
			if (this.acc != null)
			{
				this.acc.Dispose();
			}
			if (this.map != null)
			{
				this.map.Dispose();
			}
		}

		public void Write(string str)
		{
			byte[] bytes = Encoding.Unicode.GetBytes(str);
			this.acc.WriteArray<byte>(0L, bytes, 0, bytes.Length);
		}

		public string Read()
		{
			long num = 0L;
			StringBuilder stringBuilder = new StringBuilder();
			while (true)
			{
				char c = this.acc.ReadChar(num);
				if (c == '\0')
				{
					break;
				}
				stringBuilder.Append(c);
				num += 2L;
			}
			return stringBuilder.ToString();
		}
	}
}
