using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CommunityEventsApp.Models;

namespace CommunityEventsApp
{
    public partial class CalendarForm : Form
    {
        private List<EventItem> events;

        private Panel dragBox = null;
        private int dragOffsetY;

        private const int HourHeight = 40;

        public CalendarForm(List<EventItem> evs)
        {
            InitializeComponent();
            events = evs;
            RenderDay(DateTime.Today);
        }

        private void monthCalendar1_DateChanged(object sender, DateRangeEventArgs e)
        {
            RenderDay(e.Start);
        }

        // ================= DRAW DAY =================
        private void RenderDay(DateTime day)
        {
            panelTimeline.Controls.Clear();

            var dayEvents = events
                .Where(x => x.StartTime.Date == day.Date)
                .OrderBy(x => x.StartTime)
                .ToList();

            // hour labels
            for (int h = 0; h < 24; h++)
            {
                Label lbl = new Label();
                lbl.Text = h.ToString("00") + ":00";
                lbl.Top = h * HourHeight;
                lbl.Left = 5;
                lbl.Width = 50;
                panelTimeline.Controls.Add(lbl);
            }

            DrawEventsWithOverlap(dayEvents);
        }

        // ================= OVERLAP LAYOUT =================
        private void DrawEventsWithOverlap(List<EventItem> list)
        {
            var columns = new List<List<EventItem>>();

            foreach (var ev in list)
            {
                bool placed = false;

                foreach (var col in columns)
                {
                    if (!col.Any(e => Overlap(e, ev)))
                    {
                        col.Add(ev);
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                    columns.Add(new List<EventItem> { ev });
            }

            int totalCols = columns.Count;

            for (int c = 0; c < totalCols; c++)
            {
                foreach (var ev in columns[c])
                {
                    Panel box = CreateEventBlock(ev, c, totalCols);
                    panelTimeline.Controls.Add(box);
                }
            }
        }

        private bool Overlap(EventItem a, EventItem b)
        {
            var aEnd = a.EndTime ?? a.StartTime.AddHours(1);
            var bEnd = b.EndTime ?? b.StartTime.AddHours(1);

            return a.StartTime < bEnd && b.StartTime < aEnd;
        }

        // ================= EVENT BLOCK =================
        private Panel CreateEventBlock(EventItem ev, int column, int totalColumns)
        {
            int width = 220 / totalColumns;

            Panel box = new Panel();
            box.Left = 60 + column * width;
            box.Top = ev.StartTime.Hour * HourHeight;
            box.Width = width - 5;
            box.Height = 35;
            box.Tag = ev;
            box.BackColor = GetCategoryColor(ev.Category);

            Label txt = new Label();
            txt.Text = ev.StartTime.ToString("HH:mm") + " " + ev.Title;
            txt.Dock = DockStyle.Fill;

            box.Controls.Add(txt);

            // drag handlers
            box.MouseDown += DragStart;
            box.MouseMove += DragMove;
            box.MouseUp += DragEnd;

            return box;
        }

        // ================= CATEGORY COLOR =================
        private System.Drawing.Color GetCategoryColor(string cat)
        {
            if (cat == null) return System.Drawing.Color.MediumPurple;

            string c = cat.ToLower();

            if (c.Contains("music")) return System.Drawing.Color.SkyBlue;
            if (c.Contains("sport")) return System.Drawing.Color.LightGreen;
            if (c.Contains("academic")) return System.Drawing.Color.Orange;
            if (c.Contains("career")) return System.Drawing.Color.Gold;

            return System.Drawing.Color.MediumPurple;
        }

        // ================= DRAG START =================
        private void DragStart(object sender, MouseEventArgs e)
        {
            dragBox = sender as Panel;
            dragOffsetY = e.Y;
            dragBox.BringToFront();
        }

        // ================= DRAG MOVE =================
        private void DragMove(object sender, MouseEventArgs e)
        {
            if (dragBox == null) return;

            int newY = dragBox.Top + e.Y - dragOffsetY;

            if (newY < 0) newY = 0;
            if (newY > 24 * HourHeight) newY = 24 * HourHeight;

            dragBox.Top = newY;
        }

        // ================= DRAG END =================
        private void DragEnd(object sender, MouseEventArgs e)
        {
            if (dragBox == null) return;

            var ev = dragBox.Tag as EventItem;
            if (ev == null) return;

            int hour = dragBox.Top / HourHeight;

            ev.StartTime = new DateTime(
                ev.StartTime.Year,
                ev.StartTime.Month,
                ev.StartTime.Day,
                hour,
                0,
                0);

            var lbl = dragBox.Controls[0] as Label;
            lbl.Text = ev.StartTime.ToString("HH:mm") + " " + ev.Title;

            dragBox = null;
        }
    }
}