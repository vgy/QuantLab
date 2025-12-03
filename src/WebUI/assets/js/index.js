import { createChartForSymbol } from "./chart.js";

document.addEventListener("DOMContentLoaded", () => {
  loadIndicesCharts();
  document
    .getElementById("refresh-btn")
    .addEventListener("click", loadIndicesCharts);
});

async function loadIndicesCharts() {
  const indicesContainer = document.getElementById("indicesContainer");
  indicesContainer.innerHTML = "";
  const indices = ["NIFTY50", "BANKNIFTY"];
  for (const symbol of indices) {
    const symbolWrapper = document.createElement("div");
    indicesContainer.appendChild(symbolWrapper);
    symbolWrapper.className = "symbol-wrapper";
    symbolWrapper.innerHTML = "";
    await createChartForSymbol(symbolWrapper, symbol, "1d");
    await createChartForSymbol(symbolWrapper, symbol, "1h");
    await createChartForSymbol(symbolWrapper, symbol, "5min");
  }
}
