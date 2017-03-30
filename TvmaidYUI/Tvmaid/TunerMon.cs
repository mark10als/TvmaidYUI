using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tvmaid
{
	public class TunerMon : Form
	{
		private class ServiceInfo
		{
			public int Id;

			public string Name;

			public string EventTime = "";

			public string EventTitle = "";

			public int EventId = -1;
		}

		private Sql sql;

		private Tuner curTuner;

		private List<TunerMon.ServiceInfo> services = new List<TunerMon.ServiceInfo>();

		private PairList state;

		private SleepMan sleepMan = new SleepMan();

		private IContainer components;

		private ListView serviceView;

		private SplitContainer split1;

		private SplitContainer split2;

		private Timer serviceTimer;

		private ColumnHeader nameHeader;

		private ColumnHeader timeHeader;

		private ColumnHeader eventHeader;

		private TreeView tunerView;

		private ContextMenuStrip serviceMenu;

		private ToolStripMenuItem viewMenuItem;

		private ToolStripSeparator toolStripMenuItem1;

		private ToolStripMenuItem recordAddMenuItem;

		private Timer tunerTimer;

		private Timer logTimer;

		private ToolStripMenuItem recordRemoveMenuItem;

		private NotifyIcon notifyIcon;

		private TextBox logBox;

		private Panel panel1;

		private ContextMenuStrip trayMenu;

		private ToolStripMenuItem exitMenuItem;

		private ToolStripMenuItem updateTunerMenuItem;

		private ToolStripSeparator toolStripMenuItem3;

		private ToolStripMenuItem stopEpgMenuItem;

		private ToolStripMenuItem startEpgMenuItem;

		private ToolStripSeparator toolStripSeparator1;

		private ToolStripMenuItem sleepMenuItem;

		private ToolStripSeparator toolStripSeparator2;

		private ToolStripMenuItem tunerMonMenuItem;

		private ToolStripMenuItem testToolStripMenuItem;

		private ToolStripMenuItem test2ToolStripMenuItem;

		private ToolStripMenuItem closeTunerMenuItem;

		private ToolStripSeparator toolStripMenuItem2;

		private ToolStripMenuItem fontChangeMenuItem;

		private ToolStripMenuItem openEpgMenuItem;

		private ToolStripMenuItem setupMenuItem;

		private ToolStripMenuItem startServiceEpgMenuItem;

		private ToolStripSeparator toolStripMenuItem4;

		public TunerMon()
		{
			this.InitializeComponent();
			TunerMon.EnableDoubleBuffer(this.serviceView);
			try
			{
				this.LoadState();
				this.sql = new Sql(true);
				this.InitTunerView();
				base.ActiveControl = this.tunerView;
				if (this.tunerView.Nodes.Count > 0)
				{
					this.tunerView.SelectedNode = this.tunerView.Nodes[0];
				}
			}
			catch (Exception ex)
			{
				Log.Write("エラーが発生しました。[詳細] " + ex.Message);
				Log.Write(1, ex.StackTrace);
			}
		}

		private static void EnableDoubleBuffer(Control c)
		{
			c.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(c, true, null);
		}

		private void LoadState()
		{
			this.state = new PairList(Util.GetUserPath("state.def"));
			this.state.Load();
			if (this.state.IsDefined("font") && this.state.IsDefined("fontsize"))
			{
				FontFamily family = new FontFamily(this.state["font"]);
				this.Font = new Font(family, this.state.GetFloat("fontsize"));
			}
			base.Left = this.state.GetInt("left");
			base.Top = this.state.GetInt("top");
			base.Width = this.state.GetInt("width");
			base.Height = this.state.GetInt("height");
			this.split1.SplitterDistance = this.state.GetInt("split1");
			this.split2.SplitterDistance = this.state.GetInt("split2");
			this.nameHeader.Width = this.state.GetInt("column.name");
			this.timeHeader.Width = this.state.GetInt("column.time");
			this.eventHeader.Width = this.state.GetInt("column.event");
			base.WindowState = (FormWindowState)this.state.GetInt("window");
		}

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);
			if (536 == m.Msg)
			{
				int num = m.WParam.ToInt32();
				if (num != 4)
				{
					if (num == 18)
					{
						this.sleepMan.OnResume();
						return;
					}
				}
				else
				{
					this.sleepMan.OnSuspend();
				}
			}
		}

		private void InitTunerView()
		{
			this.sql.Text = "select * from tuner order by id";
			using (DataTable table = this.sql.GetTable())
			{
				while (table.Read())
				{
					Tuner tuner = new Tuner(table);
					TreeNode treeNode = new TreeNode(tuner.Name);
					treeNode.Tag = tuner;
					this.tunerView.Nodes.Add(treeNode);
				}
			}
			this.tunerTimer.Enabled = true;
		}

		private void TunerMon_FormClosing(object sender, FormClosingEventArgs arg)
		{
			if (Program.ExitMode != 0)
			{
				return;
			}
			if (arg.CloseReason == CloseReason.WindowsShutDown)
			{
				return;
			}
			if (MessageBox.Show("終了していいですか？", AppData.AppName, MessageBoxButtons.OKCancel) != DialogResult.OK)
			{
				arg.Cancel = true;
			}
		}

		private void TunerMon_FormClosed(object sender, FormClosedEventArgs e)
		{
			this.sleepMan.Dispose();
			this.SaveState();
		}

		private void SaveState()
		{
			try
			{
				this.state["window"] = ((int)base.WindowState).ToString();
				base.WindowState = FormWindowState.Normal;
				this.state["font"] = this.Font.FontFamily.Name;
				this.state["fontsize"] = this.Font.SizeInPoints.ToString();
				this.state["left"] = base.Left.ToString();
				this.state["top"] = base.Top.ToString();
				this.state["width"] = base.Width.ToString();
				this.state["height"] = base.Height.ToString();
				this.state["split1"] = this.split1.SplitterDistance.ToString();
				this.state["split2"] = this.split2.SplitterDistance.ToString();
				this.state["column.name"] = this.nameHeader.Width.ToString();
				this.state["column.time"] = this.timeHeader.Width.ToString();
				this.state["column.event"] = this.eventHeader.Width.ToString();
				this.state.Save();
				this.sql.Dispose();
			}
			catch (Exception ex)
			{
				Log.Write("エラーが発生しました。[詳細] " + ex.Message);
				Log.Write(1, ex.StackTrace);
			}
		}

		private void serviceTimer_Tick(object sender, EventArgs arg)
		{
			if (!base.Visible)
			{
				return;
			}
			try
			{
				this.SetService();
				this.serviceView.Invalidate();
			}
			catch (Exception ex)
			{
				Log.Write("エラーが発生しました。[詳細] " + ex.Message);
				Log.Write(1, ex.StackTrace);
			}
		}

		private void serviceView_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
		{
			try
			{
				TunerMon.ServiceInfo serviceInfo = this.services[e.ItemIndex];
				ListViewItem listViewItem = new ListViewItem();
				listViewItem.Text = serviceInfo.Name;
				ListViewItem.ListViewSubItem item = new ListViewItem.ListViewSubItem(listViewItem, serviceInfo.EventTime);
				listViewItem.SubItems.Add(item);
				ListViewItem.ListViewSubItem item2 = new ListViewItem.ListViewSubItem(listViewItem, serviceInfo.EventTitle);
				listViewItem.SubItems.Add(item2);
				e.Item = listViewItem;
			}
			catch (Exception ex)
			{
				Log.Write(ex.Message);
				Log.Write(1, ex.StackTrace);
			}
		}

		private void SetService()
		{
			this.serviceTimer.Enabled = false;
			this.sql.Text = "select service.id, name, start, end, title, event1.id from service \r\n                        left join\r\n                        (select id,fsid, start,end, title from event\r\n                        where start < {1} and end > {1}) as event1\r\n                        on service.fsid = event1.fsid\r\n                        where driver = '{0}' \r\n                        order by service.id".Formatex(new object[]
			{
				Sql.SqlEncode(this.curTuner.Driver),
				DateTime.Now.Ticks
			});
			using (DataTable table = this.sql.GetTable())
			{
				this.services.Clear();
				while (table.Read())
				{
					TunerMon.ServiceInfo serviceInfo = new TunerMon.ServiceInfo();
					serviceInfo.Id = table.GetInt(0);
					serviceInfo.Name = table.GetStr(1);
					if (!table.IsNull(2))
					{
						DateTime dateTime = new DateTime(table.GetLong(2));
						DateTime dateTime2 = new DateTime(table.GetLong(3));
						serviceInfo.EventTime = dateTime.ToString("HH:mm") + "-" + dateTime2.ToString("HH:mm");
						serviceInfo.EventTitle = table.GetStr(4);
						serviceInfo.EventId = table.GetInt(5);
					}
					this.services.Add(serviceInfo);
				}
			}
			this.serviceView.VirtualListSize = this.services.Count;
			this.serviceTimer.Enabled = true;
		}

		private void tunerView_AfterSelect(object sender, TreeViewEventArgs arg)
		{
			try
			{
				this.curTuner = (Tuner)arg.Node.Tag;
				this.SetService();
				this.serviceView.Refresh();
			}
			catch (Exception ex)
			{
				Log.Write("エラーが発生しました。[詳細] " + ex.Message);
				Log.Write(1, ex.StackTrace);
			}
		}

		private void serviceView_ItemActivate(object sender, EventArgs arg)
		{
			this.View();
		}

		private void View()
		{
			try
			{
				int id = this.services[this.serviceView.FocusedItem.Index].Id;
				Task.Factory.StartNew(delegate
				{
					try
					{
						Service service = new Service(this.sql, id);
						this.curTuner.Open(true);
						this.curTuner.SetService(service);
					}
					catch (Exception ex2)
					{
						Log.Write("サービス切り替えに失敗しました。[詳細]" + ex2.Message);
						Log.Write(1, ex2.StackTrace);
					}
				}, TaskCreationOptions.AttachedToParent);
			}
			catch (Exception ex)
			{
				Log.Write("エラーが発生しました。[詳細]" + ex.Message);
				Log.Write(1, ex.StackTrace);
			}
		}

		private void viewMenuItem_Click(object sender, EventArgs e)
		{
			this.View();
		}

		private void recordAddMenuItem_Click(object sender, EventArgs arg)
		{
			try
			{
				TunerMon.ServiceInfo serviceInfo = this.services[this.serviceView.FocusedItem.Index];
				if (serviceInfo.EventId == -1)
				{
					if (MessageBox.Show("番組情報が無いため予約できません。TVTestの録画機能で録画してください。TVTestを開きますか？", AppData.AppName, MessageBoxButtons.OKCancel) == DialogResult.OK)
					{
						this.View();
					}
				}
				else
				{
					Record arg_5A_0 = new Record();
					Event @event = new Event(this.sql, serviceInfo.EventId);
					arg_5A_0.Fsid = @event.Fsid;
					arg_5A_0.Eid = @event.Eid;
					arg_5A_0.StartTime = @event.Start;
					arg_5A_0.Duration = @event.Duration;
					arg_5A_0.Title = @event.Title;
					arg_5A_0.Add(this.sql);
				}
			}
			catch (Exception ex)
			{
				Log.Write("予約できませんでした。[詳細] " + ex.Message);
				Log.Write(1, ex.StackTrace);
			}
		}

		private void recordRemoveMenuItem_Click(object sender, EventArgs arg)
		{
			try
			{
				Record record = null;
				this.sql.Text = "select * from record where status & {0} and tuner = '{1}'".Formatex(new object[]
				{
					64,
					this.curTuner.Name
				});
				using (DataTable table = this.sql.GetTable())
				{
					if (table.Read())
					{
						record = new Record(table);
					}
				}
				if (record != null)
				{
					if (record.Auto == -1 || record.EndTime < DateTime.Now)
					{
						record.Remove(this.sql, false);
					}
					else
					{
						record.SetEnable(this.sql, false);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Write("エラーが発生しました。[詳細] " + ex.Message);
				Log.Write(1, ex.StackTrace);
			}
		}

		private void tunerTimer_Tick(object sender, EventArgs arg)
		{
			if (!base.Visible)
			{
				return;
			}
			try
			{
				string[] str = new string[]
				{
					"(視聴)",
					"(録画)",
					"(録画一時停止)",
					"",
					"(応答なし)"
				};
				IEnumerator enumerator = this.tunerView.Nodes.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						TreeNode node = (TreeNode)enumerator.Current;
						Tuner tuner = (Tuner)node.Tag;
						int state = 0;
						Task.Factory.StartNew(() =>
						{
							state = (int)tuner.GetState();
						}, TaskCreationOptions.AttachedToParent).ContinueWith((_) =>
						{
							node.Text = "{0} {1}".Formatex(new object[]
							{
								tuner.Name,
								str[state]
							});
						}, TaskScheduler.FromCurrentSynchronizationContext());
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
				this.tunerView.Invalidate();
			}
			catch (Exception ex)
			{
				Log.Write("エラーが発生しました。[詳細] " + ex.Message);
				Log.Write(1, ex.StackTrace);
			}
		}

		private void logTimer_Tick(object sender, EventArgs e)
		{
			for (string str = Log.Read(); str != null; str = Log.Read())
			{
				this.AppendText(str + "\r\n");
			}
		}

		private void AppendText(string str)
		{
			int num = 200;
			if (this.logBox.Lines.Length > num)
			{
				string[] array = new string[num / 3 * 2];
				int num2 = this.logBox.Lines.Length - array.Length;
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = this.logBox.Lines[num2 + i];
				}
				this.logBox.Lines = array;
			}
			this.logBox.AppendText(str);
		}

		private void TunerMon_VisibleChanged(object sender, EventArgs e)
		{
			this.tunerTimer.Enabled = base.Visible;
		}

		private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			this.tunerMonMenuItem_Click(null, null);
		}

		private void exitMenuItem_Click(object sender, EventArgs e)
		{
			base.Close();
		}

		private void updateTunerMenuItem_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("チューナ更新していいですか？\n続行すると、Tvmaidを再起動し、更新処理を行います。", AppData.AppName, MessageBoxButtons.OKCancel) == DialogResult.OK)
			{
				Program.ExitMode = 1;
				base.Close();
			}
		}

		private void stopEpgMenuItem_Click(object sender, EventArgs e)
		{
			RecTimer.GetInstance().StopEpg();
		}

		private void startEpgMenuItem_Click(object sender, EventArgs e)
		{
			RecTimer.GetInstance().StartEpg();
		}

		private void sleepMenuItem_Click(object sender, EventArgs e)
		{
			this.sleepMan.SetSleep();
		}

		private void tunerMonMenuItem_Click(object sender, EventArgs e)
		{
			base.WindowState = FormWindowState.Normal;
			base.Activate();
		}

		private void serviceMenu_Opening(object sender, CancelEventArgs e)
		{
			this.EnableServiceMenu(this.serviceView.FocusedItem != null);
			if (this.serviceView.FocusedItem == null)
			{
				return;
			}
			TunerMon.ServiceInfo arg_3D_0 = this.services[this.serviceView.FocusedItem.Index];
			this.closeTunerMenuItem.Enabled = this.curTuner.IsOpen();
		}

		private void EnableServiceMenu(bool enable)
		{
			IEnumerator enumerator = this.serviceMenu.Items.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					((ToolStripItem)enumerator.Current).Enabled = enable;
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
		}

		private void closeTunerMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				this.curTuner.Close();
			}
			catch (Exception ex)
			{
				Log.Write("TVTestを終了できません。[詳細] " + ex.Message);
				Log.Write(1, ex.StackTrace);
			}
		}

		private void fontChangeMenuItem_Click(object sender, EventArgs e)
		{
			FontDialog fontDialog = new FontDialog();
			fontDialog.Font = this.tunerView.Font;
			if (fontDialog.ShowDialog() != DialogResult.Cancel)
			{
				this.Font = fontDialog.Font;
			}
		}

		private void TunerMon_Resize(object sender, EventArgs e)
		{
			if (base.WindowState == FormWindowState.Minimized)
			{
				base.Opacity = 0.0;
				return;
			}
			base.Opacity = 100.0;
		}

		private void openEpgMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				Process.Start(MainDef.GetInstance()["epgurl"]);
			}
			catch (Exception ex)
			{
				Log.Write("番組表を開けませんでした。[詳細] " + ex.Message);
				Log.Write(1, ex.StackTrace);
			}
		}

		private void setupMenuItem_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("Tvmaidを終了して、セットアップを起動しますか？\nTvmaidを終了したくない場合はキャンセルしてください。", AppData.AppName, MessageBoxButtons.OKCancel) == DialogResult.OK)
			{
				Program.ExitMode = 2;
				base.Close();
			}
		}

		private void startServiceEpgMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				int id = this.services[this.serviceView.FocusedItem.Index].Id;
				Service service = new Service(this.sql, id);
				RecTimer.GetInstance().StartEpg(service);
			}
			catch (Exception ex)
			{
				Log.Write("番組表取得開始に失敗しました。[詳細] " + ex.Message);
				Log.Write(1, ex.StackTrace);
			}
		}

		private void testToolStripMenuItem_Click(object sender, EventArgs e)
		{
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
			ComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof(TunerMon));
			this.serviceView = new ListView();
			this.nameHeader = new ColumnHeader();
			this.timeHeader = new ColumnHeader();
			this.eventHeader = new ColumnHeader();
			this.serviceMenu = new ContextMenuStrip(this.components);
			this.viewMenuItem = new ToolStripMenuItem();
			this.closeTunerMenuItem = new ToolStripMenuItem();
			this.toolStripMenuItem1 = new ToolStripSeparator();
			this.recordAddMenuItem = new ToolStripMenuItem();
			this.recordRemoveMenuItem = new ToolStripMenuItem();
			this.toolStripMenuItem2 = new ToolStripSeparator();
			this.startServiceEpgMenuItem = new ToolStripMenuItem();
			this.toolStripMenuItem4 = new ToolStripSeparator();
			this.fontChangeMenuItem = new ToolStripMenuItem();
			this.split1 = new SplitContainer();
			this.tunerView = new TreeView();
			this.split2 = new SplitContainer();
			this.logBox = new TextBox();
			this.serviceTimer = new Timer(this.components);
			this.tunerTimer = new Timer(this.components);
			this.logTimer = new Timer(this.components);
			this.notifyIcon = new NotifyIcon(this.components);
			this.trayMenu = new ContextMenuStrip(this.components);
			this.exitMenuItem = new ToolStripMenuItem();
			this.updateTunerMenuItem = new ToolStripMenuItem();
			this.setupMenuItem = new ToolStripMenuItem();
			this.toolStripMenuItem3 = new ToolStripSeparator();
			this.stopEpgMenuItem = new ToolStripMenuItem();
			this.startEpgMenuItem = new ToolStripMenuItem();
			this.toolStripSeparator1 = new ToolStripSeparator();
			this.sleepMenuItem = new ToolStripMenuItem();
			this.toolStripSeparator2 = new ToolStripSeparator();
			this.openEpgMenuItem = new ToolStripMenuItem();
			this.tunerMonMenuItem = new ToolStripMenuItem();
			this.testToolStripMenuItem = new ToolStripMenuItem();
			this.test2ToolStripMenuItem = new ToolStripMenuItem();
			this.panel1 = new Panel();
			this.serviceMenu.SuspendLayout();
			((ISupportInitialize)this.split1).BeginInit();
			this.split1.Panel1.SuspendLayout();
			this.split1.Panel2.SuspendLayout();
			this.split1.SuspendLayout();
			((ISupportInitialize)this.split2).BeginInit();
			this.split2.Panel1.SuspendLayout();
			this.split2.Panel2.SuspendLayout();
			this.split2.SuspendLayout();
			this.trayMenu.SuspendLayout();
			this.panel1.SuspendLayout();
			base.SuspendLayout();
			this.serviceView.BorderStyle = BorderStyle.None;
			this.serviceView.Columns.AddRange(new ColumnHeader[]
			{
				this.nameHeader,
				this.timeHeader,
				this.eventHeader
			});
			this.serviceView.ContextMenuStrip = this.serviceMenu;
			this.serviceView.Dock = DockStyle.Fill;
			this.serviceView.FullRowSelect = true;
			this.serviceView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
			this.serviceView.HideSelection = false;
			this.serviceView.Location = new Point(0, 0);
			this.serviceView.Margin = new Padding(3, 4, 3, 4);
			this.serviceView.MultiSelect = false;
			this.serviceView.Name = "serviceView";
			this.serviceView.ShowItemToolTips = true;
			this.serviceView.Size = new Size(589, 178);
			this.serviceView.TabIndex = 0;
			this.serviceView.UseCompatibleStateImageBehavior = false;
			this.serviceView.View = System.Windows.Forms.View.Details;
			this.serviceView.VirtualMode = true;
			this.serviceView.ItemActivate += new EventHandler(this.serviceView_ItemActivate);
			this.serviceView.RetrieveVirtualItem += new RetrieveVirtualItemEventHandler(this.serviceView_RetrieveVirtualItem);
			this.nameHeader.Text = "サービス";
			this.nameHeader.Width = 179;
			this.timeHeader.Text = "時間";
			this.timeHeader.Width = 133;
			this.eventHeader.Text = "番組";
			this.eventHeader.Width = 195;
			this.serviceMenu.ImageScalingSize = new Size(20, 20);
			this.serviceMenu.Items.AddRange(new ToolStripItem[]
			{
				this.viewMenuItem,
				this.closeTunerMenuItem,
				this.toolStripMenuItem1,
				this.recordAddMenuItem,
				this.recordRemoveMenuItem,
				this.toolStripMenuItem2,
				this.startServiceEpgMenuItem,
				this.toolStripMenuItem4,
				this.fontChangeMenuItem
			});
			this.serviceMenu.Name = "contextMenu";
			this.serviceMenu.Size = new Size(250, 166);
			this.serviceMenu.Opening += new CancelEventHandler(this.serviceMenu_Opening);
			this.viewMenuItem.Font = new Font("Meiryo UI", 9f, FontStyle.Bold);
			this.viewMenuItem.Name = "viewMenuItem";
			this.viewMenuItem.Size = new Size(249, 24);
			this.viewMenuItem.Text = "見る(&V)";
			this.viewMenuItem.Click += new EventHandler(this.viewMenuItem_Click);
			this.closeTunerMenuItem.Name = "closeTunerMenuItem";
			this.closeTunerMenuItem.Size = new Size(249, 24);
			this.closeTunerMenuItem.Text = "閉じる(&C)";
			this.closeTunerMenuItem.Click += new EventHandler(this.closeTunerMenuItem_Click);
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new Size(246, 6);
			this.recordAddMenuItem.Name = "recordAddMenuItem";
			this.recordAddMenuItem.Size = new Size(249, 24);
			this.recordAddMenuItem.Text = "現在の番組を録画予約(&R)";
			this.recordAddMenuItem.Click += new EventHandler(this.recordAddMenuItem_Click);
			this.recordRemoveMenuItem.Name = "recordRemoveMenuItem";
			this.recordRemoveMenuItem.Size = new Size(249, 24);
			this.recordRemoveMenuItem.Text = "現在の録画を中断する(&S)";
			this.recordRemoveMenuItem.Click += new EventHandler(this.recordRemoveMenuItem_Click);
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new Size(246, 6);
			this.startServiceEpgMenuItem.Name = "startServiceEpgMenuItem";
			this.startServiceEpgMenuItem.Size = new Size(249, 24);
			this.startServiceEpgMenuItem.Text = "このサービスの番組表取得(&E)";
			this.startServiceEpgMenuItem.Click += new EventHandler(this.startServiceEpgMenuItem_Click);
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new Size(246, 6);
			this.fontChangeMenuItem.Name = "fontChangeMenuItem";
			this.fontChangeMenuItem.Size = new Size(249, 24);
			this.fontChangeMenuItem.Text = "フォントの変更(&F)...";
			this.fontChangeMenuItem.Click += new EventHandler(this.fontChangeMenuItem_Click);
			this.split1.Dock = DockStyle.Fill;
			this.split1.FixedPanel = FixedPanel.Panel1;
			this.split1.Location = new Point(0, 0);
			this.split1.Margin = new Padding(3, 4, 3, 4);
			this.split1.Name = "split1";
			this.split1.Panel1.Controls.Add(this.tunerView);
			this.split1.Panel2.Controls.Add(this.serviceView);
			this.split1.Size = new Size(765, 178);
			this.split1.SplitterDistance = 171;
			this.split1.SplitterWidth = 5;
			this.split1.TabIndex = 9;
			this.tunerView.BorderStyle = BorderStyle.None;
			this.tunerView.Dock = DockStyle.Fill;
			this.tunerView.FullRowSelect = true;
			this.tunerView.HideSelection = false;
			this.tunerView.Location = new Point(0, 0);
			this.tunerView.Margin = new Padding(3, 4, 3, 4);
			this.tunerView.Name = "tunerView";
			this.tunerView.ShowLines = false;
			this.tunerView.ShowPlusMinus = false;
			this.tunerView.ShowRootLines = false;
			this.tunerView.Size = new Size(171, 178);
			this.tunerView.TabIndex = 0;
			this.tunerView.AfterSelect += new TreeViewEventHandler(this.tunerView_AfterSelect);
			this.split2.Dock = DockStyle.Fill;
			this.split2.FixedPanel = FixedPanel.Panel2;
			this.split2.Location = new Point(0, 1);
			this.split2.Margin = new Padding(3, 2, 3, 2);
			this.split2.Name = "split2";
			this.split2.Orientation = Orientation.Horizontal;
			this.split2.Panel1.Controls.Add(this.split1);
			this.split2.Panel2.Controls.Add(this.logBox);
			this.split2.Size = new Size(765, 317);
			this.split2.SplitterDistance = 178;
			this.split2.TabIndex = 11;
			this.logBox.BorderStyle = BorderStyle.None;
			this.logBox.Dock = DockStyle.Fill;
			this.logBox.Location = new Point(0, 0);
			this.logBox.Margin = new Padding(3, 2, 3, 2);
			this.logBox.Multiline = true;
			this.logBox.Name = "logBox";
			this.logBox.ScrollBars = ScrollBars.Vertical;
			this.logBox.Size = new Size(765, 135);
			this.logBox.TabIndex = 0;
			this.serviceTimer.Interval = 5000;
			this.serviceTimer.Tick += new EventHandler(this.serviceTimer_Tick);
			this.tunerTimer.Interval = 2000;
			this.tunerTimer.Tick += new EventHandler(this.tunerTimer_Tick);
			this.logTimer.Enabled = true;
			this.logTimer.Interval = 500;
			this.logTimer.Tick += new EventHandler(this.logTimer_Tick);
			this.notifyIcon.ContextMenuStrip = this.trayMenu;
			this.notifyIcon.Icon = (Icon)componentResourceManager.GetObject("notifyIcon.Icon");
			this.notifyIcon.Text = "Tvmaid";
			this.notifyIcon.Visible = true;
			this.notifyIcon.MouseDoubleClick += new MouseEventHandler(this.notifyIcon_MouseDoubleClick);
			this.trayMenu.ImageScalingSize = new Size(20, 20);
			this.trayMenu.Items.AddRange(new ToolStripItem[]
			{
				this.exitMenuItem,
				this.updateTunerMenuItem,
				this.setupMenuItem,
				this.toolStripMenuItem3,
				this.stopEpgMenuItem,
				this.startEpgMenuItem,
				this.toolStripSeparator1,
				this.sleepMenuItem,
				this.toolStripSeparator2,
				this.openEpgMenuItem,
				this.tunerMonMenuItem,
				this.testToolStripMenuItem,
				this.test2ToolStripMenuItem
			});
			this.trayMenu.Name = "contextMenuStrip";
			this.trayMenu.Size = new Size(221, 310);
			this.exitMenuItem.Name = "exitMenuItem";
			this.exitMenuItem.Size = new Size(220, 26);
			this.exitMenuItem.Text = "終了(&X)";
			this.exitMenuItem.Click += new EventHandler(this.exitMenuItem_Click);
			this.updateTunerMenuItem.Name = "updateTunerMenuItem";
			this.updateTunerMenuItem.Size = new Size(220, 26);
			this.updateTunerMenuItem.Text = "チューナ更新(&T)";
			this.updateTunerMenuItem.Click += new EventHandler(this.updateTunerMenuItem_Click);
			this.setupMenuItem.Name = "setupMenuItem";
			this.setupMenuItem.Size = new Size(220, 26);
			this.setupMenuItem.Text = "設定(&S)...";
			this.setupMenuItem.Click += new EventHandler(this.setupMenuItem_Click);
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new Size(217, 6);
			this.stopEpgMenuItem.Name = "stopEpgMenuItem";
			this.stopEpgMenuItem.Size = new Size(220, 26);
			this.stopEpgMenuItem.Text = "番組表取得を中止(&A)";
			this.stopEpgMenuItem.Click += new EventHandler(this.stopEpgMenuItem_Click);
			this.startEpgMenuItem.Name = "startEpgMenuItem";
			this.startEpgMenuItem.Size = new Size(220, 26);
			this.startEpgMenuItem.Text = "番組表取得(&E)";
			this.startEpgMenuItem.Click += new EventHandler(this.startEpgMenuItem_Click);
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new Size(217, 6);
			this.sleepMenuItem.Name = "sleepMenuItem";
			this.sleepMenuItem.Size = new Size(220, 26);
			this.sleepMenuItem.Text = "スリープ(&S)";
			this.sleepMenuItem.Click += new EventHandler(this.sleepMenuItem_Click);
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new Size(217, 6);
			this.openEpgMenuItem.Name = "openEpgMenuItem";
			this.openEpgMenuItem.Size = new Size(220, 26);
			this.openEpgMenuItem.Text = "番組表を開く(&O)";
			this.openEpgMenuItem.Click += new EventHandler(this.openEpgMenuItem_Click);
			this.tunerMonMenuItem.Font = new Font("Meiryo UI", 9f, FontStyle.Bold);
			this.tunerMonMenuItem.Name = "tunerMonMenuItem";
			this.tunerMonMenuItem.Size = new Size(220, 26);
			this.tunerMonMenuItem.Text = "チューナモニタ(&M)";
			this.tunerMonMenuItem.Click += new EventHandler(this.tunerMonMenuItem_Click);
			this.testToolStripMenuItem.Name = "testToolStripMenuItem";
			this.testToolStripMenuItem.Size = new Size(220, 26);
			this.testToolStripMenuItem.Text = "test";
			this.testToolStripMenuItem.Visible = false;
			this.testToolStripMenuItem.Click += new EventHandler(this.testToolStripMenuItem_Click);
			this.test2ToolStripMenuItem.Name = "test2ToolStripMenuItem";
			this.test2ToolStripMenuItem.Size = new Size(220, 26);
			this.test2ToolStripMenuItem.Text = "test2";
			this.test2ToolStripMenuItem.Visible = false;
			this.panel1.Controls.Add(this.split2);
			this.panel1.Dock = DockStyle.Fill;
			this.panel1.Location = new Point(0, 0);
			this.panel1.Margin = new Padding(3, 2, 3, 2);
			this.panel1.Name = "panel1";
			this.panel1.Padding = new Padding(0, 1, 0, 0);
			this.panel1.Size = new Size(765, 318);
			this.panel1.TabIndex = 12;
			base.AutoScaleDimensions = new SizeF(9f, 19f);
			base.AutoScaleMode = AutoScaleMode.Font;
			base.ClientSize = new Size(765, 318);
			base.Controls.Add(this.panel1);
			this.Font = new Font("Meiryo UI", 9f, FontStyle.Regular, GraphicsUnit.Point, 128);
			base.Icon = (Icon)componentResourceManager.GetObject("$this.Icon");
			base.Margin = new Padding(4, 5, 4, 5);
			base.Name = "TunerMon";
			base.ShowInTaskbar = false;
			base.StartPosition = FormStartPosition.Manual;
			this.Text = "Tvmaid チューナモニタ";
			base.FormClosing += new FormClosingEventHandler(this.TunerMon_FormClosing);
			base.FormClosed += new FormClosedEventHandler(this.TunerMon_FormClosed);
			base.VisibleChanged += new EventHandler(this.TunerMon_VisibleChanged);
			base.Resize += new EventHandler(this.TunerMon_Resize);
			this.serviceMenu.ResumeLayout(false);
			this.split1.Panel1.ResumeLayout(false);
			this.split1.Panel2.ResumeLayout(false);
			((ISupportInitialize)this.split1).EndInit();
			this.split1.ResumeLayout(false);
			this.split2.Panel1.ResumeLayout(false);
			this.split2.Panel2.ResumeLayout(false);
			this.split2.Panel2.PerformLayout();
			((ISupportInitialize)this.split2).EndInit();
			this.split2.ResumeLayout(false);
			this.trayMenu.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			base.ResumeLayout(false);
		}
	}
}
