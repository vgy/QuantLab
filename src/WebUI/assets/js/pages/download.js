const types = [
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

const tableBody = document.getElementById("tableBody");
types.forEach((type) => {
  const tr = document.createElement("tr");
  const key = type.replace(/\s+/g, "_").toLowerCase();

  tr.innerHTML = `
<td>${type}</td>
<td><button id="btn-${key}">Download</button></td>
<td class="response" id="resp-${key}">Loading previous data...</td>
`;

  tableBody.appendChild(tr);

  const saved = getCookie(`resp_${key}`);
  document.getElementById(`resp-${key}`).textContent = saved
    ? decodeURIComponent(saved)
    : "No previous response.";
});

types.forEach((type) => {
  const key = type.replace(/\s+/g, "_").toLowerCase();
  const btn = document.getElementById(`btn-${key}`);
  const respCell = document.getElementById(`resp-${key}`);

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
      setCookie(`resp_${key}`, message);
    } catch (err) {
      respCell.textContent = `Error: ${err.message}`;
    }
  });
});
