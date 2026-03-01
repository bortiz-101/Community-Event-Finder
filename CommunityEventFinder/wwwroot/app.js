// ================= MAP INIT =================
const categoryColors = {
    "Music and theather": "#e74c3c",
    "Game, party and social": "#ff7f50",
    Sports: "#27ae60",
    Food: "#f39c12",
    Study: "#3498db",
    Arts: "#9b59b6",
    Business: "#34495e",
    Festival: "#8e44ad",
    Community: "#16a085",
    Career: "#2c3e50",
    Volunteer: "#d35400",
    Health: "#1abc9c",
    Family: "#e84393",
    Other: "#7f8c8d"
};

let map = L.map('map').setView([41.9975, -87.6586], 12);

L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png')
    .addTo(map);

let clusterLayer = L.markerClusterGroup();
map.addLayer(clusterLayer);

let circleLayer = null;

let allEvents = [];
let favoritesOnly = false;
let searchText = "";
let radiusMiles = 0;

let centerLat = 41.9975;
let centerLon = -87.6586;

let selectedId = null;

function categoryColor(cat) {

    if (!cat) return "#7f8c8d";

    if (cat === "Music and theather") return "#e74c3c"; 
    if (cat === "Game, party and social") return "#ff7f50";
    if (cat === "Sports") return "#27ae60";
    if (cat === "Food") return "#f39c12";
    if (cat === "Study") return "#3498db";
    if (cat === "Arts") return "#9b59b6";
    if (cat === "Business") return "#34495e";
    if (cat === "Festival") return "#8e44ad";
    if (cat === "Community") return "#16a085"; 
    if (cat === "Career") return "#2c3e50";
    if (cat === "Volunteer") return "#d35400";
    if (cat === "Health") return "#1abc9c";
    if (cat === "Family") return "#e84393"; 
    if (cat === "Other") return "#7f8c8d";

    return "#7f8c8d";
}

// ================= LOAD =================

async function loadEvents() {
    const url = favoritesOnly ? "/api/events/favorites" : "/api/events";
    const res = await fetch(url);
    allEvents = await res.json();
    applyFilters();
}


// ================= FAVORITE =================

async function toggleFav(id) {
    await fetch("/api/events/favorite/" + id, { method: "PUT" });
    await loadEvents();
}


// ================= DELETE =================

async function deleteEvent(id) {
    if (!confirm("Delete this event?")) return;

    const res = await fetch("/api/events/" + id, { method: "DELETE" });

    if (!res.ok) {
        const text = await res.text();
        alert(text || "Delete failed");
        return;
    }

    if (selectedId === id) {
        selectedId = null;
        document.getElementById("detail").value = "Select an event…";
    }

    await loadEvents();
}


// ================= FILTER =================

function applyFilters() {
    let filtered = allEvents;

    // search
    if (searchText.trim()) {
        const s = searchText.toLowerCase();
        filtered = filtered.filter(e =>
            (e.title || "").toLowerCase().includes(s) ||
            (e.city || "").toLowerCase().includes(s) ||
            (e.category || "").toLowerCase().includes(s)
        );
    }

    // radius
    if (radiusMiles > 0) {
        filtered = filtered.filter(ev => {
            if (!ev.latitude) return false;
            return distanceMiles(centerLat, centerLon, ev.latitude, ev.longitude) <= radiusMiles;
        });
    }

    renderList(filtered);
    renderMap(filtered);
}


// ================= SEARCH =================

function onSearch(val) {
    searchText = val;
    applyFilters();
}

function clearSearch() {
    const box = document.getElementById("searchBox");
    box.value = "";
    searchText = "";
    applyFilters();
}


// ================= FAVORITES ONLY =================

function setFavoritesOnly(val) {
    favoritesOnly = !!val;
    loadEvents();
}


// ================= RADIUS =================

function setRadius(val) {
    radiusMiles = (val === "All radius" || val === "All") ? 0 : parseFloat(val);
    drawCircle();
    applyFilters();
}


// ================= CENTER =================

async function setCenterFromAddress(addr) {
    if (!addr) return;

    try {
        const res = await fetch(
            "https://nominatim.openstreetmap.org/search?format=json&limit=1&countrycodes=us&q="
            + encodeURIComponent(addr)
        );

        const data = await res.json();

        if (!data.length) {
            alert("Address not found or too vague. Please enter full US street address.");
            return;
        }

        const result = data[0];
        const lat = parseFloat(result.lat);
        const lon = parseFloat(result.lon);

        // US bounding check
        if (lat < 24 || lat > 50 || lon < -125 || lon > -66) {
            alert("Address must be within United States.");
            return;
        }

        // Require street-level precision
        if (result.type === "city" || result.type === "state" || result.type === "country") {
            alert("Please enter a full street-level address.");
            return;
        }

        centerLat = lat;
        centerLon = lon;

        map.setView([centerLat, centerLon], 12);
        drawCircle();
        applyFilters();

    } catch (e) {
        alert("Address search failed");
        console.log(e);
    }
}


// ================= CIRCLE =================

function drawCircle() {
    if (circleLayer)
        map.removeLayer(circleLayer);

    if (radiusMiles <= 0) return;

    circleLayer = L.circle([centerLat, centerLon], { radius: radiusMiles * 1609 })
        .addTo(map);
}


// ================= DISTANCE =================

function distanceMiles(lat1, lon1, lat2, lon2) {
    const R = 3958.8;
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;

    const a =
        Math.sin(dLat / 2) ** 2 +
        Math.cos(lat1 * Math.PI / 180) *
        Math.cos(lat2 * Math.PI / 180) *
        Math.sin(dLon / 2) ** 2;

    return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}


// ================= MAP =================
function makeMarkerIcon(color) {

    return L.divIcon({
        className: "",
        html: `
<svg width="28" height="40" viewBox="0 0 24 36">
<path fill="${color}"
d="M12 0C7 0 3 4 3 9c0 7 9 27 9 27s9-20 9-27c0-5-4-9-9-9z"/>
<circle cx="12" cy="10" r="4" fill="white"/>
</svg>
`,
        iconSize: [28, 40],
        iconAnchor: [14, 40],
        popupAnchor: [0, -35]
    });

}
function renderMap(events) {
    clusterLayer.clearLayers();

    let bounds = [];

    events.forEach(ev => {
        if (!ev.latitude) return;

        const color = categoryColors[ev.category] || categoryColors.default;

        const marker = L.marker(
            [ev.latitude, ev.longitude],
            { icon: makeMarkerIcon(color) }
        );

        marker.on("click", () => {
            marker.openPopup();
            selectEvent(ev);
        });

        clusterLayer.addLayer(marker);
        bounds.push([ev.latitude, ev.longitude]);
    });

    if (bounds.length && selectedId === null)
        map.fitBounds(bounds, { padding: [50, 50] });
}


// ================= LIST =================

function renderList(events) {
    const list = document.getElementById("list");
    list.innerHTML = "";

    events.forEach(ev => {
        const div = document.createElement("div");
        div.className = "item" + (ev.eventId === selectedId ? " selected" : "");
        div.id = "item-" + ev.eventId;

        const isFav = !!ev.isFavorite;

        const canDelete = (ev.source || "").toLowerCase() === "user";

        div.innerHTML = `
      <div class="row">
        <div class="title" style="color:${categoryColor(ev.category)}">${escapeHtml(ev.title || "")}</div>
        <div class="actions">
          <span class="star ${isFav ? "on" : ""}" title="Favorite">★</span>
          ${canDelete ? `<span class="trash" title="Delete">🗑</span>` : ``}
        </div>
      </div>
      <div class="meta">${escapeHtml((ev.city || "") + (ev.category ? " • " + ev.category : ""))}</div>
    `;

        div.addEventListener("click", () => selectEvent(ev));

        div.querySelector(".star").addEventListener("click", async (e) => {
            e.stopPropagation();
            await toggleFav(ev.eventId);
        });

        const trash = div.querySelector(".trash");
        if (trash) {
            trash.addEventListener("click", async (e) => {
                e.stopPropagation();
                await deleteEvent(ev.eventId);
            });
        }

        list.appendChild(div);
    });
}


// ================= SELECT =================

function selectEvent(ev) {
    selectedId = ev.eventId;

    showDetail(ev);

    if (!ev.latitude || !ev.longitude) {
        alert("This event has no valid coordinates. Map location cannot be shown.");
    } else {
        map.setView([ev.latitude, ev.longitude], 14);
    }

    scrollToListItem(ev.eventId);
}


// ================= SCROLL =================

function scrollToListItem(id) {
    const el = document.getElementById("item-" + id);
    if (!el) return;

    el.scrollIntoView({ behavior: "smooth", block: "center" });
}


// ================= DETAIL =================

function showDetail(ev) {
    const box = document.getElementById("detail");

    box.value =
        `${ev.title || ""}

When:
${ev.startTime || ""}${ev.endTime ? " → " + ev.endTime : ""}

Where:
${ev.venueName || ""}
${ev.address || ""}
${ev.city || ""}, ${ev.state || ""} ${ev.zip || ""}

Category:
${ev.category || ""}

${ev.description || ""}

${ev.url ? "Link: " + ev.url : ""}`;
}

// ================= CALENDAR WINDOW =================

function openCalendarWindow() {
    const win = window.open("", "calendar", "width=1100,height=750");

    win.document.write(`
<html>
<head>
<meta charset="utf-8"/>

<link href="https://cdn.jsdelivr.net/npm/fullcalendar@6.1.8/index.global.min.css" rel="stylesheet">
<script src="https://cdn.jsdelivr.net/npm/fullcalendar@6.1.8/index.global.min.js"><\/script>

<style>
  body { margin:0; font-family: Arial; }
  .toolbar {
    padding: 10px;
    border-bottom: 1px solid #ddd;
    display: flex;
    gap: 8px;
    align-items: center;
  }
  button { padding: 6px 10px; cursor: pointer; }
  #calendar { padding: 10px; }
  .infoBox {
    padding: 10px;
    border-top: 1px solid #ddd;
    font-size: 13px;
    background: #fafafa;
  }
</style>
</head>

<body>

<div class="toolbar">
  <button id="btnMonth">Month</button>
  <button id="btnWeek">Week</button>
  <button id="btnDay">Day</button>
  <button id="btnToday">Today</button>

  <span style="margin-left:auto"></span>
  <button id="toggleBtn">All Events</button>
</div>

<div id="calendar"></div>
<div id="info" class="infoBox">Select an event</div>

<script>
  // ===== data from parent window =====
  const allEvents = ${JSON.stringify(allEvents)};

  // ===== category colors =====
    const COLORS = {
      "Music and theather":"#e74c3c",
      "Game, party and social":"#ff7f50",
      Sports:"#27ae60",
      Food:"#f39c12",
      Study:"#3498db",
      Arts:"#9b59b6",
      Business:"#34495e",
      Festival:"#8e44ad",
      Community:"#16a085",
      Career:"#2c3e50",
      Volunteer:"#d35400",
      Health:"#1abc9c",
      Family:"#e84393",
      Other:"#7f8c8d"
    };

  function categoryColor(cat){
    return COLORS[cat] || "#555";
  }

  // ===== toggle mode =====
  let showFav = false; // false = all, true = starred only

  function buildEventSource(){
    return allEvents
      .filter(e => !showFav || e.isFavorite)
      .map(e => {
        const c = categoryColor(e.category);
        return {
          title: e.title,
          start: e.startTime,
          end: e.endTime || null,

          // IMPORTANT: month view dots + other views use these
          backgroundColor: c,
          borderColor: c,
          textColor: "#fff",

          extendedProps: {
            venue: e.venueName,
            address: e.address,
            city: e.city,
            state: e.state,
            zip: e.zip,
            category: e.category,
            description: e.description
          }
        };
      });
  }

  function renderDetail(fcEvent){
    const p = fcEvent.extendedProps || {};
    const when =
      fcEvent.start
        ? fcEvent.start.toLocaleString()
        : "";

    const when2 =
      fcEvent.end
        ? " → " + fcEvent.end.toLocaleString()
        : "";

    document.getElementById("info").innerHTML =
      "<b>" + (fcEvent.title || "") + "</b><br><br>" +
      "<b>When:</b><br>" + when + when2 + "<br><br>" +
      "<b>Where:</b><br>" +
      (p.venue || "") + "<br>" +
      (p.address || "") + "<br>" +
      ((p.city || "") + (p.state ? ", " + p.state : "") + (p.zip ? " " + p.zip : "")) +
      "<br><br>" +
      "<b>Category:</b> " + (p.category || "") +
      "<br><br>" +
      (p.description || "");
  }

  // ===== calendar init =====
  const calendar = new FullCalendar.Calendar(
    document.getElementById("calendar"),
    {
      initialView: "timeGridWeek",
      events: buildEventSource(),

      slotEventOverlap: false,

      // click shows detail
      eventClick: function(info){
        renderDetail(info.event);
      },

      // hover tooltip
      eventDidMount: function(info){
        const p = info.event.extendedProps || {};
        info.el.title =
          (info.event.title || "") + "\\n" +
          (p.address || "");
      }
    }
  );

  calendar.render();

  // ===== view buttons =====
  document.getElementById("btnMonth").onclick = () => calendar.changeView("dayGridMonth");
  document.getElementById("btnWeek").onclick  = () => calendar.changeView("timeGridWeek");
  document.getElementById("btnDay").onclick   = () => calendar.changeView("timeGridDay");
  document.getElementById("btnToday").onclick = () => calendar.today();

  // ===== reload helper =====
  function reload(){
    calendar.removeAllEvents();
    const src = buildEventSource();
    // addEventSource can take an array of events directly
    calendar.addEventSource(src);
  }

  // ===== toggle button =====
  const toggleBtn = document.getElementById("toggleBtn");

  function updateToggleText(){
    toggleBtn.innerText = showFav ? "⭐ Starred" : "All Events";
  }
  updateToggleText();

  toggleBtn.onclick = () => {
    showFav = !showFav;
    updateToggleText();
    reload();
  };

<\/script>

</body>
</html>
`);
}

// ================= UI HELPERS =================

function escapeHtml(s) {
    return (s || "").replace(/[&<>"']/g, c => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;",
        '"': "&quot;", "'": "&#39;"
    }[c]));
}


// ================= INIT =================

function toggleDefaultCenter() {
    const box = document.getElementById("centerBox");
    const checked = document.getElementById("defaultCheck").checked;

    if (checked) {
        centerLat = 41.9975;
        centerLon = -87.6586;

        box.value = "1032 W Sheridan Rd, Chicago, IL";
        box.disabled = true;

        map.setView([centerLat, centerLon], 12);
        drawCircle();
        applyFilters();
    } else {
        box.disabled = false;
    }
}

function openAddWindow() {
    window.open("/add.html", "addEvent", "width=500,height=700");
}

// ================= INIT =================

async function init() {

    const params = new URLSearchParams(location.search);

    const addr = params.get("addr");
    const cat = params.get("cat");
    const r = params.get("radius");

    if (cat)
        searchText = cat;

    if (r)
        radiusMiles = parseFloat(r);

    if (addr) {
        await setCenterFromAddress(addr);
    }

    await loadEvents();
}

init();

function showAll() {

    searchText = "";
    radiusMiles = 0;
    selectedId = null;

    const s = document.getElementById("searchBox");
    if (s) s.value = "";

    centerLat = 41.9975;
    centerLon = -87.6586;
    map.setView([centerLat, centerLon], 12);

    history.replaceState(null, "", "/index.html");

    loadEvents();
}

// ===== expose to window =====

window.loadEvents = loadEvents;
window.clearSearch = clearSearch;
window.setFavoritesOnly = setFavoritesOnly;
window.openAddWindow = openAddWindow;
window.openCalendarWindow = openCalendarWindow;
window.setCenterFromAddress = setCenterFromAddress;
window.setRadius = setRadius;
window.onSearch = onSearch;
window.toggleDefaultCenter = toggleDefaultCenter;
window.showAll = showAll;

