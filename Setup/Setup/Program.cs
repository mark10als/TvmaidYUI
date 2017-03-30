using System;
using System.Windows.Forms;
using Tvmaid;

namespace Setup
{
	internal static class Program
	{
		public static string Logo = "Tvmaid";

		[STAThread]
		private static void Main()
		{
			Ticket ticket = new Ticket("/tvmaid/mutex/main");
			while (!ticket.GetOwner(1000))
			{
				if (MessageBox.Show("Tvmaidを起動中に設定できません。\nTvmaidを終了して、再試行してください。\nTvmaidを終了できないときは、キャンセルしてください。", Program.Logo, MessageBoxButtons.RetryCancel) == DialogResult.Cancel)
				{
					return;
				}
			}
			ticket.Dispose();
			try
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new SetupForm());
			}
			catch (Exception ex)
			{
				MessageBox.Show("このエラーは回復できないため、アプリケーションは終了します。[詳細]" + ex.Message, "Tvmaid");
			}
		}
	}
}
