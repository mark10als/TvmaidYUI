using System;

namespace Tvmaid
{
	internal class TunerServerExceotion : Exception
	{
		public uint Code;

		private string[] messages = new string[]
		{
			"",
			"Tvmaid、TVTest間の共有メモリ作成に失敗しました。",
			"Tvmaid、TVTest間の通信ウインドウの作成に失敗しました。",
			"排他制御の作成に失敗しました。",
			"録画開始に失敗しました。",
			"録画停止に失敗しました。",
			"サービス切り替えに失敗しました。",
			"番組情報の取得に失敗しました(複数の番組)。",
			"録画状態の取得に失敗しました。",
			"初期化に失敗しました(環境変数の取得失敗)。",
			"録画中の番組時間の取得に失敗しました。",
			"エラーパケット数の取得に失敗しました。",
			"ロゴの取得に失敗しました。"
		};

		public override string Message
		{
			get
			{
				return "TVTestでエラーが発生しました。" + this.messages[(int)this.Code];
			}
		}

		public TunerServerExceotion(uint code)
		{
			this.Code = code;
		}
	}
}
