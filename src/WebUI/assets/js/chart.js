import { fetchSymbolData } from "./api.js";

const baseChartOptions = {
  layout: {
    background: { type: "solid", color: "white" },
    textColor: "#1f2937",
    fontFamily: "Inter, sans-serif",
  },
  grid: {
    vertLines: { color: "rgba(229,231,235,0.8)", style: 1 },
    horzLines: { color: "rgba(229,231,235,0.8)", style: 1 },
  },
  crosshair: {
    mode: LightweightCharts.CrosshairMode.MagnetOHLC,
    vertLine: { color: "rgba(75,85,99,0.3)", width: 1, style: 2 },
    horzLine: { color: "rgba(75,85,99,0.3)", width: 1, style: 2 },
  },
  timeScale: {
    visible: true,
    borderColor: "#e5e7eb",
    timeVisible: true,
    secondsVisible: false,
  },
  rightPriceScale: {
    borderColor: "#e5e7eb",
    scaleMargins: { top: 0.1, bottom: 0.2 },
  },
  height: 250,
};

// Create a chart for one symbol
export async function createChartForSymbol(container, symbol, interval) {
  const wrapper = document.createElement("div");
  wrapper.className = "chart-wrapper";

  const label = document.createElement("h3");
  label.className = "chart-label";
  label.innerText = `${symbol} - ${interval.toUpperCase()}`;

  label.style.cursor = "pointer";

  label.addEventListener("click", () => {
    window.open(
      `/pages/symbol.html?q=${encodeURIComponent(symbol)}`,
      "_blank",
      "noopener,noreferrer"
    );
  });

  const chartDiv = document.createElement("div");
  chartDiv.className = "chart";

  wrapper.appendChild(label);
  wrapper.appendChild(chartDiv);
  container.appendChild(wrapper);

  const chart = LightweightCharts.createChart(chartDiv, {
    ...baseChartOptions,
    width: chartDiv.clientWidth,
  });

  chart.applyOptions({
    timeScale: {
      ...baseChartOptions.timeScale,
      tickMarkFormatter: makeISTTickFormatter(interval),
    },
    localization: {
      timeFormatter: makeISTTooltipFormatter(),
    },
  });

  const candleSeries = chart.addSeries(LightweightCharts.CandlestickSeries, {
    upColor: "#26a69a",
    downColor: "#ef5350",
    borderVisible: false,
    wickUpColor: "#26a69a",
    wickDownColor: "#ef5350",
  });

  const bars = await fetchSymbolData(symbol, interval);
  candleSeries.setData(bars);

  new ResizeObserver(() => {
    chart.applyOptions({ width: chartDiv.clientWidth });
  }).observe(chartDiv);
}

function makeISTTickFormatter(interval) {
  return function (time) {
    const date = new Date(time * 1000);

    const optionsBase = { timeZone: "Asia/Kolkata", hour12: false };

    if (interval.endsWith("min")) {
      // Minute timeframe → HH:MM
      return date.toLocaleString("en-IN", {
        ...optionsBase,
        hour: "2-digit",
        minute: "2-digit",
      });
    }

    if (interval.endsWith("h") || interval.endsWith("d")) {
      const day = date.getDate();
      if (day === 1) {
        // First day of month → show month only
        return date.toLocaleString("en-IN", { ...optionsBase, month: "short" });
      }
      return date.toLocaleString("en-IN", {
        ...optionsBase,
        day: "2-digit",
      });
    }

    if (
      interval.endsWith("w") ||
      interval.endsWith("m") ||
      interval.endsWith("y")
    ) {
      const day = date.getDate();
      const month = date.getMonth(); // 0-11
      if (day === 1 && month === 0) {
        // 1st day of year → show year only
        return date.toLocaleString("en-IN", {
          ...optionsBase,
          year: "numeric",
        });
      }
      if (day === 1) {
        // 1st day of month → show short month only
        return date.toLocaleString("en-IN", { ...optionsBase, month: "short" });
      }
      // Normal week/month → show day + month
      return date.toLocaleString("en-IN", { ...optionsBase, day: "2-digit" });
    }

    return date.toLocaleString("en-IN", {
      ...optionsBase,
      month: "short",
      year: "numeric",
    });
  };
}

function makeISTTooltipFormatter() {
  return function (unix) {
    const d = new Date(unix * 1000);
    return d.toLocaleString("en-IN", {
      timeZone: "Asia/Kolkata",
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
      hour12: false,
    });
  };
}
