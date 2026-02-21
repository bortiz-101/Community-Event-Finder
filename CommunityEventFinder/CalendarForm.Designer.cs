namespace CommunityEventsApp
{
    partial class CalendarForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MonthCalendar monthCalendar1;
        private System.Windows.Forms.Panel panelTimeline;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.monthCalendar1 = new System.Windows.Forms.MonthCalendar();
            this.panelTimeline = new System.Windows.Forms.Panel();
            this.SuspendLayout();

            this.monthCalendar1.Location = new System.Drawing.Point(10, 10);
            this.monthCalendar1.DateChanged +=
                new System.Windows.Forms.DateRangeEventHandler(this.monthCalendar1_DateChanged);

            this.panelTimeline.Location = new System.Drawing.Point(250, 10);
            this.panelTimeline.Size = new System.Drawing.Size(400, 550);
            this.panelTimeline.AutoScroll = true;

            this.ClientSize = new System.Drawing.Size(700, 600);
            this.Controls.Add(this.monthCalendar1);
            this.Controls.Add(this.panelTimeline);
            this.Text = "Calendar";

            this.ResumeLayout(false);
        }
    }
}