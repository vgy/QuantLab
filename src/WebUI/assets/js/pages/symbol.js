import { createChartForSymbol } from "../chart.js";

function getQuerySymbol() {
  const params = new URLSearchParams(window.location.search);
  return params.get("q");
}

const symbol = getQuerySymbol();
if (!symbol) {
  document.body.innerHTML = "<p>❌ Symbol not provided in query string.</p>";
  throw new Error("Symbol query missing");
}

const chartConfigs = [
  { id: "chart-1w", interval: "1w" },
  { id: "chart-1d", interval: "1d" },
  { id: "chart-1h", interval: "1h" },
  { id: "chart-30min", interval: "30min" },
  { id: "chart-15min", interval: "15min" },
  { id: "chart-5min", interval: "5min" },
];

async function loadCharts() {
  for (const { id, interval } of chartConfigs) {
    const container = document.getElementById(id);
    container.innerHTML = "";
    await createChartForSymbol(container, symbol, interval);
  }
}

document.getElementById("refresh-btn").addEventListener("click", loadCharts);

loadCharts();
