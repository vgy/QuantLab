import os
from typing import List
import numpy as np
import pandas as pd
import talib
from loguru import logger
from strategies.constants.candlestick_patterns import get_patterns
from strategies.utils.config_loader import load_config

class CandlestickPatternsService:

    def __init__(self):
        self.directory = load_config().get("data").get("directory")
    
    def get_symbols_for_pattern_and_interval(self, group: str, subgroup: str, pattern: str, interval: str, period: int) -> List[str]:
        patterns = get_patterns(group, subgroup, pattern)
        if not patterns:
            raise KeyError(f"No pattern available for group:'{group}', subgroup:'{subgroup}', and pattern:'{pattern}'")
        required_cols = ["Open", "High", "Low", "Close"]
        self.folder_path = os.path.join(self.directory, interval)
        symbols = []
        for file_name in os.listdir(self.folder_path):
            # Select only files with format "<interval>-<symbol>.csv"
            if file_name.startswith(f"{interval}-") and file_name.endswith(".csv"):
                full_file_name = os.path.join(self.folder_path, file_name)
                if not os.path.isfile(full_file_name):
                    continue
                try:
                    df = pd.read_csv(full_file_name)
                    if df.empty:
                        continue                    
                    if not all(col in df.columns for col in required_cols):
                        continue
                    
                    for ptrn in patterns:        
                        # Get the function dynamically
                        func = getattr(talib, ptrn.upper())
                        result = func(df["Open"], df["High"], df["Low"], df["Close"])
                        if len(result) >= period:
                            if ((group == "bullish" and np.any(result[-period:] > 0)) or 
                                (group == "bearish" and np.any(result[-period:] < 0)) or 
                                ((group == "neutral" or group == "all") and np.any(result[-period:] != 0))):
                                # Extract symbol from filename: "<interval>-<symbol>.csv"
                                symbol = file_name.split("-")[1].replace(".csv", "")
                                symbols.append(symbol)
                except Exception as e:
                    logger.warning(f"Skipping {file_name}: {e}")
        logger.info(f"Found {len(symbols)} symbols for group:'{group}', subgroup:'{subgroup}', pattern:'{pattern}', interval:'{interval}', and period:'{period}'")
        return symbols
    
    def get_candlestick_patterns_for_symbol_interval_period(self, symbol: str, interval: str, period: int) -> List[str]:
        matched_patterns = []
        patterns = get_patterns("all", "all", "all")
        if not patterns:
            raise KeyError("No patterns available")
        required_cols = ["Timestamp", "Open", "High", "Low", "Close"]
        folder_name = os.path.join(self.directory, interval)
        file_name = f'{interval}-{symbol}.csv'
        full_file_name = os.path.join(folder_name, file_name)
        if not os.path.isfile(full_file_name):
            logger.error(f"For {symbol} and {interval}, its corresponding file does not exist: {full_file_name}")
            return matched_patterns
        try:
            df = pd.read_csv(full_file_name)
            if df.empty:
                logger.error(f"For {symbol} and {interval}, its corresponding file is empty: {full_file_name}")
                return matched_patterns
            if not all(col in df.columns for col in required_cols):
                logger.error(f"For {symbol} and {interval}, its corresponding file does not have required OHLC columns: {full_file_name}")
                return matched_patterns        
            for ptrn in patterns:
                func = getattr(talib, ptrn.upper())
                result = func(df["Open"], df["High"], df["Low"], df["Close"])
                if len(result) >= period and np.any(result[-period:] != 0):
                    for i in range(-period, 0):
                        if result.iloc[i] != 0:
                            rec = df.iloc[i]
                            matched_pattern = f'{rec['Timestamp']} - {ptrn}'
                            matched_patterns.append(matched_pattern)
        except Exception as e:
            logger.error(f"Skipping symbol:'{symbol}', interval:'{interval}', and period:'{period}': {e}")
        matched_patterns.sort()
        logger.info(f"Found {len(matched_patterns)} patterns for symbol:'{symbol}', interval:'{interval}', and period:'{period}'")
        return matched_patterns

