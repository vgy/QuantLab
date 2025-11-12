import CONFIG from "../../config/config.js";
import { fetchStrategies, fetchSymbols } from "./api.js";
import { createChartForSymbol } from "./chart.js";

const chartContainer = document.getElementById("chartContainer");
const strategySelect = document.getElementById("strategy");
const intervalSelect = document.getElementById("interval");
const getSymbolsBtn = document.getElementById("GetSymbols");

// Populate dropdowns
export async function initializeUI() {
  // Populate intervals
  CONFIG.INTERVALS.forEach((intv) => {
    const option = document.createElement("option");
    option.value = intv;
    option.innerText = intv;
    intervalSelect.appendChild(option);
  });

  // Populate strategies from API
  const strategies = await fetchStrategies();
  strategies.forEach((strategy) => {
    const option = document.createElement("option");
    option.value = strategy;
    option.innerText = strategy;
    strategySelect.appendChild(option);
  });
}

// Handle "GetSymbols" click
export function setupEventListeners() {
  getSymbolsBtn.addEventListener("click", async () => {
    const strategy = strategySelect.value;
    const interval = intervalSelect.value;

    if (!strategy) {
      alert("Please select a strategy!");
      return;
    }

    chartContainer.innerHTML = "";

    const symbols = await fetchSymbols(strategy, interval);
    for (const symbol of symbols) {
      await createChartForSymbol(chartContainer, symbol, interval);
    }
  });
}
