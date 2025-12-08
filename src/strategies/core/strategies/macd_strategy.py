import talib as ta
import pandas as pd

class MacdStrategy:
    @staticmethod
    def is_bullish_macd_crossover(df: pd.DataFrame, fast: int = 12, slow: int = 26, signal: int = 9, duration: int = 12) -> bool:
        if (len(df) < slow) or (len(df) < duration):
            return False
        macd, signal, hist = ta.MACD(
            df['Close'],
            fastperiod=fast,
            slowperiod=slow,
            signalperiod=signal
        )
        macd = pd.Series(macd, index=df.index)
        signal = pd.Series(signal, index=df.index)
        bullish = (macd.shift(1) < signal.shift(1)) & (macd > signal)
        duration = min(duration, len(df))
        return bullish.iloc[-duration:].any()   
    
    @staticmethod
    def is_bearish_macd_crossover(df: pd.DataFrame, fast: int = 12, slow: int = 26, signal: int = 9, duration: int = 12) -> bool:
        if (len(df) < slow) or (len(df) < duration):
            return False
        macd, signal, hist = ta.MACD(
            df['Close'],
            fastperiod=fast,
            slowperiod=slow,
            signalperiod=signal
        )
        macd = pd.Series(macd, index=df.index)
        signal = pd.Series(signal, index=df.index)
        bearish = (macd.shift(1) > signal.shift(1)) & (macd < signal)
        duration = min(duration, len(df))
        return bearish.iloc[-duration:].any()
