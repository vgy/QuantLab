from scipy.signal import argrelextrema
import talib as ta
import numpy as np
import pandas as pd

class RsiStrategy:
    @staticmethod
    def is_rsi_overbought(df: pd.DataFrame, period: int = 14, overbought: int = 70, duration: int = 12) -> bool:
        if (len(df) < period) or (len(df) < duration):
            return False
        rsi = ta.RSI(df["Close"], timeperiod=period)
        duration = min(duration, len(df))
        return np.any(rsi[-duration:] >= overbought)
    
    @staticmethod
    def is_rsi_oversold(df: pd.DataFrame, period: int = 14, oversold: int = 30, duration: int = 12) -> bool:
        if (len(df) < period) or (len(df) < duration):
            return False
        rsi = ta.RSI(df["Close"], timeperiod=period)
        duration = min(duration, len(df))
        return np.any(rsi[-duration:] <= oversold)

    @staticmethod
    def is_rsi_bullish_divergence(df: pd.DataFrame, period: int = 14, duration: int = 12) -> bool:
        if (len(df) < period) or (len(df) < duration):
            return False
        rsi = ta.RSI(df["Close"], timeperiod=period)
        duration = min(duration, len(df))
        prices = df["Close"][-duration:].values
        rsi_window = rsi[-duration:].values
        
        # Detect pivot lows (local minima)
        pivot_indices = argrelextrema(prices, np.less)[0]

        if len(pivot_indices) < 2:
            return False  # Not enough pivots to check divergence

        # Check all pairs of pivots
        for i in range(len(pivot_indices) - 1):
            i1 = pivot_indices[i]
            i2 = pivot_indices[i + 1]
            p1, p2 = prices[i1], prices[i2]
            rsi1, rsi2 = rsi_window[i1], rsi_window[i2]

            # Bullish divergence: price lower low, RSI higher low
            if p2 < p1 and rsi2 > rsi1:
                return True

        return False
    
    @staticmethod
    def is_rsi_bearish_divergence(df: pd.DataFrame, period: int = 14, duration: int = 12) -> bool:
        if (len(df) < period) or (len(df) < duration):
            return False
        rsi = ta.RSI(df["Close"], timeperiod=period)
        duration = min(duration, len(df))
        prices = df["Close"][-duration:].values
        rsi_window = rsi[-duration:].values

        # Detect pivot highs (local maxima)
        pivot_indices = argrelextrema(prices, np.greater)[0]

        if len(pivot_indices) < 2:
            return False  # Not enough pivots to check divergence

        # Check all pairs of consecutive pivots
        for i in range(len(pivot_indices) - 1):
            i1 = pivot_indices[i]
            i2 = pivot_indices[i + 1]
            p1, p2 = prices[i1], prices[i2]
            rsi1, rsi2 = rsi_window[i1], rsi_window[i2]

            # Bearish divergence: price higher high, RSI lower high
            if p2 > p1 and rsi2 < rsi1:
                return True

        return False