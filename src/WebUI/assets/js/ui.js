import CONFIG from "../../config/config.js";
import { fetchSymbols } from "./api.js";
import { createChartForSymbol } from "./chart.js";

const CANDLESTICK_PATTERNS = {
  All: {
    All: ["All"],
  },
  Bullish: {
    All: ["All"],
    Reversal: [
      "All",
      "3 White Soldiers",
      "Morning Star",
      "Morning Doji Star",
      "Piercing",
      "Engulfing",
      "Hammer",
      "Inverted Hammer",
      "Ladder Bottom",
      "Unique 3 River",
      "Homing Pigeon",
      "Matching Low",
      "3 Inside",
      "3 Outside",
    ],
    Continuation: [
      "All",
      "Mat Hold",
      "Rise Fall 3 Methods",
      "Tasuki Gap",
      "Separating Lines",
    ],
  },
  Bearish: {
    All: ["All"],
    Reversal: [
      "All",
      "3 Black Crows",
      "Evening Star",
      "Evening Doji Star",
      "Engulfing",
      "Dark Cloud Cover",
      "Shooting Star",
      "Hanging Man",
      "Identical 3 Crows",
      "2 Crows",
      "Harami",
      "Harami Cross",
      "3 Inside",
      "3 Outside",
    ],
    Continuation: [
      "All",
      "Rise Fall 3 Methods",
      "Upside Gap 2 Crows",
      "Gap Side Side White",
      "Thrusting",
      "On Neck",
      "In Neck",
    ],
  },
  Neutral: {
    All: ["All"],
    Doji: [
      "All",
      "Doji",
      "Long Legged Doji",
      "Dragon Fly Doji",
      "Gravestone Doji",
      "Rickshaw Man",
    ],
    "Spinning / Range": [
      "All",
      "Spinning Top",
      "Short Line",
      "Long Line",
      "High Wave",
    ],
    Marubozu: ["All", "Marubozu"],
    Hikkake: ["All", "Hikkake", "Hikkake Mod"],
    "Side-by-Side / Gap Methods": [
      "All",
      "Gap Side Side White",
      "X Side Gap 3 Methods",
    ],
    "Breakaway / Counterattack / Stalled": [
      "All",
      "Breakaway",
      "Counter Attack",
      "Stalled Pattern",
      "Takuri",
    ],
    "Tri-Star & Special": ["All", "Tristar", "Conceal Baby Swall"],
  },
};

/* -------------------------------------------------
   Helper: Fill a <select> element
--------------------------------------------------*/
function fillSelect(selectEl, items) {
  selectEl.innerHTML = "";
  items.forEach((item) => {
    const option = document.createElement("option");
    option.value = item;
    option.textContent = item;
    selectEl.appendChild(option);
  });
}

const groupElement = document.getElementById("group");
const subgroupElement = document.getElementById("subgroup");
const patternElement = document.getElementById("pattern");
const intervalElement = document.getElementById("interval");
const periodElement = document.getElementById("period");
const chartContainer = document.getElementById("chartContainer");
const getScanBtn = document.getElementById("scanBtn");

/* -------------------------------------------------
   Loading Dropdowns
--------------------------------------------------*/
function loadGroups() {
  fillSelect(groupElement, Object.keys(CANDLESTICK_PATTERNS));
}

function loadSubGroups() {
  const group = groupElement.value;
  fillSelect(subgroupElement, Object.keys(CANDLESTICK_PATTERNS[group]));
}

function loadPatterns() {
  const group = groupElement.value;
  const subgroup = subgroupElement.value;
  fillSelect(patternElement, CANDLESTICK_PATTERNS[group][subgroup]);
}

/* -------------------------------------------------
   Event Listeners for Cascade Dropdowns
--------------------------------------------------*/
groupElement.addEventListener("change", () => {
  loadSubGroups();
  loadPatterns();
});

subgroupElement.addEventListener("change", loadPatterns);

// Populate dropdowns
export async function initializeUI() {
  loadGroups();
  loadSubGroups();
  loadPatterns();
  CONFIG.INTERVALS.forEach((intv) => {
    const option = document.createElement("option");
    option.value = intv;
    option.innerText = intv;
    intervalElement.appendChild(option);
  });
}

export function setupEventListeners() {
  getScanBtn.addEventListener("click", async () => {
    const group = groupElement.value.toLowerCase().trim();
    const subgroup = subgroupElement.value.toLowerCase().trim();
    const pattern = patternElement.value.toLowerCase().replace(/\s+/g, "");
    const interval = intervalElement.value.toLowerCase().trim();
    const period = periodElement.value.toLowerCase().trim();
    const cdlpattern = pattern == "all" ? "all" : `cdl${pattern}`;

    const url_path = `candlestick/${group}/${subgroup}/${cdlpattern}/${interval}/${period}`;

    chartContainer.innerHTML = "";

    const symbols = await fetchSymbols(url_path);
    for (const symbol of symbols) {
      await createChartForSymbol(chartContainer, symbol, interval);
    }
  });
}
