import talib as ta
import numpy as np
import pandas as pd

class BollingerBandsStrategy:
    
    @staticmethod
    def is_near_lower_bb(df: pd.DataFrame, period: int = 12, nbdev: float = 2.0, duration=12, tolerance=0.01) -> bool:
        if (len(df) < period) or (len(df) < duration):
            return False
        upper, middle, lower = ta.BBANDS(
            df["Close"],
            timeperiod=period,
            nbdevup=nbdev,
            nbdevdn=nbdev,
            matype=0
        )
        duration = min(duration, len(df))
        recent_close = df["Close"][-duration:]
        recent_lower = lower[-duration:]
        dist_to_lower = (recent_close - recent_lower) / recent_close
        return np.any(dist_to_lower <= tolerance)
    
    @staticmethod
    def is_near_upper_bb(df: pd.DataFrame, period: int = 12, nbdev: float = 2.0, duration=12, tolerance=0.01) -> bool:
        if (len(df) < period) or (len(df) < duration):
            return False
        upper, middle, lower = ta.BBANDS(
            df["Close"],
            timeperiod=period,
            nbdevup=nbdev,
            nbdevdn=nbdev,
            matype=0
        )
        duration = min(duration, len(df))
        recent_close = df["Close"][-duration:]
        recent_upper = upper[-duration:]
        dist_to_upper = (recent_upper - recent_close) / recent_close
        return np.any(dist_to_upper <= tolerance)
