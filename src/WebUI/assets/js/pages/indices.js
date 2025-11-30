import { createChartForSymbol } from "../chart.js";
const path = "../../../data/nse_index_securities.csv";

document.addEventListener("DOMContentLoaded", () => {
  loadCSV();
  document.getElementById("scanBtn").addEventListener("click", loadCharts);
});

let csvData = {};

async function loadCSV() {
  try {
    const response = await fetch(path);
    const text = await response.text();
    csvData = parseCSV(text);
    const indices = Object.keys(csvData);
    const indexSelect = document.getElementById("indexSelect");
    indexSelect.innerHTML = indices
      .map((g) => `<option value="${g}">${g}</option>`)
      .join("");
  } catch (err) {
    console.error("Error loading CSV:", err);
  }
}

function parseCSV(text) {
  const lines = text.trim().split("\n");
  const dict = {};
  for (let i = 1; i < lines.length; i++) {
    const cols = lines[i].split(",");
    const index = cols[0].trim();
    const symbol = cols[1].trim();
    if (!dict[index]) dict[index] = [];
    dict[index].push(symbol);
  }

  return dict;
}

async function loadCharts() {
  const selectedIndex = document.getElementById("indexSelect").value;
  const filteredSymbols = csvData[selectedIndex];
  const indicesContainer = document.getElementById("indicesContainer");
  indicesContainer.innerHTML = "";
  for (const symbol of filteredSymbols) {
    const symbolWrapper = document.createElement("div");
    indicesContainer.appendChild(symbolWrapper);
    symbolWrapper.className = "symbol-wrapper";
    symbolWrapper.innerHTML = "";
    await createChartForSymbol(symbolWrapper, symbol, "1d");
    await createChartForSymbol(symbolWrapper, symbol, "1h");
    await createChartForSymbol(symbolWrapper, symbol, "5min");
  }
}
