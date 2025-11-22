const downloadTypes = [
  "5m",
  "5m Retry",
  "15m",
  "15m Retry",
  "1H",
  "1H Retry",
  "1D",
  "1D Retry",
  "1W",
  "1W Retry",
];

const downsamplingTypes = ["30min", "1h"];

function setCookie(name, value, days = 7) {
  const expires = new Date(Date.now() + days * 864e5).toUTCString();
  document.cookie = `${encodeURIComponent(name)}=${encodeURIComponent(
    value
  )}; expires=${expires}; path=/`;
}

function getCookie(name) {
  return document.cookie
    .split("; ")
    .find((row) => row.startsWith(encodeURIComponent(name) + "="))
    ?.split("=")[1];
}

const downloadTableBody = document.getElementById("downloadTableBody");
downloadTypes.forEach((type) => {
  const tr = document.createElement("tr");
  const key = type.replace(/\s+/g, "_").toLowerCase();

  tr.innerHTML = `
<td>${type}</td>
<td><button id="download-btn-${key}">Download</button></td>
<td class="response" id="download-resp-${key}">Loading previous data...</td>
`;

  downloadTableBody.appendChild(tr);

  const saved = getCookie(`download_resp_${key}`);
  document.getElementById(`download-resp-${key}`).textContent = saved
    ? decodeURIComponent(saved)
    : "No previous response.";
});

downloadTypes.forEach((type) => {
  const key = type.replace(/\s+/g, "_").toLowerCase();
  const btn = document.getElementById(`download-btn-${key}`);
  const respCell = document.getElementById(`download-resp-${key}`);

  btn.addEventListener("click", async () => {
    const baseType = type
      .toLowerCase()
      .replace(/\s*retry\s*/i, "")
      .trim();
    const isRetry = type.toLowerCase().includes("retry");
    const url = isRetry
      ? `http://localhost:6001/api/download/bars/${baseType}/retry`
      : `http://localhost:6001/api/download/bars/${baseType}`;

    respCell.textContent = "Downloading...";

    try {
      const response = await fetch(url, { method: "POST" });
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      const text = await response.text();
      const message = JSON.parse(text).message || "(empty response)";

      respCell.textContent = message;
      setCookie(`download_resp_${key}`, message);
    } catch (err) {
      respCell.textContent = `Error: ${err.message}`;
    }
  });
});

const downsamplingTableBody = document.getElementById("downsamplingTableBody");
downsamplingTypes.forEach((type) => {
  const tr = document.createElement("tr");
  const key = type.replace(/\s+/g, "_").toLowerCase();

  tr.innerHTML = `
<td>${type}</td>
<td><button id="downsampling-btn-${key}">Downsampling</button></td>
<td class="response" id="downsampling-resp-${key}">Loading previous data...</td>
`;

  downsamplingTableBody.appendChild(tr);

  const saved = getCookie(`downsampling_resp_${key}`);
  document.getElementById(`downsampling-resp-${key}`).textContent = saved
    ? decodeURIComponent(saved)
    : "No previous response.";
});

downsamplingTypes.forEach((type) => {
  const key = type.replace(/\s+/g, "_").toLowerCase();
  const btn = document.getElementById(`downsampling-btn-${key}`);
  const respCell = document.getElementById(`downsampling-resp-${key}`);

  btn.addEventListener("click", async () => {
    const baseType = type.toLowerCase();
    const url = `http://localhost:6002/downsampling/15min/${baseType}`;
    respCell.textContent = "downsamplinging...";

    try {
      const response = await fetch(url, { method: "POST" });
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      const text = await response.text();
      const message = JSON.parse(text).message || "(empty response)";

      respCell.textContent = message;
      setCookie(`downsampling_resp_${key}`, message);
    } catch (err) {
      respCell.textContent = `Error: ${err.message}`;
    }
  });
});
