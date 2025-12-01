import { createChartForSymbol } from "../chart.js";
import { fetchStrategiesData } from "../api.js";

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

const intervals = ["1d", "1h", "5min", "1w", "30min", "15min"];

function formatDateTime(datetimeStr) {
  const date = new Date(datetimeStr.replace(" ", "T"));
  return date.toLocaleString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  });
}

const controlsContainer = document.getElementById("controls-container");

intervals.forEach((interval) => {
  const label = document.createElement("label");
  label.textContent = `${interval}:`;

  const select = document.createElement("select");
  select.id = `period-${interval}`;

  for (let i = 1; i <= 30; i++) {
    const option = document.createElement("option");
    option.value = i;
    option.textContent = i;
    select.appendChild(option);
  }

  select.value = "2";

  controlsContainer.appendChild(label);
  controlsContainer.appendChild(select);
});

async function loadCharts() {
  for (const { id, interval } of chartConfigs) {
    const container = document.getElementById(id);
    container.innerHTML = "";
    await createChartForSymbol(container, symbol, interval);
  }
}

async function fetchPatterns() {
  const patternsListContainer = document.getElementById(
    "patterns-list-container"
  );
  patternsListContainer.innerHTML = "";

  for (const interval of intervals) {
    const period = document.getElementById(`period-${interval}`).value;

    try {
      const url_path = `patterns/candlestick/${symbol}/${interval}/${period}`;
      const data = await fetchStrategiesData(url_path);
      const patterns = data.patterns || [];

      if (patterns.length === 0) continue;

      const reversedPatterns = [...patterns].reverse();
      const block = document.createElement("div");
      block.className = "interval-block";

      const header = document.createElement("h3");
      header.textContent = `Interval: ${interval}, Period: ${period}`;
      block.appendChild(header);

      const list = document.createElement("ul");

      patterns.forEach((item) => {
        const [datetime, pattern] = item.split(" - ");
        const li = document.createElement("li");
        li.textContent = `${formatDateTime(datetime)} — ${pattern}`;
        list.appendChild(li);
      });

      block.appendChild(list);
      patternsListContainer.appendChild(block);
    } catch (error) {
      console.error(
        `Error fetching interval ${interval} with period ${period}:`,
        error
      );
    }
  }
}

document.getElementById("refresh-btn").addEventListener("click", loadCharts);
document.getElementById("scan-btn").addEventListener("click", fetchPatterns);

document.addEventListener("DOMContentLoaded", async () => {
  await loadCharts();
  await fetchPatterns();
});
