using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Tvmaid
{
	public class ExitForm : Form
	{
		private IContainer components;

		private Label label1;

		private ProgressBar progressBar;

		private Timer timer;

		public ExitForm(int max)
		{
			this.InitializeComponent();
			this.progressBar.Maximum = max;
		}

		private void timer_Tick(object sender, EventArgs e)
		{
			ProgressBar expr_06 = this.progressBar;
			int value = expr_06.Value;
			expr_06.Value = value + 1;
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
			this.label1 = new Label();
			this.progressBar = new ProgressBar();
			this.timer = new Timer(this.components);
			base.SuspendLayout();
			this.label1.AutoSize = true;
			this.label1.Location = new Point(27, 25);
			this.label1.Name = "label1";
			this.label1.Size = new Size(113, 19);
			this.label1.TabIndex = 0;
			this.label1.Text = "終了しています...";
			this.progressBar.Location = new Point(31, 65);
			this.progressBar.Margin = new Padding(3, 4, 3, 4);
			this.progressBar.Name = "progressBar";
			this.progressBar.Size = new Size(402, 12);
			this.progressBar.TabIndex = 1;
			this.timer.Enabled = true;
			this.timer.Interval = 1000;
			this.timer.Tick += new EventHandler(this.timer_Tick);
			base.AutoScaleDimensions = new SizeF(9f, 19f);
			base.AutoScaleMode = AutoScaleMode.Font;
			base.ClientSize = new Size(464, 109);
			base.ControlBox = false;
			base.Controls.Add(this.progressBar);
			base.Controls.Add(this.label1);
			this.Font = new Font("Meiryo UI", 9f, FontStyle.Regular, GraphicsUnit.Point, 128);
			base.FormBorderStyle = FormBorderStyle.FixedDialog;
			base.Margin = new Padding(3, 4, 3, 4);
			base.Name = "ExitForm";
			base.ShowInTaskbar = false;
			base.StartPosition = FormStartPosition.CenterScreen;
			this.Text = "Tvmaid";
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}
}
