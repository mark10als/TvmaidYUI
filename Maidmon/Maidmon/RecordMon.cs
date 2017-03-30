using Codeplex.Data;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using Tvmaid;

namespace Maidmon
{
	public class RecordMon : Form
	{
		private class Record
		{
			public int Id;

			public string Title;

			public string Service;

			public DateTime Start;

			public DateTime End;

			public int Status;

			public string Host;

			public string Tuner;

			public string Time;
		}

		private enum RecStatus
		{
			Enable = 1,
			EventMode,
			Duplication = 32,
			Recoding = 64,
			Complete = 128
		}

		private List<RecordMon.Record> records = new List<RecordMon.Record>();

		private List<string> hosts = new List<string>();

		private Dictionary<string, bool> actives = new Dictionary<string, bool>();

		private object downloadLock = 0;

		private PairList stateDef;

		private IContainer components;

		private ListView listView;

		private ColumnHeader titleHeader;

		private ColumnHeader timeHeader;

		private ColumnHeader statusHeader;

		private ColumnHeader hostHeader;

		private ColumnHeader tunerHeader;

		private ColumnHeader serviceHeader;

		private ToolStrip toolBar;

		private ToolStripButton updateButton;

		private ToolStripSeparator toolStripSeparator1;

		private Timer timer;

		private Timer downloadTimer;

		private ContextMenuStrip contextMenu;

		private ToolStripMenuItem fontChangeMenuItem;

		public RecordMon()
		{
			this.InitializeComponent();
			RecordMon.EnableDoubleBuffer(this.listView);
			try
			{
				this.LoadState();
				this.SetHost();
				this.Download();
				this.downloadTimer.Enabled = true;
				this.timer.Enabled = true;
			}
			catch (Exception ex)
			{
				MessageBox.Show("エラーが発生しました。[詳細] " + ex.Message, this.Text);
			}
		}

		private static void EnableDoubleBuffer(Control c)
		{
			c.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(c, true, null);
		}

		private void SetHost()
		{
			if (Environment.GetCommandLineArgs().Length == 1)
			{
				this.hosts.Add("localhost:20001");
			}
			else
			{
				this.hosts.AddRange(Environment.GetCommandLineArgs());
				this.hosts.RemoveAt(0);
			}
			foreach (string current in this.hosts)
			{
				ToolStripButton toolStripButton = new ToolStripButton();
				toolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
				toolStripButton.Enabled = false;
				toolStripButton.Margin = new Padding(10, 1, 0, 2);
				toolStripButton.Name = current;
				toolStripButton.Text = current;
				toolStripButton.Click += new EventHandler(this.hostButton_Click);
				this.toolBar.Items.Add(toolStripButton);
				this.actives[current] = false;
			}
		}

		private void hostButton_Click(object sender, EventArgs arg)
		{
			try
			{
				Process.Start("http://" + ((ToolStripButton)sender).Text + "/maid/record.html");
			}
			catch (Exception ex)
			{
				MessageBox.Show("ブラウザの起動に失敗しました。[詳細]" + ex.Message, this.Text);
			}
		}

		private void MaidMon_FormClosing(object sender, FormClosingEventArgs e)
		{
			PairList expr_06 = this.stateDef;
			expr_06["font"] = this.Font.FontFamily.Name;
			expr_06["fontsize"] = this.Font.SizeInPoints.ToString();
			expr_06["left"] = base.Left.ToString();
			expr_06["top"] = base.Top.ToString();
			expr_06["width"] = base.Width.ToString();
			expr_06["height"] = base.Height.ToString();
			expr_06["title.width"] = this.titleHeader.Width.ToString();
			expr_06["service.width"] = this.serviceHeader.Width.ToString();
			expr_06["time.width"] = this.timeHeader.Width.ToString();
			expr_06["status.width"] = this.statusHeader.Width.ToString();
			expr_06["host.width"] = this.hostHeader.Width.ToString();
			expr_06["tuner.width"] = this.tunerHeader.Width.ToString();
			this.stateDef.Save();
		}

		private void LoadState()
		{
			Util.CopyUserFile();
			this.stateDef = new PairList(Util.GetUserPath("maidmon.state.def"));
			this.stateDef.Load();
			PairList pairList = this.stateDef;
			if (pairList.IsDefined("font") && pairList.IsDefined("fontsize"))
			{
				FontFamily family = new FontFamily(pairList["font"]);
				this.Font = new Font(family, pairList.GetFloat("fontsize"));
			}
			base.Left = pairList["left"].ToInt();
			base.Top = pairList["top"].ToInt();
			base.Width = pairList["width"].ToInt();
			base.Height = pairList["height"].ToInt();
			this.titleHeader.Width = pairList["title.width"].ToInt();
			this.serviceHeader.Width = pairList["service.width"].ToInt();
			this.timeHeader.Width = pairList["time.width"].ToInt();
			this.statusHeader.Width = pairList["status.width"].ToInt();
			this.hostHeader.Width = pairList["host.width"].ToInt();
			this.tunerHeader.Width = pairList["tuner.width"].ToInt();
		}

		private void listView_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
		{
			List<RecordMon.Record> obj = this.records;
			lock (obj)
			{
				try
				{
					RecordMon.Record record;
					if (e.ItemIndex >= this.records.Count)
					{
						record = new RecordMon.Record();
						record.Id = -1;
						record.Service = "";
						record.Start = DateTime.Now;
						record.End = DateTime.Now;
						record.Title = "";
						record.Status = 0;
						record.Tuner = "";
						record.Host = "";
						record.Time = "";
					}
					else
					{
						record = this.records[e.ItemIndex];
					}
					ListViewItem listViewItem = new ListViewItem();
					listViewItem.Text = record.Title;
					listViewItem.SubItems.Add(record.Service);
					listViewItem.SubItems.Add(record.Time);
					string text = "OK";
					if ((record.Status & 32) > 0)
					{
						text = "重複";
						listViewItem.BackColor = Color.Gold;
					}
					if ((record.Status & 64) > 0)
					{
						text = "録画中";
						listViewItem.BackColor = Color.LightCoral;
					}
					listViewItem.SubItems.Add(text);
					listViewItem.SubItems.Add(record.Host);
					listViewItem.SubItems.Add(record.Tuner);
					e.Item = listViewItem;
				}
				catch
				{
				}
			}
		}

		private void downloadTimer_Tick(object sender, EventArgs e)
		{
			this.Download();
		}

		private void timer_Tick(object sender, EventArgs e)
		{
			List<RecordMon.Record> obj = this.records;
			lock (obj)
			{
				this.listView.VirtualListSize = this.records.Count;
				this.listView.Invalidate();
			}
			foreach (string current in this.hosts)
			{
				this.toolBar.Items[current].Enabled = this.actives[current];
			}
		}

		private void Download()
		{
			Task.Factory.StartNew(delegate
			{
				object obj = this.downloadLock;
				lock (obj)
				{
					Dictionary<string, string> dictionary = new Dictionary<string, string>();
					dictionary.Clear();
					foreach (string current in this.hosts)
					{
						dictionary[current] = this.Download(current);
						this.actives[current] = (dictionary[current] != null);
					}
					List<RecordMon.Record> obj2 = this.records;
					lock (obj2)
					{
						this.records.Clear();
						foreach (string current2 in this.hosts)
						{
							this.SetRecord(current2, dictionary[current2]);
						}
						this.records.Sort((x, y) =>
						{
							if (x.Start == y.Start)
							{
								return 0;
							}
							if (x.Start < y.Start)
							{
								return -1;
							}
							return 1;
						});
					}
				}
			}, TaskCreationOptions.AttachedToParent);
		}

		private void SetRecord(string host, string data)
		{
			if (data == null)
			{
				return;
			}

			var ret = DynamicJson.Parse(data);
			if (ret.Code == 0)
			{
				foreach (var record in (dynamic[])ret.Data1)
				{
					Record rec = new Record();
					rec.Id = (int)record[0];
					rec.Service = record[1];
					rec.Start = new DateTime((long)record[2]);
					rec.End = new DateTime((long)record[3]);
					rec.Title = record[4];
					rec.Status = (int)record[5];
					rec.Tuner = record[6];
					rec.Host = host;
					rec.Time = rec.Start.ToString("dd(ddd) HH:mm-") + rec.End.ToString("HH:mm");
					records.Add(rec);
				}
			}
		}

		private string Download(string host)
		{
			WebClient webClient = new WebClient();
			string result;
			try
			{
				webClient.Encoding = Encoding.UTF8;
				string text = "select record.id, service.name, start, end, title, status, tuner from record left join service on record.fsid = service.fsid" + " where status & 1 and end > {0} group by record.id order by start".Formatex(new object[]
				{
					DateTime.Now.Ticks
				});
				text = HttpUtility.UrlEncode(text, Encoding.UTF8);
				string address = "http://" + host + "/webapi/GetTable?sql=" + text;
				result = webClient.DownloadString(address);
			}
			catch
			{
				result = null;
			}
			finally
			{
				webClient.Dispose();
			}
			return result;
		}

		private void updateButton_Click(object sender, EventArgs e)
		{
			this.Download();
		}

		private void listView_ItemActivate(object sender, EventArgs arg)
		{
			try
			{
				List<RecordMon.Record> obj = this.records;
				lock (obj)
				{
					RecordMon.Record record = this.records[this.listView.FocusedItem.Index];
					Process.Start(string.Concat(new object[]
					{
						"http://",
						record.Host,
						"/maid/record-edit.html?id=",
						record.Id
					}));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("ブラウザの起動に失敗しました。[詳細]" + ex.Message, this.Text);
			}
		}

		private void fontChangeMenuItem_Click(object sender, EventArgs e)
		{
			FontDialog fontDialog = new FontDialog();
			fontDialog.Font = this.listView.Font;
			if (fontDialog.ShowDialog() != DialogResult.Cancel)
			{
				this.Font = fontDialog.Font;
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && this.components != null)
			{
				this.components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			this.components = new Container();
			ComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof(RecordMon));
			this.listView = new ListView();
			this.titleHeader = new ColumnHeader();
			this.serviceHeader = new ColumnHeader();
			this.timeHeader = new ColumnHeader();
			this.statusHeader = new ColumnHeader();
			this.hostHeader = new ColumnHeader();
			this.tunerHeader = new ColumnHeader();
			this.contextMenu = new ContextMenuStrip(this.components);
			this.fontChangeMenuItem = new ToolStripMenuItem();
			this.toolBar = new ToolStrip();
			this.updateButton = new ToolStripButton();
			this.toolStripSeparator1 = new ToolStripSeparator();
			this.timer = new Timer(this.components);
			this.downloadTimer = new Timer(this.components);
			this.contextMenu.SuspendLayout();
			this.toolBar.SuspendLayout();
			base.SuspendLayout();
			this.listView.BorderStyle = BorderStyle.None;
			this.listView.Columns.AddRange(new ColumnHeader[]
			{
				this.titleHeader,
				this.serviceHeader,
				this.timeHeader,
				this.statusHeader,
				this.hostHeader,
				this.tunerHeader
			});
			this.listView.ContextMenuStrip = this.contextMenu;
			this.listView.Dock = DockStyle.Fill;
			this.listView.FullRowSelect = true;
			this.listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
			this.listView.Location = new Point(0, 0);
			this.listView.Margin = new Padding(3, 4, 3, 4);
			this.listView.MultiSelect = false;
			this.listView.Name = "listView";
			this.listView.ShowItemToolTips = true;
			this.listView.Size = new Size(866, 146);
			this.listView.TabIndex = 0;
			this.listView.UseCompatibleStateImageBehavior = false;
			this.listView.View = View.Details;
			this.listView.VirtualMode = true;
			this.listView.ItemActivate += new EventHandler(this.listView_ItemActivate);
			this.listView.RetrieveVirtualItem += new RetrieveVirtualItemEventHandler(this.listView_RetrieveVirtualItem);
			this.titleHeader.Text = "タイトル";
			this.titleHeader.Width = 250;
			this.serviceHeader.Text = "サービス";
			this.serviceHeader.Width = 97;
			this.timeHeader.Text = "予約日時";
			this.timeHeader.Width = 173;
			this.statusHeader.Text = "状態";
			this.statusHeader.Width = 115;
			this.hostHeader.Text = "ホスト";
			this.hostHeader.Width = 107;
			this.tunerHeader.Text = "チューナ";
			this.tunerHeader.Width = 113;
			this.contextMenu.ImageScalingSize = new Size(20, 20);
			this.contextMenu.Items.AddRange(new ToolStripItem[]
			{
				this.fontChangeMenuItem
			});
			this.contextMenu.Name = "contextMenu";
			this.contextMenu.Size = new Size(176, 28);
			this.fontChangeMenuItem.Name = "fontChangeMenuItem";
			this.fontChangeMenuItem.Size = new Size(175, 24);
			this.fontChangeMenuItem.Text = "フォント変更(&F)...";
			this.fontChangeMenuItem.Click += new EventHandler(this.fontChangeMenuItem_Click);
			this.toolBar.Dock = DockStyle.Bottom;
			this.toolBar.GripStyle = ToolStripGripStyle.Hidden;
			this.toolBar.ImageScalingSize = new Size(20, 20);
			this.toolBar.Items.AddRange(new ToolStripItem[]
			{
				this.updateButton,
				this.toolStripSeparator1
			});
			this.toolBar.Location = new Point(0, 146);
			this.toolBar.Name = "toolBar";
			this.toolBar.RenderMode = ToolStripRenderMode.System;
			this.toolBar.Size = new Size(866, 27);
			this.toolBar.TabIndex = 1;
			this.updateButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
			this.updateButton.ImageTransparentColor = Color.Magenta;
			this.updateButton.Name = "updateButton";
			this.updateButton.Size = new Size(43, 24);
			this.updateButton.Text = "更新";
			this.updateButton.ToolTipText = "すぐに更新";
			this.updateButton.Click += new EventHandler(this.updateButton_Click);
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new Size(6, 27);
			this.timer.Interval = 5000;
			this.timer.Tick += new EventHandler(this.timer_Tick);
			this.downloadTimer.Interval = 20000;
			this.downloadTimer.Tick += new EventHandler(this.downloadTimer_Tick);
			base.AutoScaleDimensions = new SizeF(9f, 19f);
			base.AutoScaleMode = AutoScaleMode.Font;
			base.ClientSize = new Size(866, 173);
			base.Controls.Add(this.listView);
			base.Controls.Add(this.toolBar);
			this.Font = new Font("Meiryo UI", 9f, FontStyle.Regular, GraphicsUnit.Point, 128);
			base.Icon = (Icon)componentResourceManager.GetObject("$this.Icon");
			base.Margin = new Padding(3, 4, 3, 4);
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Name = "RecordMon";
			base.ShowInTaskbar = false;
			base.StartPosition = FormStartPosition.Manual;
			this.Text = "メイドモニタ";
			base.FormClosing += new FormClosingEventHandler(this.MaidMon_FormClosing);
			this.contextMenu.ResumeLayout(false);
			this.toolBar.ResumeLayout(false);
			this.toolBar.PerformLayout();
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}
}
