// =====================
//  Covert Sites Map JS
// =====================

// ---------------------
//  Leaflet Map Setup
// ---------------------
const map = L.map("map", {
    zoomControl: true,
    attributionControl: true
}).setView([-30, 0], 2.3);

const worldImagery = L.tileLayer(
    "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
    {
        attribution: "Tiles © Esri, Maxar, Earthstar Geographics",
        maxZoom: 18
    }
);

const darkOverlay = L.tileLayer(
    "https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png",
    {
        subdomains: "abcd",
        maxZoom: 19,
        opacity: 0.5,
        attribution: "&copy; CartoDB, © OpenStreetMap contributors"
    }
);

// Night-ops aesthetic
L.layerGroup([worldImagery, darkOverlay]).addTo(map);

// ---------------------
//  Global State
// ---------------------
let sites = [];
let markers = [];

// ---------------------
//  Utility Helpers
// ---------------------

function getValue(obj, ...keys) {
    for (const key of keys) {
        if (obj && obj[key] != null) return obj[key];
    }
    return null;
}

function normalizeSites(rawSites) {
    if (!Array.isArray(rawSites)) return [];

    return rawSites
        .map(s => ({
            id: getValue(s, "id", "Id"),
            name: getValue(s, "name", "Name"),
            location: getValue(s, "location", "Location"),
            description: getValue(s, "description", "Description"),
            publicNarrative: getValue(s, "publicNarrative", "PublicNarrative"),
            latitude: parseFloat(getValue(s, "latitude", "Latitude")),
            longitude: parseFloat(getValue(s, "longitude", "Longitude"))
        }))
        .filter(s =>
            s.id != null &&
            !Number.isNaN(s.latitude) &&
            !Number.isNaN(s.longitude)
        );
}

function showLoadError() {
    const el = document.createElement("li");
    el.className = "list-group-item list-group-item-danger text-danger";
    el.textContent = "Error loading sites.";

    document.getElementById("siteList")?.appendChild(el.cloneNode(true));
    document.getElementById("siteListOffcanvas")?.appendChild(el.cloneNode(true));
}

// ---------------------
//  Data Loading
// ---------------------
async function loadData() {
    try {
        const res = await fetch("/api/public/sites");
        if (!res.ok) throw new Error("Failed to load sites.");

        sites = normalizeSites(await res.json());

        renderSiteList(sites);
        renderMarkers(sites);
        resetMapView();

    } catch (err) {
        console.error(err);
        showLoadError();
    }
}

// ---------------------
//  Sidebar Rendering
// ---------------------
function renderSiteList(sites) {
    const listDesktop = document.getElementById("siteList");
    const listMobile = document.getElementById("siteListOffcanvas");

    [listDesktop, listMobile].forEach(list => {
        if (!list) return;
        list.innerHTML = "";

        sites.forEach((site, index) => {
            const li = document.createElement("li");
            li.className = "list-group-item list-group-item-action";
            li.textContent = site.name || `Site ${site.id}`;

            li.addEventListener("click", () => {
                setActive(index);
                centerOnSite(site, index);
            });

            list.appendChild(li);
        });
    });
}

function setActive(index) {
    document.querySelectorAll("#siteList .list-group-item")
        .forEach((el, i) => {
            el.classList.toggle("active", i === index);
        });
}

function centerOnSite(site, index) {
    let zoom = window.innerWidth < 768 ? 5 : 6;
    if (site.latitude < -60) zoom = window.innerWidth < 768 ? 3 : 4;

    map.flyTo([site.latitude, site.longitude], zoom, {
        animate: true,
        duration: 1.5
    });

    setTimeout(() => markers[index]?.openPopup(), 700);

    // Close offcanvas on mobile
    const offcanvasEl = document.getElementById("offcanvasSites");
    if (offcanvasEl?.classList.contains("show")) {
        bootstrap.Offcanvas.getInstance(offcanvasEl)?.hide();
        setTimeout(() => map.invalidateSize(), 400);
    }
}

// ---------------------
//  Marker Rendering
// ---------------------
function buildPopupContent(site, index) {
    return `
        <div>
            <h6>${site.name}</h6>
            <p><strong>Location:</strong> ${site.location || "N/A"}</p>
            <p>${site.description || ""}</p>
            <button class="btn btn-sm btn-outline-info mt-2 details-btn"
                data-index="${index}">
                View Details
            </button>
        </div>
    `;
}

function renderMarkers(sites) {
    markers.forEach(m => map.removeLayer(m));
    markers = [];

    sites.forEach((site, index) => {
        const marker = L.marker([site.latitude, site.longitude])
            .addTo(map)
            .bindPopup(buildPopupContent(site, index), {
                maxWidth: 250,
                autoPan: true,
                autoPanPaddingTopLeft: [20, 80],
                autoPanPaddingBottomRight: [20, 20]
            });

        markers.push(marker);
    });
}

map.on("popupopen", (e) => {
    const btn = e.popup._contentNode.querySelector(".details-btn");
    if (!btn) return;

    btn.addEventListener("click", async () => {
        const site = sites[btn.dataset.index];
        await showDetails(site);
    });
});

// ---------------------
//  Details Modal
// ---------------------
async function showDetails(site) {
    const modalTitle = document.getElementById("detailsModalTitle");
    const modalBody = document.getElementById("detailsModalBody");

    modalBody.innerHTML = "";

    // Public narrative
    const p = document.createElement("p");
    p.textContent = site.publicNarrative || "No public narrative available.";
    modalBody.appendChild(p);

    // Fetch artifacts for this site
    const artifacts = await loadArtifactsForSite(site.id);

    if (artifacts.length > 0) {
        modalBody.appendChild(document.createElement("hr"));

        const heading = document.createElement("h6");
        heading.textContent = "Artifacts";
        modalBody.appendChild(heading);

        artifacts.forEach(a => {
            modalBody.appendChild(buildArtifactCard(a));
        });
    } else {
        const empty = document.createElement("p");
        empty.innerHTML = "<em>No artifacts found for this site.</em>";
        modalBody.appendChild(empty);
    }

    modalTitle.textContent = `${site.name} — Details`;

    new bootstrap.Modal(document.getElementById("detailsModal")).show();
}

// ---------------------
//  Artifact Fetch + UI
// ---------------------
async function loadArtifactsForSite(siteId) {
    try {
        const res = await fetch(`/api/public/sites/${siteId}/artifacts`);
        if (!res.ok) return [];

        // PublicArtifactResponse[]
        return await res.json();
    } catch (err) {
        console.error("Error loading artifacts:", err);
        return [];
    }
}

function buildArtifactCard(a) {
    const tmpl = document.getElementById("artifact-template");
    const card = tmpl.content.cloneNode(true);

    const img = card.querySelector("img.details-image");
    if (a.primaryImageUrl && img) {
        img.src = `${a.primaryImageUrl}?v=${Date.now()}`;
        img.alt = a.name;
    } else if (img) {
        img.remove();
    }

    card.querySelector("h6").textContent = a.name;
    card.querySelector(".catalog").innerHTML =
        `<strong>Catalog #:</strong> ${a.catalogNumber}`;
    card.querySelector(".description").textContent =
        a.publicNarrative || "No public narrative available.";

    return card;
}

// ---------------------
//  Reset Map View
// ---------------------
function resetMapView() {
    if (!markers.length) return;

    map.invalidateSize();

    const group = L.featureGroup(markers);

    const padding = window.innerWidth < 768
        ? [100, 100]
        : [50, 60];

    map.fitBounds(group.getBounds(), {
        padding,
        animate: true,
        duration: 1.5
    });

    map.once("moveend", () => map.invalidateSize());
}

// ---------------------
//  Initialize
// ---------------------
loadData();
