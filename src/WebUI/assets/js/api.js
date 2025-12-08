import CONFIG from "../../config/config.js";

export async function fetchStrategiesData(url_path) {
  try {
    const response = await fetch(`${CONFIG.STRATEGIES_BASE_URL}/${url_path}`);
    const data = await response.json();
    return data;
  } catch (err) {
    console.error("Error fetching data from strategies:", err);
    return [];
  }
}

// Fetch symbols for a strategy + interval
export async function fetchSymbols(url_path) {
  try {
    const response = await fetch(`${CONFIG.STRATEGIES_BASE_URL}/${url_path}`);
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

export async function fetchSymbolsForJson(url_path, jsonBody) {
  try {
    const response = await fetch(`${CONFIG.STRATEGIES_BASE_URL}/${url_path}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(jsonBody),
    });
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
      time: toUnixFromIST(item.timestamp),
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

function toUnixFromIST(istString) {
  const date = new Date(istString.replace(" ", "T") + "+05:30");
  return Math.floor(date.getTime() / 1000); // return in seconds
}
