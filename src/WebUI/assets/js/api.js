import CONFIG from "../../config/config.js";

// Fetch available strategies
export async function fetchStrategies() {
  try {
    const response = await fetch(CONFIG.STRATEGIES_BASE_URL);
    const data = await response.json();
    if (!Array.isArray(data.strategies)) {
      console.error("Unexpected strategies format:", data);
      return [];
    }
    return data.strategies;
  } catch (err) {
    console.error("Error fetching strategies:", err);
    return [];
  }
}

// Fetch symbols for a strategy + interval
export async function fetchSymbols(strategy, interval = "1d") {
  try {
    const response = await fetch(
      `${CONFIG.STRATEGIES_BASE_URL}/${strategy}/${interval}`
    );
    const data = await response.json();
    if (!Array.isArray(data.symbols)) {
      console.error("Unexpected data format:", data);
      return [];
    }
    return data.symbols;
  } catch (err) {
    console.error("Error fetching symbols:", err);
    return [];
  }
}

// Fetch bars for a symbol
export async function fetchSymbolData(symbol, interval = "1d") {
  try {
    const response = await fetch(
      `${CONFIG.API_BASE_URL}/${symbol}/${interval}`
    );
    const data = await response.json();
    return data.bars.map((item) => ({
      time: item.timestamp / 1000,
      open: item.open,
      high: item.high,
      low: item.low,
      close: item.close,
      volume: item.volume,
    }));
  } catch (err) {
    console.error(`Error fetching data for ${symbol}:`, err);
    return [];
  }
}
