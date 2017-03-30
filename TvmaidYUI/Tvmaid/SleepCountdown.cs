using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Tvmaid
{
	public class SleepCountdown : Form
	{
		private int count = 60;

		private IContainer components;

		private Button cancelButton;

		private Label countLabel;

		private Timer timer;

		private Button sleepButton;

		private ProgressBar progressBar;

		private Label wakeTimeLable;

		public SleepCountdown(DateTime wakeTime)
		{
			this.InitializeComponent();
			this.wakeTimeLable.Text = "復帰予定 " + wakeTime.ToString("MM/dd HH:mm");
		}

		private void timer_Tick(object sender, EventArgs e)
		{
			this.countLabel.Text = "スリープまで {0} 秒".Formatex(new object[]
			{
				this.count
			});
			this.progressBar.Value = this.progressBar.Maximum - this.count;
			if (this.count == 0)
			{
				base.DialogResult = DialogResult.OK;
				this.timer.Stop();
				return;
			}
			this.count--;
		}

		private void sleepButton_Click(object sender, EventArgs e)
		{
			base.DialogResult = DialogResult.OK;
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
			this.cancelButton = new Button();
			this.countLabel = new Label();
			this.timer = new Timer(this.components);
			this.sleepButton = new Button();
			this.progressBar = new ProgressBar();
			this.wakeTimeLable = new Label();
			base.SuspendLayout();
			this.cancelButton.DialogResult = DialogResult.Cancel;
			this.cancelButton.Location = new Point(429, 92);
			this.cancelButton.Margin = new Padding(3, 2, 3, 2);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new Size(99, 31);
			this.cancelButton.TabIndex = 4;
			this.cancelButton.Text = "キャンセル";
			this.cancelButton.UseVisualStyleBackColor = true;
			this.countLabel.AutoSize = true;
			this.countLabel.Location = new Point(27, 23);
			this.countLabel.Name = "countLabel";
			this.countLabel.Size = new Size(86, 19);
			this.countLabel.TabIndex = 0;
			this.countLabel.Text = "カウントダウン";
			this.timer.Enabled = true;
			this.timer.Interval = 1000;
			this.timer.Tick += new EventHandler(this.timer_Tick);
			this.sleepButton.Location = new Point(324, 92);
			this.sleepButton.Margin = new Padding(3, 2, 3, 2);
			this.sleepButton.Name = "sleepButton";
			this.sleepButton.Size = new Size(99, 31);
			this.sleepButton.TabIndex = 3;
			this.sleepButton.Text = "スリープ";
			this.sleepButton.UseVisualStyleBackColor = true;
			this.sleepButton.Click += new EventHandler(this.sleepButton_Click);
			this.progressBar.Location = new Point(31, 64);
			this.progressBar.Margin = new Padding(3, 2, 3, 2);
			this.progressBar.Maximum = 60;
			this.progressBar.Name = "progressBar";
			this.progressBar.Size = new Size(497, 10);
			this.progressBar.TabIndex = 1;
			this.wakeTimeLable.AutoSize = true;
			this.wakeTimeLable.Location = new Point(27, 98);
			this.wakeTimeLable.Name = "wakeTimeLable";
			this.wakeTimeLable.Size = new Size(69, 19);
			this.wakeTimeLable.TabIndex = 2;
			this.wakeTimeLable.Text = "復帰予定";
			base.AutoScaleDimensions = new SizeF(9f, 19f);
			base.AutoScaleMode = AutoScaleMode.Font;
			base.CancelButton = this.cancelButton;
			base.ClientSize = new Size(558, 142);
			base.ControlBox = false;
			base.Controls.Add(this.wakeTimeLable);
			base.Controls.Add(this.progressBar);
			base.Controls.Add(this.sleepButton);
			base.Controls.Add(this.countLabel);
			base.Controls.Add(this.cancelButton);
			this.Font = new Font("Meiryo UI", 9f, FontStyle.Regular, GraphicsUnit.Point, 128);
			base.FormBorderStyle = FormBorderStyle.FixedSingle;
			base.Margin = new Padding(3, 4, 3, 4);
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Name = "SleepCountdown";
			base.ShowInTaskbar = false;
			base.StartPosition = FormStartPosition.CenterScreen;
			this.Text = "Tvmaid";
			base.TopMost = true;
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}
}
