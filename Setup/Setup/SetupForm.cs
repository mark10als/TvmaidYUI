using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Tvmaid;

namespace Setup
{
	public class SetupForm : Form
	{
		private PairList tunerDef;

		private MainDef mainDef;

		private Label label1;

		private TextBox tvtestBox;

		private Button recDirRefButton;

		private Button tvtestRefButton;

		private TextBox recDirBox;

		private Label label3;

		private Button tunerAddButton;

		private Button removeButton;

		private Button downButton;

		private Button upButton;

		private Label label5;

		private TextBox tunerNameBox;

		private Label label2;

		private Button endButton;

		private Label label6;

		private SaveFileDialog tvtestDialog;

		private FolderBrowserDialog recDirDialog;

		private Button driverRefButton;

		private Label label7;

		private TextBox driverBox;

		private SaveFileDialog driverDialog;

		private TabControl tabControl1;

		private TabPage tabPage1;

		private TabPage tabPage2;

		private CheckBox fileNameOnlyCheck;

		private Label label4;

		private TabPage tabPage4;

		private Button postProcessRefButton;

		private Label label13;

		private TextBox postProcessBox;

		private Label label12;

		private TextBox recFileBox;

		private Label label10;

		private Label label9;

		private NumericUpDown startMarginBox;

		private NumericUpDown endMarginBox;

		private TabPage tabPage3;

		private Label label17;

		private Label label14;

		private CheckBox autoSleepCheck;

		private Label label8;

		private SaveFileDialog postProcessDialog;

		private Panel tunerPanel;

		private CheckBox tunerUpdateCheck;

		private Button unregStartupButton;

		private Button regStartupButton;

		private TreeView tunerBox;

		private TextBox epgHourBox;

		public SetupForm()
		{
			this.InitializeComponent();
			Util.CopyUserFile();
			this.LoadMainDef();
			this.LoadTunerDef();
		}

		private void LoadMainDef()
		{
			this.mainDef = MainDef.GetInstance();
			MainDef mainDef = this.mainDef;
			this.tvtestBox.Text = mainDef["tvtest"];
			this.recDirBox.Text = mainDef["record.folder"];
			this.startMarginBox.Text = mainDef["record.margin.start"];
			this.endMarginBox.Text = mainDef["record.margin.end"];
			this.recFileBox.Text = mainDef["record.file"];
			this.epgHourBox.Text = mainDef["epg.hour"];
			this.autoSleepCheck.Checked = (mainDef["autosleep"] == "on");
			this.postProcessBox.Text = mainDef["postprocess"];
		}

		private void LoadTunerDef()
		{
			this.tunerDef = new PairList(Util.GetUserPath("tuner.def"));
			this.tunerDef.Load();
			foreach (KeyValuePair<string, string> current in this.tunerDef)
			{
				this.tunerBox.Nodes.Add(current.Key + "=" + current.Value);
			}
		}

		private void tvtestRefButton_Click(object sender, EventArgs e)
		{
			if (this.tvtestDialog.ShowDialog() == DialogResult.OK)
			{
				this.tvtestBox.Text = this.tvtestDialog.FileName;
			}
		}

		private void recDirRefButton_Click(object sender, EventArgs e)
		{
			if (this.recDirDialog.ShowDialog() == DialogResult.OK)
			{
				this.recDirBox.Text = this.recDirDialog.SelectedPath;
			}
		}

		private void driverRefButton_Click(object sender, EventArgs e)
		{
			if (File.Exists(this.tvtestBox.Text))
			{
				this.driverDialog.InitialDirectory = Path.GetDirectoryName(this.tvtestBox.Text);
			}
			if (this.driverDialog.ShowDialog() == DialogResult.OK)
			{
				this.driverBox.Text = this.driverDialog.FileName;
			}
		}

		private void postProcessRefButton_Click(object sender, EventArgs e)
		{
			if (this.postProcessDialog.ShowDialog() == DialogResult.OK)
			{
				this.postProcessBox.Text = this.postProcessDialog.FileName;
			}
		}

		private void tunerAddButton_Click(object sender, EventArgs arg)
		{
			try
			{
				if (this.tunerNameBox.Text == "")
				{
					throw new Exception("チューナ名を入力してください。");
				}
				if (this.tunerNameBox.Text.IndexOf('=') != -1)
				{
					throw new Exception("チューナ名に「=」は使えません。");
				}
				if (!this.fileNameOnlyCheck.Checked && !File.Exists(this.driverBox.Text))
				{
					throw new Exception("Bonドライバのパスが間違っています。");
				}
				IEnumerator enumerator = this.tunerBox.Nodes.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						string[] array = ((TreeNode)enumerator.Current).Text.Split(new char[]
						{
							'='
						});
						if (this.tunerNameBox.Text == array[0])
						{
							throw new Exception("同じ名前のチューナを複数指定できません。");
						}
					}
				}
				finally
				{
					IDisposable disposable = enumerator as IDisposable;
					if (disposable != null)
					{
						disposable.Dispose();
					}
				}
				string str = this.fileNameOnlyCheck.Checked ? Path.GetFileName(this.driverBox.Text) : this.driverBox.Text;
				this.tunerBox.Nodes.Add(this.tunerNameBox.Text + "=" + str);
			}
			catch (Exception arg_12F_0)
			{
				MessageBox.Show(arg_12F_0.Message, Program.Logo);
			}
		}

		private void upButton_Click(object sender, EventArgs e)
		{
			if (this.tunerBox.SelectedNode != null)
			{
				this.ReplaceNode(this.tunerBox.SelectedNode, this.tunerBox.SelectedNode.PrevNode);
			}
		}

		private void downButton_Click(object sender, EventArgs e)
		{
			if (this.tunerBox.SelectedNode != null)
			{
				this.ReplaceNode(this.tunerBox.SelectedNode, this.tunerBox.SelectedNode.NextNode);
			}
		}

		private void ReplaceNode(TreeNode node1, TreeNode node2)
		{
			if (node1 != null && node2 != null)
			{
				string text = node1.Text;
				node1.Text = node2.Text;
				node2.Text = text;
				this.tunerBox.SelectedNode = node2;
			}
		}

		private void removeButton_Click(object sender, EventArgs e)
		{
			if (this.tunerBox.SelectedNode != null)
			{
				this.tunerBox.Nodes.Remove(this.tunerBox.SelectedNode);
			}
		}

		private void endButton_Click(object sender, EventArgs arg)
		{
			try
			{
				string arguments = "";
				if (this.tunerUpdateCheck.Checked)
				{
					this.SaveTunerDef();
					arguments = "-tunerupdate";
				}
				this.SaveMainDef();
				Process.Start(Util.GetBasePath("Tvmaid.exe"), arguments);
				base.Close();
			}
			catch (Exception arg_3E_0)
			{
				MessageBox.Show(arg_3E_0.Message, Program.Logo);
			}
		}

		private void SaveMainDef()
		{
			MainDef expr_06 = this.mainDef;
			expr_06["tvtest"] = this.tvtestBox.Text;
			expr_06["record.folder"] = this.recDirBox.Text;
			expr_06["record.margin.start"] = this.startMarginBox.Text;
			expr_06["record.margin.end"] = this.endMarginBox.Text;
			expr_06["record.file"] = this.recFileBox.Text;
			expr_06["epg.hour"] = this.epgHourBox.Text;
			expr_06["autosleep"] = (this.autoSleepCheck.Checked ? "on" : "off");
			expr_06["postprocess"] = this.postProcessBox.Text;
			expr_06.Save();
		}

		private void SaveTunerDef()
		{
			PairList pairList = this.tunerDef;
			pairList.Clear();
			IEnumerator enumerator = this.tunerBox.Nodes.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					string[] array = ((TreeNode)enumerator.Current).Text.Split(new char[]
					{
						'='
					});
					pairList[array[0]] = array[1];
				}
			}
			finally
			{
				IDisposable disposable = enumerator as IDisposable;
				if (disposable != null)
				{
					disposable.Dispose();
				}
			}
			pairList.Save();
		}

		private void tunerUpdateCheck_CheckedChanged(object sender, EventArgs e)
		{
			this.tunerPanel.Enabled = this.tunerUpdateCheck.Checked;
		}

		private void regStartupButton_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("スタートアップに登録していいですか？\n注意！ レジストリのスタートアップに登録します。\nこの機能は使用せず、自分でショーカットをスタートメニューに置いてもかまいません。", Program.Logo, MessageBoxButtons.OKCancel) != DialogResult.OK)
			{
				return;
			}
			RegistryKey expr_24 = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
			expr_24.SetValue("TvmaidYUI", Util.GetBasePath("Tvmaid.exe"));
			expr_24.Close();
			MessageBox.Show("登録しました。", Program.Logo);
		}

		private void unregStartupButton_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("レジストリのスタートアップ設定を削除していいですか？", Program.Logo, MessageBoxButtons.OKCancel) != DialogResult.OK)
			{
				return;
			}
			RegistryKey expr_24 = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
			expr_24.DeleteValue("TvmaidYUI", false);
			expr_24.Close();
			MessageBox.Show("削除しました。", Program.Logo);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			ComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof(SetupForm));
			this.label1 = new Label();
			this.tvtestBox = new TextBox();
			this.recDirRefButton = new Button();
			this.tvtestRefButton = new Button();
			this.recDirBox = new TextBox();
			this.label3 = new Label();
			this.driverRefButton = new Button();
			this.label7 = new Label();
			this.driverBox = new TextBox();
			this.label6 = new Label();
			this.tunerAddButton = new Button();
			this.removeButton = new Button();
			this.downButton = new Button();
			this.upButton = new Button();
			this.label5 = new Label();
			this.tunerNameBox = new TextBox();
			this.label2 = new Label();
			this.endButton = new Button();
			this.tvtestDialog = new SaveFileDialog();
			this.recDirDialog = new FolderBrowserDialog();
			this.driverDialog = new SaveFileDialog();
			this.tabControl1 = new TabControl();
			this.tabPage1 = new TabPage();
			this.unregStartupButton = new Button();
			this.regStartupButton = new Button();
			this.label4 = new Label();
			this.tabPage2 = new TabPage();
			this.tunerPanel = new Panel();
			this.tunerBox = new TreeView();
			this.fileNameOnlyCheck = new CheckBox();
			this.tunerUpdateCheck = new CheckBox();
			this.tabPage4 = new TabPage();
			this.postProcessRefButton = new Button();
			this.label13 = new Label();
			this.postProcessBox = new TextBox();
			this.label12 = new Label();
			this.recFileBox = new TextBox();
			this.label10 = new Label();
			this.label9 = new Label();
			this.startMarginBox = new NumericUpDown();
			this.endMarginBox = new NumericUpDown();
			this.tabPage3 = new TabPage();
			this.epgHourBox = new TextBox();
			this.label8 = new Label();
			this.label17 = new Label();
			this.label14 = new Label();
			this.autoSleepCheck = new CheckBox();
			this.postProcessDialog = new SaveFileDialog();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.tunerPanel.SuspendLayout();
			this.tabPage4.SuspendLayout();
			((ISupportInitialize)this.startMarginBox).BeginInit();
			((ISupportInitialize)this.endMarginBox).BeginInit();
			this.tabPage3.SuspendLayout();
			base.SuspendLayout();
			this.label1.AutoSize = true;
			this.label1.Location = new Point(35, 43);
			this.label1.Name = "label1";
			this.label1.Size = new Size(101, 19);
			this.label1.TabIndex = 0;
			this.label1.Text = "TVTestの場所";
			this.tvtestBox.Location = new Point(146, 40);
			this.tvtestBox.Name = "tvtestBox";
			this.tvtestBox.Size = new Size(359, 27);
			this.tvtestBox.TabIndex = 1;
			this.recDirRefButton.Location = new Point(511, 85);
			this.recDirRefButton.Name = "recDirRefButton";
			this.recDirRefButton.Size = new Size(114, 30);
			this.recDirRefButton.TabIndex = 5;
			this.recDirRefButton.Text = "参照...";
			this.recDirRefButton.UseVisualStyleBackColor = false;
			this.recDirRefButton.Click += new EventHandler(this.recDirRefButton_Click);
			this.tvtestRefButton.Location = new Point(511, 38);
			this.tvtestRefButton.Name = "tvtestRefButton";
			this.tvtestRefButton.Size = new Size(114, 30);
			this.tvtestRefButton.TabIndex = 2;
			this.tvtestRefButton.Text = "参照...";
			this.tvtestRefButton.UseVisualStyleBackColor = false;
			this.tvtestRefButton.Click += new EventHandler(this.tvtestRefButton_Click);
			this.recDirBox.Location = new Point(146, 86);
			this.recDirBox.Name = "recDirBox";
			this.recDirBox.Size = new Size(359, 27);
			this.recDirBox.TabIndex = 4;
			this.label3.AutoSize = true;
			this.label3.Location = new Point(55, 90);
			this.label3.Name = "label3";
			this.label3.Size = new Size(81, 19);
			this.label3.TabIndex = 3;
			this.label3.Text = "録画の場所";
			this.driverRefButton.Location = new Point(506, 60);
			this.driverRefButton.Name = "driverRefButton";
			this.driverRefButton.Size = new Size(114, 30);
			this.driverRefButton.TabIndex = 3;
			this.driverRefButton.Text = "参照...";
			this.driverRefButton.UseVisualStyleBackColor = false;
			this.driverRefButton.Click += new EventHandler(this.driverRefButton_Click);
			this.label7.AutoSize = true;
			this.label7.Location = new Point(44, 154);
			this.label7.Name = "label7";
			this.label7.Size = new Size(55, 19);
			this.label7.TabIndex = 7;
			this.label7.Text = "チューナ";
			this.driverBox.Location = new Point(120, 61);
			this.driverBox.Name = "driverBox";
			this.driverBox.Size = new Size(380, 27);
			this.driverBox.TabIndex = 2;
			this.label6.AutoSize = true;
			this.label6.Location = new Point(16, 16);
			this.label6.Name = "label6";
			this.label6.Size = new Size(391, 19);
			this.label6.TabIndex = 0;
			this.label6.Text = "Bonドライバ、チューナ名を指定して、追加ボタンを押してください。";
			this.tunerAddButton.Location = new Point(506, 105);
			this.tunerAddButton.Name = "tunerAddButton";
			this.tunerAddButton.Size = new Size(114, 30);
			this.tunerAddButton.TabIndex = 6;
			this.tunerAddButton.Text = "追加";
			this.tunerAddButton.UseVisualStyleBackColor = false;
			this.tunerAddButton.Click += new EventHandler(this.tunerAddButton_Click);
			this.removeButton.Location = new Point(506, 248);
			this.removeButton.Name = "removeButton";
			this.removeButton.Size = new Size(114, 30);
			this.removeButton.TabIndex = 11;
			this.removeButton.Text = "削除";
			this.removeButton.UseVisualStyleBackColor = false;
			this.removeButton.Click += new EventHandler(this.removeButton_Click);
			this.downButton.Location = new Point(506, 200);
			this.downButton.Name = "downButton";
			this.downButton.Size = new Size(114, 30);
			this.downButton.TabIndex = 10;
			this.downButton.Text = "下へ";
			this.downButton.UseVisualStyleBackColor = false;
			this.downButton.Click += new EventHandler(this.downButton_Click);
			this.upButton.Font = new Font("MS UI Gothic", 9f, FontStyle.Regular, GraphicsUnit.Point, 128);
			this.upButton.Location = new Point(506, 154);
			this.upButton.Name = "upButton";
			this.upButton.Size = new Size(114, 30);
			this.upButton.TabIndex = 9;
			this.upButton.Text = "上へ";
			this.upButton.UseVisualStyleBackColor = false;
			this.upButton.Click += new EventHandler(this.upButton_Click);
			this.label5.AutoSize = true;
			this.label5.Location = new Point(16, 64);
			this.label5.Name = "label5";
			this.label5.Size = new Size(82, 19);
			this.label5.TabIndex = 1;
			this.label5.Text = "Bonドライバ";
			this.tunerNameBox.Location = new Point(120, 106);
			this.tunerNameBox.Name = "tunerNameBox";
			this.tunerNameBox.Size = new Size(380, 27);
			this.tunerNameBox.TabIndex = 5;
			this.label2.AutoSize = true;
			this.label2.Location = new Point(29, 109);
			this.label2.Name = "label2";
			this.label2.Size = new Size(70, 19);
			this.label2.TabIndex = 4;
			this.label2.Text = "チューナ名";
			this.endButton.Location = new Point(555, 491);
			this.endButton.Name = "endButton";
			this.endButton.Size = new Size(114, 30);
			this.endButton.TabIndex = 1;
			this.endButton.Text = "設定完了";
			this.endButton.UseVisualStyleBackColor = false;
			this.endButton.Click += new EventHandler(this.endButton_Click);
			this.tvtestDialog.CheckFileExists = true;
			this.tvtestDialog.Filter = "TVTest|TVTest.exe";
			this.tvtestDialog.OverwritePrompt = false;
			this.tvtestDialog.Title = "TVTestの場所";
			this.recDirDialog.Description = "録画フォルダを選択してください。";
			this.recDirDialog.RootFolder = Environment.SpecialFolder.MyComputer;
			this.driverDialog.CheckFileExists = true;
			this.driverDialog.Filter = "BonDriver|Bondriver*.dll";
			this.driverDialog.OverwritePrompt = false;
			this.driverDialog.Title = "Bonドライバ";
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Controls.Add(this.tabPage4);
			this.tabControl1.Controls.Add(this.tabPage3);
			this.tabControl1.Location = new Point(12, 12);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new Size(661, 473);
			this.tabControl1.TabIndex = 0;
			this.tabPage1.Controls.Add(this.unregStartupButton);
			this.tabPage1.Controls.Add(this.regStartupButton);
			this.tabPage1.Controls.Add(this.label4);
			this.tabPage1.Controls.Add(this.recDirRefButton);
			this.tabPage1.Controls.Add(this.tvtestRefButton);
			this.tabPage1.Controls.Add(this.recDirBox);
			this.tabPage1.Controls.Add(this.label1);
			this.tabPage1.Controls.Add(this.label3);
			this.tabPage1.Controls.Add(this.tvtestBox);
			this.tabPage1.Location = new Point(4, 28);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new Padding(3);
			this.tabPage1.Size = new Size(653, 441);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "基本";
			this.tabPage1.UseVisualStyleBackColor = true;
			this.unregStartupButton.Location = new Point(330, 162);
			this.unregStartupButton.Name = "unregStartupButton";
			this.unregStartupButton.Size = new Size(175, 30);
			this.unregStartupButton.TabIndex = 7;
			this.unregStartupButton.Text = "登録解除...";
			this.unregStartupButton.UseVisualStyleBackColor = false;
			this.unregStartupButton.Click += new EventHandler(this.unregStartupButton_Click);
			this.regStartupButton.Location = new Point(146, 162);
			this.regStartupButton.Name = "regStartupButton";
			this.regStartupButton.Size = new Size(175, 30);
			this.regStartupButton.TabIndex = 6;
			this.regStartupButton.Text = "スタートアップ登録...";
			this.regStartupButton.UseVisualStyleBackColor = false;
			this.regStartupButton.Click += new EventHandler(this.regStartupButton_Click);
			this.label4.AutoSize = true;
			this.label4.Location = new Point(142, 281);
			this.label4.Name = "label4";
			this.label4.Size = new Size(348, 38);
			this.label4.TabIndex = 8;
			this.label4.Text = "初めてのセットアップ時は、チューナの設定を行ってください。\r\n上部にタブがあります。";
			this.tabPage2.Controls.Add(this.tunerPanel);
			this.tabPage2.Controls.Add(this.tunerUpdateCheck);
			this.tabPage2.Location = new Point(4, 28);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new Padding(3);
			this.tabPage2.Size = new Size(653, 441);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "チューナ";
			this.tabPage2.UseVisualStyleBackColor = true;
			this.tunerPanel.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left);
			this.tunerPanel.Controls.Add(this.tunerBox);
			this.tunerPanel.Controls.Add(this.tunerNameBox);
			this.tunerPanel.Controls.Add(this.upButton);
			this.tunerPanel.Controls.Add(this.fileNameOnlyCheck);
			this.tunerPanel.Controls.Add(this.downButton);
			this.tunerPanel.Controls.Add(this.driverRefButton);
			this.tunerPanel.Controls.Add(this.label7);
			this.tunerPanel.Controls.Add(this.removeButton);
			this.tunerPanel.Controls.Add(this.label6);
			this.tunerPanel.Controls.Add(this.label5);
			this.tunerPanel.Controls.Add(this.driverBox);
			this.tunerPanel.Controls.Add(this.tunerAddButton);
			this.tunerPanel.Controls.Add(this.label2);
			this.tunerPanel.Enabled = false;
			this.tunerPanel.Location = new Point(6, 48);
			this.tunerPanel.Name = "tunerPanel";
			this.tunerPanel.Size = new Size(641, 383);
			this.tunerPanel.TabIndex = 1;
			this.tunerBox.FullRowSelect = true;
			this.tunerBox.HideSelection = false;
			this.tunerBox.Indent = 5;
			this.tunerBox.Location = new Point(120, 154);
			this.tunerBox.Name = "tunerBox";
			this.tunerBox.ShowLines = false;
			this.tunerBox.ShowNodeToolTips = true;
			this.tunerBox.ShowPlusMinus = false;
			this.tunerBox.ShowRootLines = false;
			this.tunerBox.Size = new Size(380, 173);
			this.tunerBox.TabIndex = 8;
			this.fileNameOnlyCheck.AutoSize = true;
			this.fileNameOnlyCheck.Location = new Point(120, 333);
			this.fileNameOnlyCheck.Name = "fileNameOnlyCheck";
			this.fileNameOnlyCheck.Size = new Size(385, 23);
			this.fileNameOnlyCheck.TabIndex = 12;
			this.fileNameOnlyCheck.Text = "Bonドライバはファイル名のみ(ファイルの存在確認をしません)";
			this.fileNameOnlyCheck.UseVisualStyleBackColor = true;
			this.tunerUpdateCheck.AutoSize = true;
			this.tunerUpdateCheck.Location = new Point(26, 15);
			this.tunerUpdateCheck.Name = "tunerUpdateCheck";
			this.tunerUpdateCheck.Size = new Size(143, 23);
			this.tunerUpdateCheck.TabIndex = 0;
			this.tunerUpdateCheck.Text = "チューナ設定を行う";
			this.tunerUpdateCheck.UseVisualStyleBackColor = true;
			this.tunerUpdateCheck.CheckedChanged += new EventHandler(this.tunerUpdateCheck_CheckedChanged);
			this.tabPage4.Controls.Add(this.postProcessRefButton);
			this.tabPage4.Controls.Add(this.label13);
			this.tabPage4.Controls.Add(this.postProcessBox);
			this.tabPage4.Controls.Add(this.label12);
			this.tabPage4.Controls.Add(this.recFileBox);
			this.tabPage4.Controls.Add(this.label10);
			this.tabPage4.Controls.Add(this.label9);
			this.tabPage4.Controls.Add(this.startMarginBox);
			this.tabPage4.Controls.Add(this.endMarginBox);
			this.tabPage4.Location = new Point(4, 28);
			this.tabPage4.Name = "tabPage4";
			this.tabPage4.Padding = new Padding(3);
			this.tabPage4.Size = new Size(653, 441);
			this.tabPage4.TabIndex = 3;
			this.tabPage4.Text = "録画設定";
			this.tabPage4.UseVisualStyleBackColor = true;
			this.postProcessRefButton.Location = new Point(508, 85);
			this.postProcessRefButton.Name = "postProcessRefButton";
			this.postProcessRefButton.Size = new Size(114, 30);
			this.postProcessRefButton.TabIndex = 4;
			this.postProcessRefButton.Text = "参照...";
			this.postProcessRefButton.UseVisualStyleBackColor = false;
			this.postProcessRefButton.Click += new EventHandler(this.postProcessRefButton_Click);
			this.label13.AutoSize = true;
			this.label13.Location = new Point(42, 88);
			this.label13.Name = "label13";
			this.label13.Size = new Size(99, 19);
			this.label13.TabIndex = 2;
			this.label13.Text = "録画後プロセス";
			this.postProcessBox.Location = new Point(161, 85);
			this.postProcessBox.Name = "postProcessBox";
			this.postProcessBox.Size = new Size(341, 27);
			this.postProcessBox.TabIndex = 3;
			this.label12.AutoSize = true;
			this.label12.Location = new Point(45, 39);
			this.label12.Name = "label12";
			this.label12.Size = new Size(96, 19);
			this.label12.TabIndex = 0;
			this.label12.Text = "録画ファイル名";
			this.recFileBox.Location = new Point(161, 36);
			this.recFileBox.Name = "recFileBox";
			this.recFileBox.Size = new Size(461, 27);
			this.recFileBox.TabIndex = 1;
			this.label10.AutoSize = true;
			this.label10.Location = new Point(27, 182);
			this.label10.Name = "label10";
			this.label10.Size = new Size(114, 19);
			this.label10.TabIndex = 7;
			this.label10.Text = "終了マージン(秒)";
			this.label9.AutoSize = true;
			this.label9.Location = new Point(27, 146);
			this.label9.Name = "label9";
			this.label9.Size = new Size(114, 19);
			this.label9.TabIndex = 5;
			this.label9.Text = "開始マージン(秒)";
			this.startMarginBox.Location = new Point(161, 144);
			NumericUpDown arg_1584_0 = this.startMarginBox;
			int[] expr_1577 = new int[4];
			expr_1577[0] = 180;
			arg_1584_0.Maximum = new decimal(expr_1577);
			this.startMarginBox.Minimum = new decimal(new int[]
			{
				180,
				0,
				0,
				-2147483648
			});
			this.startMarginBox.Name = "startMarginBox";
			this.startMarginBox.Size = new Size(99, 27);
			this.startMarginBox.TabIndex = 6;
			this.endMarginBox.Location = new Point(161, 180);
			NumericUpDown arg_1612_0 = this.endMarginBox;
			int[] expr_1605 = new int[4];
			expr_1605[0] = 180;
			arg_1612_0.Maximum = new decimal(expr_1605);
			this.endMarginBox.Minimum = new decimal(new int[]
			{
				180,
				0,
				0,
				-2147483648
			});
			this.endMarginBox.Name = "endMarginBox";
			this.endMarginBox.Size = new Size(99, 27);
			this.endMarginBox.TabIndex = 8;
			this.tabPage3.Controls.Add(this.epgHourBox);
			this.tabPage3.Controls.Add(this.label8);
			this.tabPage3.Controls.Add(this.label17);
			this.tabPage3.Controls.Add(this.label14);
			this.tabPage3.Controls.Add(this.autoSleepCheck);
			this.tabPage3.Location = new Point(4, 28);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Padding = new Padding(3);
			this.tabPage3.Size = new Size(653, 441);
			this.tabPage3.TabIndex = 2;
			this.tabPage3.Text = "その他";
			this.tabPage3.UseVisualStyleBackColor = true;
			this.epgHourBox.Location = new Point(208, 43);
			this.epgHourBox.Name = "epgHourBox";
			this.epgHourBox.Size = new Size(142, 27);
			this.epgHourBox.TabIndex = 4;
			this.label8.Location = new Point(53, 179);
			this.label8.Name = "label8";
			this.label8.Size = new Size(564, 80);
			this.label8.TabIndex = 7;
			this.label8.Text = "スリープからの自動復帰時、録画終了後にスリープ状態に戻す機能です。\r\n（録画後プロセスが指定されているときは無視され、OFFになります）\r\n";
			this.label17.AutoSize = true;
			this.label17.Location = new Point(374, 46);
			this.label17.Name = "label17";
			this.label17.Size = new Size(232, 57);
			this.label17.TabIndex = 5;
			this.label17.Text = "毎日実行されます。\r\nカンマで区切って、複数指定できます。\r\n(例) 9,15,21";
			this.label14.AutoSize = true;
			this.label14.Location = new Point(49, 46);
			this.label14.Name = "label14";
			this.label14.Size = new Size(143, 19);
			this.label14.TabIndex = 3;
			this.label14.Text = "番組表取得時刻(時)";
			this.autoSleepCheck.AutoSize = true;
			this.autoSleepCheck.Location = new Point(57, 141);
			this.autoSleepCheck.Name = "autoSleepCheck";
			this.autoSleepCheck.Size = new Size(140, 23);
			this.autoSleepCheck.TabIndex = 6;
			this.autoSleepCheck.Text = "自動スリープを行う\r\n";
			this.autoSleepCheck.UseVisualStyleBackColor = true;
			this.postProcessDialog.CheckFileExists = true;
			this.postProcessDialog.Filter = "実行ファイル (*.exe *.bat)|*.exe;*.bat";
			this.postProcessDialog.OverwritePrompt = false;
			this.postProcessDialog.Title = "録画後プロセス";
			base.AutoScaleDimensions = new SizeF(120f, 120f);
			base.AutoScaleMode = AutoScaleMode.Dpi;
			this.AutoScroll = true;
			base.ClientSize = new Size(685, 533);
			base.Controls.Add(this.endButton);
			base.Controls.Add(this.tabControl1);
			this.Font = new Font("Meiryo UI", 9f, FontStyle.Regular, GraphicsUnit.Point, 128);
			base.FormBorderStyle = FormBorderStyle.FixedDialog;
			base.Icon = (Icon)componentResourceManager.GetObject("$this.Icon");
			base.Margin = new Padding(3, 5, 3, 5);
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Name = "SetupForm";
			base.StartPosition = FormStartPosition.CenterScreen;
			this.Text = "Tvmaid セットアップ";
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage1.PerformLayout();
			this.tabPage2.ResumeLayout(false);
			this.tabPage2.PerformLayout();
			this.tunerPanel.ResumeLayout(false);
			this.tunerPanel.PerformLayout();
			this.tabPage4.ResumeLayout(false);
			this.tabPage4.PerformLayout();
			((ISupportInitialize)this.startMarginBox).EndInit();
			((ISupportInitialize)this.endMarginBox).EndInit();
			this.tabPage3.ResumeLayout(false);
			this.tabPage3.PerformLayout();
			base.ResumeLayout(false);
		}
	}
}
