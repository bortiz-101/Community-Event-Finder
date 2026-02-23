using CommunityEventsApp.Data;
using CommunityEventsApp.Models;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CommunityEventsApp
{
    public partial class FormList : Form
    {
        private readonly EventRepository _repo = new EventRepository();

        private SplitContainer mainSplit;
        private SplitContainer split;

        private TextBox detailBox;
        private Button saveButton;
        private Button exportIcsButton;
        private CheckBox favoritesOnlyCheckBox;

        private TextBox searchBox;
        private Label searchLabel;

        private ComboBox radiusCombo;
        private CheckBox useDefaultCenterCheckBox;
        private TextBox centerAddressBox;
        private Button applyDistanceButton;
        private Label defaultCenterLabel;

        private GMapControl map;
        private GMapOverlay markerOverlay;
        private GMapOverlay radiusOverlay;

        private double _radiusMiles = 0;
        private const double EarthRadiusMiles = 3958.8;

        private const double DefaultCenterLat = 41.9975;
        private const double DefaultCenterLon = -87.6586;
        private const string DefaultCenterName = "LUC Lake Shore Campus";
        private const string DefaultAddress = "1032 W Sheridan Rd, Chicago, IL";

        private double _centerLat = DefaultCenterLat;
        private double _centerLon = DefaultCenterLon;

        private EventItem currentEvent;
        private List<EventItem> _lastGridEvents = new List<EventItem>();

        private Dictionary<string, (decimal lat, decimal lon)> geoCache
            = new Dictionary<string, (decimal, decimal)>();

        public FormList()
        {
            InitializeComponent();
            BuildLayout();
        }

        // ================= UI =================
        private void BuildLayout()
        {
            mainSplit = new SplitContainer { Dock = DockStyle.Fill };
            Controls.Add(mainSplit);

            // ===== MAP =====
            map = new GMapControl { Dock = DockStyle.Fill };

            GMaps.Instance.Mode = AccessMode.ServerAndCache;

            map.MapProvider = GMapProviders.BingMap;

            map.MinZoom = 2;
            map.MaxZoom = 18;
            map.Zoom = 12;
            map.Position = new PointLatLng(DefaultCenterLat, DefaultCenterLon);

            markerOverlay = new GMapOverlay("events");
            radiusOverlay = new GMapOverlay("radius");

            map.Overlays.Add(markerOverlay);
            map.Overlays.Add(radiusOverlay);

            map.OnMarkerClick += Map_OnMarkerClick;
            map.OnMapZoomChanged += delegate { RedrawMap(); };
            map.OnPositionChanged += delegate { RedrawMap(); };

            mainSplit.Panel1.Controls.Add(map);

            // ===== RIGHT PANEL =====
            split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
            mainSplit.Panel2.Controls.Add(split);

            Controls.Remove(dataGridView1);
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.ReadOnly = true;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            split.Panel1.Controls.Add(dataGridView1);

            var rightPanel = new Panel { Dock = DockStyle.Fill };

            // SEARCH
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 34 };

            searchLabel = new Label { Text = "Search:", Dock = DockStyle.Left };
            searchBox = new TextBox { Dock = DockStyle.Fill };
            searchBox.TextChanged += delegate { ReloadGrid(); };

            topPanel.Controls.Add(searchBox);
            topPanel.Controls.Add(searchLabel);

            // DISTANCE PANEL
            var distancePanel = new Panel { Dock = DockStyle.Top, Height = 120 };

            defaultCenterLabel = new Label
            {
                Text = "Default center: " + DefaultCenterName,
                Dock = DockStyle.Top
            };

            var row1 = new Panel { Dock = DockStyle.Top, Height = 28 };
            var row2 = new Panel { Dock = DockStyle.Top, Height = 28 };

            var radiusLabel = new Label { Text = "Radius:", Dock = DockStyle.Left };

            radiusCombo = new ComboBox { Dock = DockStyle.Left, Width = 110 };
            radiusCombo.Items.AddRange(new object[] { "All", "5 miles", "10 miles", "20 miles" });
            radiusCombo.SelectedIndex = 0;

            useDefaultCenterCheckBox = new CheckBox
            {
                Text = "Use default center",
                Dock = DockStyle.Fill,
                Checked = true
            };

            applyDistanceButton = new Button { Text = "Apply", Dock = DockStyle.Right };
            applyDistanceButton.Click += ApplyDistanceButton_Click;

            row1.Controls.Add(applyDistanceButton);
            row1.Controls.Add(useDefaultCenterCheckBox);
            row1.Controls.Add(radiusCombo);
            row1.Controls.Add(radiusLabel);

            var centerLabel = new Label { Text = "Center address:", Dock = DockStyle.Left };

            centerAddressBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = DefaultAddress
            };

            row2.Controls.Add(centerAddressBox);
            row2.Controls.Add(centerLabel);

            var formatHint = new Label
            {
                Text = "Format: Street, City, State",
                Dock = DockStyle.Top,
                ForeColor = System.Drawing.Color.Gray
            };

            distancePanel.Controls.Add(row2);
            distancePanel.Controls.Add(formatHint);
            distancePanel.Controls.Add(row1);
            distancePanel.Controls.Add(defaultCenterLabel);

            // FAVORITES
            favoritesOnlyCheckBox = new CheckBox
            {
                Text = "Show saved events only",
                Dock = DockStyle.Top,
                Height = 28,
                Margin = new Padding(0, 8, 0, 8),
                AutoSize = false
            };
            favoritesOnlyCheckBox.CheckedChanged += delegate { ReloadGrid(); };

            // BUTTONS
            saveButton = new Button { Text = "⭐ Save Event", Dock = DockStyle.Bottom, Height = 40 };
            saveButton.Click += SaveButton_Click;

            exportIcsButton = new Button { Text = "📅 Export Favorites (.ics)", Dock = DockStyle.Bottom, Height = 40 };
            exportIcsButton.Click += ExportIcsButton_Click;

            var addBtn = new Button { Text = "+ Add Event", Dock = DockStyle.Top, Height = 32 };
            addBtn.Click += AddBtn_Click;

            var calendarBtn = new Button { Text = "📅 Calendar View for saved events", Dock = DockStyle.Top, Height = 32 };
            calendarBtn.Click += CalendarBtn_Click;

            detailBox = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            rightPanel.Controls.Add(detailBox);
            rightPanel.Controls.Add(saveButton);
            rightPanel.Controls.Add(exportIcsButton);
            rightPanel.Controls.Add(new Panel { Height = 6, Dock = DockStyle.Top });
            rightPanel.Controls.Add(favoritesOnlyCheckBox);
            rightPanel.Controls.Add(distancePanel);
            rightPanel.Controls.Add(topPanel);
            rightPanel.Controls.Add(addBtn);
            rightPanel.Controls.Add(calendarBtn);

            split.Panel2.Controls.Add(rightPanel);

            dataGridView1.SelectionChanged += Grid_SelectionChanged;
        }

        // ================= APPLY ADDRESS =================
        private void ApplyDistanceButton_Click(object sender, EventArgs e)
        {
            var addr = centerAddressBox.Text.Trim();

            if (!useDefaultCenterCheckBox.Checked)
            {
                if (!Regex.IsMatch(addr, @"^.+,\s*.+,\s*[A-Za-z]{2,}$"))
                {
                    MessageBox.Show("Invalid address format.");
                    return;
                }

                var center = TryGeocodeCenter(addr);
                if (center.lat == null)
                {
                    MessageBox.Show("Address not found.");
                    return;
                }

                _centerLat = (double)center.lat.Value;
                _centerLon = (double)center.lon.Value;
            }
            else
            {
                _centerLat = DefaultCenterLat;
                _centerLon = DefaultCenterLon;
            }

            _radiusMiles = 0;
            if (radiusCombo.SelectedIndex > 0)
            {
                _radiusMiles = double.Parse(radiusCombo.SelectedItem.ToString().Split(' ')[0]);
            }

            map.Position = new PointLatLng(_centerLat, _centerLon);
            ReloadGrid();
        }

        // ================= GEOCODE =================
        private (decimal? lat, decimal? lon) TryGeocodeCenter(string query)
        {
            if (geoCache.ContainsKey(query))
                return geoCache[query];

            try
            {
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "CommunityEventsApp");

                    var json = wc.DownloadString(
                        "https://nominatim.openstreetmap.org/search?format=json&limit=1&countrycodes=us&addressdetails=1&q="
                        + Uri.EscapeDataString(query));

                    var arr = JArray.Parse(json);
                    if (arr.Count == 0)
                        return (null, null);

                    var obj = arr[0];

                    decimal lat = decimal.Parse(obj["lat"].ToString(),
                        System.Globalization.CultureInfo.InvariantCulture);

                    decimal lon = decimal.Parse(obj["lon"].ToString(),
                        System.Globalization.CultureInfo.InvariantCulture);

                    geoCache[query] = (lat, lon);
                    return (lat, lon);
                }
            }
            catch
            {
                return (null, null);
            }
        }

        // ================= MAP =================
        private void ReloadGrid()
        {
            var events = favoritesOnlyCheckBox.Checked
                ? _repo.GetFavoriteEventsForCurrentMonth()
                : _repo.GetEventsForCurrentMonth();

            if (_radiusMiles > 0)
            {
                events = events.Where(ev =>
                {
                    if (!ev.Latitude.HasValue) return false;
                    return DistanceMiles(
                        _centerLat, _centerLon,
                        ev.Latitude.Value, ev.Longitude.Value)
                        <= _radiusMiles;
                }).ToList();
            }

            _lastGridEvents = events;
            dataGridView1.DataSource = events;
            RedrawMap();
        }

        private void RedrawMap()
        {
            markerOverlay.Markers.Clear();
            radiusOverlay.Polygons.Clear();

            DrawRadiusCircle();

            int clusterPx = 50; // 聚类半径像素

            var clusters = new List<List<EventItem>>();

            foreach (var ev in _lastGridEvents.Where(e => e.Latitude.HasValue))
            {
                var p = map.FromLatLngToLocal(
                    new PointLatLng(ev.Latitude.Value, ev.Longitude.Value));

                bool added = false;

                foreach (var c in clusters)
                {
                    var first = c[0];
                    var p2 = map.FromLatLngToLocal(
                        new PointLatLng(first.Latitude.Value, first.Longitude.Value));

                    double dist = Math.Sqrt(Math.Pow(p.X - p2.X, 2) + Math.Pow(p.Y - p2.Y, 2));

                    if (dist < clusterPx)
                    {
                        c.Add(ev);
                        added = true;
                        break;
                    }
                }

                if (!added)
                    clusters.Add(new List<EventItem> { ev });
            }

            foreach (var cluster in clusters)
            {
                var ev = cluster[0];

                var marker = new GMarkerGoogle(
                    new PointLatLng(ev.Latitude.Value, ev.Longitude.Value),
                    cluster.Count == 1
                        ? GMarkerGoogleType.red_small
                        : GMarkerGoogleType.blue);

                marker.Tag = cluster.Count == 1
                    ? ev
                    : (object)cluster;

                marker.ToolTipText = cluster.Count == 1
                    ? ev.Title
                    : $"{cluster.Count} events";

                markerOverlay.Markers.Add(marker);
            }

            map.Refresh();
        }

        private void Map_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            if (item.Tag is EventItem ev)
            {
                currentEvent = ev;
                HighlightGridRow(ev);
                ShowDetail(ev);
            }
            else if (item.Tag is List<EventItem> list)
            {
                MessageBox.Show(string.Join("\n\n", list.Select(x => x.Title)), "Cluster");
            }
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow?.DataBoundItem is EventItem ev)
            {
                currentEvent = ev;

                if (!ev.Latitude.HasValue || !ev.Longitude.HasValue)
                {
                    var full = $"{ev.Address}, {ev.City}, {ev.State}, {ev.Zip}";
                    var geo = TryGeocodeCenter(full);

                    if (geo.lat == null)
                    {
                        MessageBox.Show(
                            "This event cannot be located on the map.",
                            "Location Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    ev.Latitude = (double)geo.lat.Value;
                    ev.Longitude = (double)geo.lon.Value;
                }

                map.Position = new PointLatLng(ev.Latitude.Value, ev.Longitude.Value);
                map.Zoom = 14;

                ShowDetail(ev);
            }
        }

        // ================= FAVORITES =================
        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (currentEvent == null) return;
            _repo.ToggleFavorite(currentEvent.EventId);
            ReloadGrid();
        }

        private void AddBtn_Click(object sender, EventArgs e)
        {
            using (var f = new FormAddEvent())
                if (f.ShowDialog() == DialogResult.OK)
                    ReloadGrid();
        }

        private void ExportIcsButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("ICS export unchanged.");
        }

        private void CalendarBtn_Click(object sender, EventArgs e)
        {
            var favs = _repo.GetFavoriteEventsForCurrentMonth();
            if (favs.Count > 0)
                new CalendarForm(favs).Show();
        }

        private void FormList_Load(object sender, EventArgs e)
        {
            ReloadGrid();
        }
        private void DrawRadiusCircle()
        {
            radiusOverlay.Polygons.Clear();

            if (_radiusMiles <= 0)
                return;

            var points = new List<PointLatLng>();
            double lat = _centerLat * Math.PI / 180;
            double lon = _centerLon * Math.PI / 180;
            double d = _radiusMiles / EarthRadiusMiles;

            for (int i = 0; i <= 360; i += 5)
            {
                double brng = i * Math.PI / 180;

                double lat2 = Math.Asin(Math.Sin(lat) * Math.Cos(d) +
                             Math.Cos(lat) * Math.Sin(d) * Math.Cos(brng));

                double lon2 = lon + Math.Atan2(
                    Math.Sin(brng) * Math.Sin(d) * Math.Cos(lat),
                    Math.Cos(d) - Math.Sin(lat) * Math.Sin(lat2));

                points.Add(new PointLatLng(
                    lat2 * 180 / Math.PI,
                    lon2 * 180 / Math.PI));
            }

            var poly = new GMapPolygon(points, "radius");
            poly.Stroke = new Pen(Color.Blue, 2);
            poly.Fill = new SolidBrush(Color.FromArgb(40, Color.Blue));

            radiusOverlay.Polygons.Add(poly);
        }
        private double DistanceMiles(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;

            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) *
                Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusMiles * c;
        }
        private void HighlightGridRow(EventItem ev)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.DataBoundItem == ev)
                {
                    row.Selected = true;
                    dataGridView1.CurrentCell = row.Cells[0];
                    break;
                }
            }
        }
        private void ShowDetail(EventItem ev)
        {
            detailBox.Text =
        $@"{ev.Title}

            📅 When
            {ev.StartTime:yyyy-MM-dd HH:mm} {(ev.EndTime.HasValue ? $"→ {ev.EndTime:yyyy-MM-dd HH:mm}" : "")}

            📍 Where
            {ev.VenueName}
            {ev.Address}
            {ev.City}, {ev.State} {ev.Zip}

            🏷 Category
            {ev.Category}

            📝 Description
            {ev.Description}

            🔗 Link
            {ev.Url}";
        }
    }
}