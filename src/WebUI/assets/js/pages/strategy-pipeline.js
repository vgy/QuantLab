import { fetchSymbolsForJson } from "../api.js";
import { createChartForSymbol } from "../chart.js";
import { pipelineTemplates } from "../constants/strategy_pipeline_const.js";
const path = "../../../data/symbols_nse_and_ib.csv";

const templateSelect = document.getElementById("templateSelect");
const jsonInput = document.getElementById("jsonInput");
const chartContainer = document.getElementById("chartContainer");

const symbolRank = {};

async function loadSymbolRank() {
  try {
    const response = await fetch(path);
    const text = await response.text();
    const lines = text.trim().split("\n").slice(1);
    lines.forEach((line, index) => {
      const [symbol] = line.split(",");
      symbolRank[symbol] = index; // smaller index = bigger market cap
    });
  } catch (err) {
    console.error("Error loading Symbol Rank:", err);
  }
}

function sortSymbolsByRank(symbols) {
  symbols.sort((a, b) => {
    const indexA = symbolRank[a] ?? Infinity;
    const indexB = symbolRank[b] ?? Infinity;
    return indexA - indexB;
  });
  return symbols;
}

function loadTemplateOptions() {
  Object.keys(pipelineTemplates).forEach((key) => {
    const opt = document.createElement("option");
    opt.value = key;
    opt.textContent = key.charAt(0).toUpperCase() + key.slice(1);
    templateSelect.appendChild(opt);
  });
  if (pipelineTemplates.bullish) {
    templateSelect.value = "bullish";
    jsonInput.value = pipelineTemplates.bullish;
  }
}

async function runStrategyPipeline() {
  const input = document.getElementById("jsonInput").value;
  let jsonBody;
  try {
    jsonBody = JSON.parse(input);
  } catch (e) {
    console.error("Invalid JSON: " + e.message);
    return;
  }

  chartContainer.innerHTML = "Running";
  const symbols = await fetchSymbolsForJson("pipeline/run", jsonBody);
  chartContainer.innerHTML = "";
  const sortedSymbols = sortSymbolsByRank(symbols);
  for (const symbol of sortedSymbols) {
    const symbolWrapper = document.createElement("div");
    chartContainer.appendChild(symbolWrapper);
    symbolWrapper.className = "symbol-wrapper";
    symbolWrapper.innerHTML = "";
    await createChartForSymbol(symbolWrapper, symbol, "1d");
    await createChartForSymbol(symbolWrapper, symbol, "1h");
    await createChartForSymbol(symbolWrapper, symbol, "5min");
  }
}

loadTemplateOptions();

templateSelect.addEventListener("change", (e) => {
  const key = e.target.value;
  jsonInput.value = key ? pipelineTemplates[key] : "";
});

document.addEventListener("DOMContentLoaded", () => {
  loadSymbolRank();
  document
    .getElementById("runBtn")
    .addEventListener("click", runStrategyPipeline);
});
