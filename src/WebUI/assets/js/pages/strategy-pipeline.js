import { fetchSymbolsForJson } from "../api.js";
import { createChartForSymbol } from "../chart.js";
import { pipelineTemplates } from "../constants/strategy_pipeline_const.js";

const templateSelect = document.getElementById("templateSelect");
const jsonInput = document.getElementById("jsonInput");
const chartContainer = document.getElementById("chartContainer");

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
  for (const symbol of symbols) {
    await createChartForSymbol(chartContainer, symbol, "5min");
  }
}

loadTemplateOptions();

templateSelect.addEventListener("change", (e) => {
  const key = e.target.value;
  jsonInput.value = key ? pipelineTemplates[key] : "";
});

document
  .getElementById("runBtn")
  .addEventListener("click", runStrategyPipeline);
