using System;
using System.Windows.Forms;
using CommunityEventsApp.Data;
using CommunityEventsApp.Models;

namespace CommunityEventsApp
{
    public partial class FormList : Form
    {
        // Repository used to load and update events
        private readonly EventRepository _repo = new EventRepository();

        // UI controls
        private SplitContainer split;
        private TextBox detailBox;
        private Button saveButton;
        private CheckBox favoritesOnlyCheckBox;

        // Currently selected event
        private EventItem currentEvent;

        public FormList()
        {
            InitializeComponent();
            BuildLayout(); // Dynamically build UI layout
        }

        private void BuildLayout()
        {
            // Main split container: left = grid, right = details panel
            split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.SplitterDistance = 650;

            // Remove grid from designer and reinsert into split container
            this.Controls.Remove(dataGridView1);

            // Configure grid appearance and behavior
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.ReadOnly = true;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            split.Panel1.Controls.Add(dataGridView1);

            // Right-side container panel
            var rightPanel = new Panel();
            rightPanel.Dock = DockStyle.Fill;

            // Checkbox to filter only favorite events
            favoritesOnlyCheckBox = new CheckBox();
            favoritesOnlyCheckBox.Text = "Show favorites only";
            favoritesOnlyCheckBox.Dock = DockStyle.Top;
            favoritesOnlyCheckBox.Height = 28;
            favoritesOnlyCheckBox.CheckedChanged += FavoritesOnlyCheckBox_CheckedChanged;

            // Text area displaying selected event details
            detailBox = new TextBox();
            detailBox.Multiline = true;
            detailBox.Dock = DockStyle.Fill;
            detailBox.ReadOnly = true;
            detailBox.ScrollBars = ScrollBars.Vertical;
            detailBox.Font = new System.Drawing.Font("Segoe UI", 10);

            // Button for saving/removing favorites
            saveButton = new Button();
            saveButton.Text = "⭐ Save Event";
            saveButton.Dock = DockStyle.Bottom;
            saveButton.Height = 40;
            saveButton.Click += SaveButton_Click;

            // Add controls to right panel
            rightPanel.Controls.Add(detailBox);
            rightPanel.Controls.Add(saveButton);
            rightPanel.Controls.Add(favoritesOnlyCheckBox);

            split.Panel2.Controls.Add(rightPanel);

            // Add split container to form
            this.Controls.Add(split);

            // When grid selection changes, update detail panel
            dataGridView1.SelectionChanged += Grid_SelectionChanged;
        }

        // Form load event → populate grid
        private void FormList_Load(object sender, EventArgs e)
        {
            ReloadGrid();
        }

        // Loads events into the grid depending on filter state
        private void ReloadGrid()
        {
            // Choose dataset based on favorites checkbox
            var events = favoritesOnlyCheckBox != null && favoritesOnlyCheckBox.Checked
                ? _repo.GetFavoriteEventsForCurrentMonth()
                : _repo.GetEventsForCurrentMonth();

            // Bind data to grid
            dataGridView1.AutoGenerateColumns = true;
            dataGridView1.DataSource = events;

            // Auto-select first row if available
            if (dataGridView1.Rows.Count > 0)
            {
                dataGridView1.Rows[0].Selected = true;
                dataGridView1.CurrentCell = dataGridView1.Rows[0].Cells[0];
            }
            else
            {
                // No events → clear state
                currentEvent = null;
                detailBox.Text = "";
                saveButton.Text = "⭐ Save Event";
            }

            // Update window title with event count
            this.Text = favoritesOnlyCheckBox != null && favoritesOnlyCheckBox.Checked
                ? $"Favorites (this month): {events.Count}"
                : $"Events loaded: {events.Count}";
        }

        // Trigger reload when checkbox toggled
        private void FavoritesOnlyCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ReloadGrid();
        }

        // When a row is selected → show event details
        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            // Ensure selected row contains an EventItem
            if (dataGridView1.CurrentRow?.DataBoundItem is EventItem ev)
            {
                currentEvent = ev;

                // Populate detail panel text
                detailBox.Text =
$@"{ev.Title}

📅 When
{ev.StartTime:g}

📍 Where
{ev.VenueName}
{ev.Address}

🏷 Category
{ev.Category}

📝 Description
{ev.Description}

🔗 Link
{ev.Url}";

                // Update button label depending on favorite state
                saveButton.Text = _repo.IsFavorite(ev.EventId)
                    ? "★ Saved"
                    : "⭐ Save Event";
            }
        }

        // Save/remove favorite button click
        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (currentEvent == null) return;

            // Toggle favorite state in repository
            _repo.ToggleFavorite(currentEvent.EventId);

            // Update button text
            saveButton.Text = _repo.IsFavorite(currentEvent.EventId)
                ? "★ Saved"
                : "⭐ Save Event";

            // If currently filtering favorites, reload grid to reflect change
            if (favoritesOnlyCheckBox.Checked)
                ReloadGrid();
        }
    }
}