import numpy as np
import pandas as pd
import talib
from loguru import logger
from strategies.constants.candlestick_patterns import get_patterns

class CandlestickPatternsStrategy:
    @staticmethod    
    def contains_candlestick_pattern(df: pd.DataFrame, group: str, subgroup: str, pattern: str, duration: int = 12) -> bool:
        patterns = get_patterns(group, subgroup, pattern)
        if not patterns:
            raise KeyError(f"No pattern available for group:'{group}', subgroup:'{subgroup}', and pattern:'{pattern}'")
        try:            
            for ptrn in patterns:
                func = getattr(talib, ptrn.upper())
                result = func(df["Open"], df["High"], df["Low"], df["Close"])
                if len(result) >= duration:
                    if ((group == "bullish" and np.any(result[-duration:] > 0)) or 
                        (group == "bearish" and np.any(result[-duration:] < 0)) or 
                        ((group == "neutral" or group == "all") and np.any(result[-duration:] != 0))):
                        return True
        except Exception as e:
            logger.warning(f"Skipping for group:'{group}', subgroup:'{subgroup}', pattern:'{pattern}', and duration:'{duration}': {e}")
        return False
