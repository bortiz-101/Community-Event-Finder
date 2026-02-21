using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CommunityEventsApp.Data;

namespace CommunityEventsApp
{
    public partial class FormAddEvent : Form
    {
        private readonly EventRepository repo = new EventRepository();

        TextBox titleBox = new TextBox();
        TextBox categoryBox = new TextBox();
        DateTimePicker startPicker = new DateTimePicker();
        DateTimePicker endPicker = new DateTimePicker();
        TextBox venueBox = new TextBox();
        TextBox addressBox = new TextBox();
        TextBox cityBox = new TextBox();
        TextBox stateBox = new TextBox();
        TextBox zipBox = new TextBox();
        TextBox descBox = new TextBox();
        TextBox urlBox = new TextBox();

        Button btnSave = new Button();
        Button btnCancel = new Button();

        public FormAddEvent()
        {
            InitializeComponent();
            BuildLayout();
            WireUpLogic();
        }

        // ================= UI =================
        private void BuildLayout()
        {
            Controls.Clear();

            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 2;
            panel.RowCount = 0;
            panel.Padding = new Padding(12);
            panel.AutoSize = true;

            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            int row = 0;

            void AddRow(string label, Control c)
            {
                panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                panel.Controls.Add(new Label
                {
                    Text = label,
                    AutoSize = true,
                    Anchor = AnchorStyles.Right
                }, 0, row);

                c.Dock = DockStyle.Fill;
                panel.Controls.Add(c, 1, row);

                row++;
            }

            descBox.Multiline = true;
            descBox.Height = 90;

            AddRow("Title*", titleBox);
            AddRow("Category*", categoryBox);
            AddRow("Start*", startPicker);
            AddRow("End*", endPicker);
            AddRow("Venue*", venueBox);
            AddRow("Address*", addressBox);
            AddRow("City*", cityBox);
            AddRow("State*", stateBox);
            AddRow("Zip*", zipBox);
            AddRow("URL", urlBox);
            AddRow("Description*", descBox);
            

            // ===== buttons =====
            var btnPanel = new FlowLayoutPanel();
            btnPanel.FlowDirection = FlowDirection.RightToLeft;
            btnPanel.Dock = DockStyle.Bottom;
            btnPanel.Height = 48;
            btnPanel.Padding = new Padding(0, 10, 0, 0);

            btnSave.Text = "Save";
            btnSave.Width = 90;

            btnCancel.Text = "Cancel";
            btnCancel.Width = 90;

            btnPanel.Controls.Add(btnSave);
            btnPanel.Controls.Add(btnCancel);

            Controls.Add(panel);
            Controls.Add(btnPanel);

            Text = "Add Event";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new System.Drawing.Size(460, 620);
        }

        // ================= logic =================
        private void WireUpLogic()
        {
            btnSave.Click += SaveBtn_Click;
            btnCancel.Click += (s, e) => Close();

            startPicker.Format = DateTimePickerFormat.Custom;
            startPicker.CustomFormat = "yyyy-MM-dd HH:mm";
            startPicker.ShowUpDown = true;

            endPicker.Format = DateTimePickerFormat.Custom;
            endPicker.CustomFormat = "yyyy-MM-dd HH:mm";
            endPicker.ShowUpDown = true;

            startPicker.Value = DateTime.Now.AddDays(1).Date.AddHours(18);
            endPicker.Value = startPicker.Value.AddHours(2);

            cityBox.Text = "Chicago";
            stateBox.Text = "IL";
        }

        // ================= save =================
        private void SaveBtn_Click(object sender, EventArgs e)
        {
            if (!Require(titleBox, "Title")) return;
            if (!Require(categoryBox, "Category")) return;
            if (!Require(venueBox, "Venue")) return;
            if (!Require(addressBox, "Address")) return;
            if (!Require(cityBox, "City")) return;
            if (!Require(stateBox, "State")) return;
            if (!Require(zipBox, "Zip")) return;
            if (!Require(descBox, "Description")) return;

            var url = urlBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(url))
            {
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    MessageBox.Show("Invalid URL format.");
                    urlBox.Focus();
                    return;
                }
            }

            var zip = zipBox.Text.Trim();
            if (!Regex.IsMatch(zip, @"^\d{5}(-\d{4})?$"))
            {
                MessageBox.Show("Zip must be 5 digits.");
                return;
            }

            var st = stateBox.Text.Trim();
            if (!Regex.IsMatch(st, @"^[A-Za-z]{2}$"))
            {
                MessageBox.Show("State must be 2 letters.");
                return;
            }
            stateBox.Text = st.ToUpperInvariant();

            if (endPicker.Value <= startPicker.Value)
            {
                MessageBox.Show("End time must be later than Start time.");
                return;
            }

            repo.InsertEvent(
                titleBox.Text.Trim(),
                categoryBox.Text.Trim(),
                startPicker.Value,
                endPicker.Value,
                venueBox.Text.Trim(),
                addressBox.Text.Trim(),
                cityBox.Text.Trim(),
                stateBox.Text.Trim(),
                zipBox.Text.Trim(),
                descBox.Text.Trim(),
                urlBox.Text.Trim()
            );

            DialogResult = DialogResult.OK;
            Close();
        }

        private bool Require(Control c, string label)
        {
            if (string.IsNullOrWhiteSpace(c.Text))
            {
                MessageBox.Show(label + " is required.");
                c.Focus();
                return false;
            }
            return true;
        }
    }
}